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
        public bool Enabled { get => _enabled; set { _enabled = value; OnTimerChanged("Enabled"); OnPropertyChanged(); } }

        private string _backupName;
        public string BackupName { get => _backupName; set { _backupName = value; OnTimerChanged("BackupName"); OnPropertyChanged(); } }

        private string _backupPath;
        public string BackupPath { get => _backupPath; set { _backupPath = value; OnTimerChanged("BackupPath"); OnPropertyChanged(); } }

        private int _dueTime;
        public int DueTime { get => _dueTime / 1000; set { _dueTime = value * 1000; OnTimerChanged("DueTime"); OnPropertyChanged(); } }

        private int _period;
        public int Period { get => _period / 1000; set { _period = value * 1000; OnTimerChanged("Period"); OnPropertyChanged(); } }

        private int _backupAmount;
        public int BackupAmount { get => _backupAmount; set { _backupAmount = value; OnTimerChanged("BackupAmount"); OnPropertyChanged(); } }

        private DateTime _nextRun;
        public DateTime NextRun { get => _nextRun; set { _nextRun = value; OnPropertyChanged(); } }

        private int MSBetweenBackupCheck = 1000;
        private BackupLogic _core;

        public void Initialize(BackupLogic core)
        {
            _core = core;

            if (DateTime.Now > NextRun)
            {
                NextRun = DateTime.Now.AddMilliseconds(_dueTime);
            }

            _timer?.Dispose();
            if (Enabled)
                _timer = new Timer(OnTimerTrigger, this, MSBetweenBackupCheck, MSBetweenBackupCheck);
        }

        private void OnTimerTrigger(object state)
        {
            if (!Enabled)
            {
                _timer?.Dispose();
                _timer = null;
                return;
            }
               
            if (_timer == null && Enabled) {
                Log.Warn($"Backup failed, trying again in {DueTime} seconds");
                NextRun = NextRun.AddMilliseconds(_dueTime);
                _core.Save();
                return;
            }

            if (DateTime.Now > NextRun)
            {
                RunBackup(this);
                Log.Debug($"Triggering {_backupName}");
                NextRun = NextRun.AddMilliseconds(_period);
                _core.Save();
            }
        }

        private void OnTimerChanged(string field = "")
        {
            NextRun = DateTime.Now.AddMilliseconds(_dueTime);

            if(_timer == null && Enabled)
            {
                var diff = NextRun - DateTime.Now;
                _timer = new Timer(OnTimerTrigger, this, diff.Milliseconds, MSBetweenBackupCheck);
            }
        }

        private void RunBackup(object state)
        {

            if (((TorchServer)TorchBase.Instance).State != ServerState.Running)
            {
                return;
            }

            if (!Enabled)
                return;

            var Backup = (BackupTimer)state;

            Log.Debug($"Trying to run backup: {Backup.BackupName}");

            string filePath;
            string divider;
            if (Backup.BackupPath.EndsWith("\\")) divider = ""; else divider = "\\";


           
            filePath = $"{Backup.BackupPath}{divider}{Backup.BackupName}_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
           

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
