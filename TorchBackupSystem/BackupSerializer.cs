using Sandbox.Engine.Platform;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using NLog;
using System.IO;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Utils;

namespace TorchBackupSystem
{
    public static class BackupSerializer
    {
        private static readonly Logger Log = LogManager.GetLogger("Backup");

        public static void Save(string TargetDir = null, string customName = null, Action callbackOnFinished = null)
        {
            MySessionSnapshot snapshot;
            bool snapshotSuccess = MySession.Static.Save(out snapshot, customName);

            if (TargetDir != null)
            {
                snapshot.SavingDir = $"{TargetDir}.new";
                snapshot.TargetDir = TargetDir;
            }

            snapshot.SaveParallel(Complete);

            Log.Info($"Backing up to {TargetDir}");
        }

        public static void Complete()
        {
            Log.Info($"Backing up completed!");
        }
    }
}
