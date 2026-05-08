using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ONESolutionUtility_v1
{
    internal class WorkstationInstaller
    {
        private readonly Action<string, LogLevel> _log;

        public WorkstationInstaller(Action<string, LogLevel> log)
        {
            _log = log;
        }

        // ── Entry point ───────────────────────────────────────────────────────────

        public void RunInstall(InstallConfig cfg)
        {
            if (!ValidatePaths(cfg))
                return;

            if (cfg.InstallRms)
                SetupRms(cfg);
            else
                _log("Install RMS set to false. Skipping.", LogLevel.Warning);

            if (cfg.InstallJms)
                SetupJms(cfg);
            else
                _log("Install JMS set to false. Skipping.", LogLevel.Warning);

            if (cfg.InstallMfr)
                SetupMfr(cfg);
            else
                _log("Install MFR set to false. Skipping.", LogLevel.Warning);

            if (cfg.InstallCad)
                SetupCad(cfg);
            else
                _log("Install CAD set to false. Skipping.", LogLevel.Warning);

            if (cfg.InstallOsmct)
                SetupOsmct(cfg);
            else
                _log("Install OSMCT set to false. Skipping.", LogLevel.Warning);

            _log("Done.", LogLevel.Success);
        }

        // ── Validation ────────────────────────────────────────────────────────────

        private bool ValidatePaths(InstallConfig cfg)
        {
            try
            {
                AssertPath(cfg.ShortcutPath, "Shortcut Destination Path");

                if (cfg.InstallRms || cfg.InstallJms)
                {
                    AssertPath(cfg.RmsJmsPath, "RMS/JMS App Path");
                    AssertPath(cfg.SharedPath,  "Shared Path (ODBC/VCRedist/Components)");
                }
                if (cfg.InstallMfr)
                {
                    AssertPath(cfg.MoblanPath, "Moblan Path");
                    AssertPath(cfg.SharedPath,  "Shared Path (ODBC/VCRedist)");
                }
                if (cfg.InstallOsmct)
                {
                    AssertPath(cfg.MupdatePath,      "Mupdate Path");
                    AssertPath(cfg.OsmctInstallPath, "OSMCT Installer Path");
                }
                if (cfg.InstallCad)
                {
                    AssertPath(cfg.ReportViewerPath, "Report Viewer Path");
                    AssertPath(cfg.CadPath,          "CAD Path");
                }

                return true;
            }
            catch (Exception ex)
            {
                _log("Configuration error: " + ex.Message, LogLevel.Error);
                _log("Please fix the path and try again.", LogLevel.Error);
                return false;
            }
        }

        private void AssertPath(string path, string label)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception($"Required path '{label}' is empty.");
            if (!Directory.Exists(path))
                throw new Exception($"Required path '{label}' does not exist: {path}");
        }

        // ── Per-app setup ─────────────────────────────────────────────────────────

        private void SetupRms(InstallConfig cfg)
        {
            string name = cfg.IsCloud ? cfg.RmsCloudShortcutName : cfg.RmsShortcutName;
            CreateShortcut(cfg.ShortcutPath, name,
                Path.Combine(cfg.RmsJmsPath, "onesolutionrms.exe"), cfg.RmsJmsPath);
            InstallRmsJmsComponents(cfg);
        }

        private void SetupJms(InstallConfig cfg)
        {
            string name = cfg.IsCloud ? cfg.JmsCloudShortcutName : cfg.JmsShortcutName;
            CreateShortcut(cfg.ShortcutPath, name,
                Path.Combine(cfg.RmsJmsPath, "onesolutionjms.exe"), cfg.RmsJmsPath);
            InstallRmsJmsComponents(cfg);
        }

        private void InstallRmsJmsComponents(InstallConfig cfg)
        {
            InstallMsi(Path.Combine(cfg.SharedPath, "ONESolution RMS and JMS Components.msi"),
                "RMS and JMS Components");
            InstallExe(Path.Combine(cfg.SharedPath, "vc_redist.x64.exe"), "/S /v/qn",
                "VC Redistributable");
            InstallOdbc(cfg.SharedPath);
            EnsureFolder(cfg.FoxtmpPath);
            EnsureFolder(cfg.SuperionPath);
        }

        private void SetupMfr(InstallConfig cfg)
        {
            string name = cfg.IsCloud ? cfg.MfrCloudShortcutName : cfg.MfrShortcutName;
            CreateShortcut(cfg.ShortcutPath, name,
                Path.Combine(cfg.MoblanPath, "mfr.exe"), cfg.MoblanPath);
            InstallExe(Path.Combine(cfg.SharedPath, "vc_redist.x64.exe"), "/S /v/qn",
                "VC Redistributable");
            InstallOdbc(cfg.SharedPath);
            EnsureFolder(cfg.FoxtmpPath);
            EnsureFolder(cfg.SuperionPath);
        }

        private void SetupCad(InstallConfig cfg)
        {
            InstallExe(Path.Combine(cfg.ReportViewerPath, "reportviewer.exe"), "/q",
                "Report Viewer");

            if (cfg.IsCloud)
            {
                InstallExe(Path.Combine(cfg.CadPath, "CADWorkstation_Installer.exe"), "/S /v/qn",
                    "CAD Cloud Workstation Installer");
            }
            else
            {
                CreateShortcut(cfg.ShortcutPath, cfg.CadShortcutName,
                    Path.Combine(cfg.CadPath, "onesolutioncad.exe"), cfg.CadPath);
                EnsureFolder(cfg.FoxtmpPath);
                EnsureFolder(cfg.SuperionPath);
            }
        }

        private void SetupOsmct(InstallConfig cfg)
        {
            _log("Installing OSMCT...", LogLevel.Info);
            int code = RunProcess(
                Path.Combine(cfg.OsmctInstallPath, "onesolutionmctsetup.exe"), "/S /v/qn");
            if (code == 0)
                _log("OSMCT installer completed successfully.", LogLevel.Success);
            else
                _log($"OSMCT installer exited with code {code}.", LogLevel.Warning);

            string name = cfg.IsCloud ? cfg.OsmctCloudShortcutName : cfg.OsmctShortcutName;
            CreateShortcut(cfg.ShortcutPath, name,
                @"C:\ossimob\osupdater.exe", @"C:\ossimob");

            EnsureFolder(cfg.FoxtmpPath);
            EnsureFolder(cfg.SuperionPath);
            EnsureFolder(cfg.OssimobPath);
            EnsureFolder(cfg.MobfilesPath);

            _log("Launching mupdate...", LogLevel.Info);
            try
            {
                Process.Start(Path.Combine(cfg.MupdatePath, "mupdate.exe"));
            }
            catch (Exception ex)
            {
                _log("Failed to launch mupdate: " + ex.Message, LogLevel.Error);
            }
        }

        // ── Installer helpers ─────────────────────────────────────────────────────

        private void InstallMsi(string msiPath, string displayName)
        {
            _log($"Installing {displayName}...", LogLevel.Info);
            int code = RunProcess("msiexec.exe", $"/i \"{msiPath}\" /qn /norestart");
            ReportExitCode(code, displayName);
        }

        private void InstallOdbc(string sharedPath)
        {
            _log("Installing MS ODBC SQL Driver 17...", LogLevel.Info);
            int code = RunProcess("msiexec.exe",
                $"/i \"{Path.Combine(sharedPath, "msodbcsql64_17.msi")}\" /qn /norestart IACCEPTMSODBCSQLLICENSETERMS=YES");
            ReportExitCode(code, "ODBC Driver 17");
        }

        private void InstallExe(string exePath, string args, string displayName)
        {
            _log($"Installing {displayName}...", LogLevel.Info);
            int code = RunProcess(exePath, args);
            ReportExitCode(code, displayName);
        }

        private void ReportExitCode(int code, string name)
        {
            if (code == 1638)
                _log($"{name} is already installed.", LogLevel.Success);
            else if (code == 1603)
                _log($"Newer version of {name} is already installed.", LogLevel.Success);
            else if (code == 0)
                _log($"{name} installed successfully.", LogLevel.Success);
            else
                _log($"{name} installer exited with code {code}.", LogLevel.Warning);
        }

        private int RunProcess(string fileName, string arguments = "")
        {
            var psi = new ProcessStartInfo
            {
                FileName        = fileName,
                Arguments       = arguments,
                UseShellExecute = false,
                CreateNoWindow  = true
            };
            using (var proc = Process.Start(psi))
            {
                if (proc == null)
                {
                    _log($"Failed to start process: {fileName}", LogLevel.Error);
                    return -1;
                }
                proc.WaitForExit();
                return proc.ExitCode;
            }
        }

        // ── Shortcut creation (via WScript.Shell COM) ─────────────────────────────

        private void CreateShortcut(string directory, string name, string targetPath, string workingDir)
        {
            try
            {
                string lnkPath = Path.Combine(directory, name + ".lnk");
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell     = Activator.CreateInstance(shellType);
                dynamic shortcut  = shell.CreateShortcut(lnkPath);
                shortcut.TargetPath       = targetPath;
                shortcut.WorkingDirectory = workingDir;
                shortcut.Save();
                _log($"Shortcut created: {lnkPath}", LogLevel.Success);
            }
            catch (Exception ex)
            {
                _log($"Failed to create shortcut '{name}': {ex.Message}", LogLevel.Error);
            }
        }

        // ── Folder creation + permissions ─────────────────────────────────────────

        private void EnsureFolder(string folderPath)
        {
            try
            {
                _log($"Checking folder: {folderPath}", LogLevel.Info);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    _log($"Created folder: {folderPath}", LogLevel.Success);
                }

                var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                var dirInfo  = new DirectoryInfo(folderPath);
                var acl      = dirInfo.GetAccessControl();
                var rule     = new FileSystemAccessRule(
                    everyone,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);
                acl.AddAccessRule(rule);
                dirInfo.SetAccessControl(acl);

                _log($"Granted Full Control to Everyone on: {folderPath}", LogLevel.Success);
            }
            catch (Exception ex)
            {
                _log($"Failed to configure folder '{folderPath}': {ex.Message}", LogLevel.Error);
            }
        }
    }
}
