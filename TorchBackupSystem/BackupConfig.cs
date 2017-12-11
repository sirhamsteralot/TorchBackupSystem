using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;

namespace TorchBackupSystem
{
    public class BackupConfig : ViewModel
    {
        public ObservableCollection<BackupTimer> Backups { get; } = new ObservableCollection<BackupTimer>();
    }
}
