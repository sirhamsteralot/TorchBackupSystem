using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Torch;
using Torch.API;
using Torch.Commands;
using Torch.API.Managers;
using Torch.Server;
using System.ComponentModel;

namespace TorchBackupSystem
{
    public class BackupTimer : ViewModel, IDisposable
    {
        private Timer _timer;
        private static readonly Logger Log = LogManager.GetLogger("Backup");

        private bool _enabled;
        public bool Enabled { get => _enabled; set { _enabled = value; OnTimerChanged(); OnPropertyChanged(); } }

        private string _backupName;
        public string BackupName { get => _backupName; set { _backupName = value; OnTimerChanged(); OnPropertyChanged(); } }

        private string _backupPath;
        public string BackupPath { get => _backupPath; set { _backupPath = value; OnTimerChanged(); OnPropertyChanged(); } }

        private int _dueTime;
        public int DueTime { get => _dueTime / 1000; set { _dueTime = value * 1000; OnTimerChanged(); OnPropertyChanged(); } }

        private int _period;
        public int Period { get => _period / 1000; set { _period = value * 1000; OnTimerChanged(); OnPropertyChanged(); } }

        private bool _timeStamp;
        public bool TimeStamp { get => _timeStamp; set { _timeStamp = value; OnTimerChanged(); OnPropertyChanged(); } }

        private int _backupAmount;
        public int BackupAmount { get => _backupAmount; set { _backupAmount = value; OnTimerChanged(); OnPropertyChanged(); } }

        private DateTime _nextRun;
        public DateTime NextRun { get => _nextRun; set { _nextRun = value; OnPropertyChanged(); } }

        private void OnTimerChanged()
        {
            _timer?.Dispose();
            if (Enabled && Period > 0)
            {
                _timer = new Timer(RunBackup, this, _dueTime, _period);
                NextRun = DateTime.Now.AddMilliseconds(_dueTime);
            }
        }

        public void LoadPrevTimer()
        {
            if (NextRun > DateTime.Now && Enabled && Period > 0)
            {
                var diff = NextRun - DateTime.Now;

                _timer = new Timer(RunBackup, this, diff.Milliseconds, _period);
                NextRun = DateTime.Now.AddMilliseconds(diff.Milliseconds);
            } else
            {
                OnTimerChanged();
            }
        }

        private void RunBackup(object state)
        {
            

            if (((TorchServer)TorchBase.Instance).State != ServerState.Running)
            {
                OnTimerChanged();

                return;
            }

            NextRun = DateTime.Now.AddMilliseconds(_period);

            if (!Enabled)
                return;

            var Backup = (BackupTimer)state;

            string filePath;
            string divider;
            if (Backup.BackupPath.EndsWith("\\")) divider = ""; else divider = "\\";


            if (_timeStamp)
            {
                filePath = $"{Backup.BackupPath}{divider}{Backup.BackupName}_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            } else
            {
                filePath = $"{Backup.BackupPath}{divider}{Backup.BackupName}";
            }

            if (!CheckFileNameValid(filePath))
            {
                Log.Warn($"Bad file path for backup: {Backup.BackupName}, No backup will be made!");
                return;
            }

            if (!Directory.Exists($"{Backup.BackupPath}"))
            {
                Directory.CreateDirectory($"{Backup.BackupPath}");
            }

            var files = Directory.GetDirectories($"{Backup.BackupPath}");
            if (files.Length > 0)
            {
                Tuple<string, DateTime> oldestFile = null;
                int fileCount = 0;

                foreach (var file in files)
                {
                    if (file.StartsWith($"{Backup.BackupPath}{divider}{Backup.BackupName}"))
                    {
                        fileCount++;
                        if (oldestFile == null)
                        {
                            oldestFile = new Tuple<string, DateTime>(file, Directory.GetCreationTime(file));
                        }

                        var dateDiffFirst = DateTime.Now.Subtract(oldestFile.Item2);
                        var dateDiffSecond = DateTime.Now.Subtract(Directory.GetCreationTime(file));

                        if (dateDiffSecond > dateDiffFirst)
                        {
                            oldestFile = new Tuple<string, DateTime>(file, Directory.GetCreationTime(files[0]));
                        }
                    }
                }

                if (fileCount >= _backupAmount)
                    Directory.Delete(oldestFile.Item1, true);
            }

            try
            {
                BackupSerializer.Save(filePath);
            }
            catch(Exception e)
            {
                Log.Error($"COULD NOT MAKE A BACKUP {Backup.BackupName}!\n {e.StackTrace}");
            }
            
        }

        private bool CheckFileNameValid(string fileName)
        {
            FileInfo fi = null;
            try
            {
                fi = new FileInfo($"{fileName}\\hi.new");
            }
            catch (ArgumentException) { }
            catch (PathTooLongException) { }
            catch (NotSupportedException) { }
            if (ReferenceEquals(fi, null))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        ~BackupTimer()
        {
            try
            {
                Dispose();
            }
            catch
            {
                // fuck this exception in particulair
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
