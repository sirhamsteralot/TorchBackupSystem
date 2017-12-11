using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using NLog;
using NLog.Config;
using NLog.Targets;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Commands;
using Torch.Managers;
using VRage.Game;
using VRage.Game.Entity;

namespace TorchBackupSystem
{
    public class BackupLogic : TorchPluginBase, IWpfPlugin
    {
        public BackupConfig Config => _config?.Data;

        private BackupControl _control;
        private Persistent<BackupConfig> _config;
        private static readonly Logger Log = LogManager.GetLogger("Backup");

        /// <inheritdoc />
        public UserControl GetControl() => _control ?? (_control = new BackupControl(this));

        public void Save()
        {
            _config.Save();
            Log.Info("Configuration Saved.");
        }

        /// <inheritdoc />
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            _config = Persistent<BackupConfig>.Load(Path.Combine(StoragePath, "Backup.cfg"));
        }

        public override void Update()
        {
            base.Update();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _config.Save();
        }
    }
}
