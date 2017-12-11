using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TorchBackupSystem
{
    public partial class BackupControl : UserControl
    {
        private BackupLogic Plugin { get; }

        public BackupControl()
        {
            InitializeComponent();
        }

        public BackupControl(BackupLogic plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;
            var list = (DataGrid)sender;
            var items = list.SelectedItems.Cast<BackupTimer>().ToList();
            foreach (var item in items)
            {
                item.Enabled = false;
                Plugin.Config.Backups.Remove(item);
            }
        }

        private void SaveConfig_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
        }

        private void AddBackup_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Config.Backups.Add(new BackupTimer());
        }
    }
}
