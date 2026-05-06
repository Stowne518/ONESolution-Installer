using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ONESolutionUtility_v1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            BtnInstall.IsEnabled = false;
            ClearLog();

            InstallConfig config = BuildConfig();

            Thread thread = new Thread(() =>
            {
                try
                {
                    RunInstall(config);
                }
                catch (Exception ex)
                {
                    Log("Fatal error: " + ex.Message, LogLevel.Error);
                }
                finally
                {
                    Dispatcher.Invoke(() => BtnInstall.IsEnabled = true);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearLog();
        }

        private void ClearLog()
        {
            RichTxtLog.Document.Blocks.Clear();
        }

        private InstallConfig BuildConfig()
        {
            return Dispatcher.Invoke(() => new InstallConfig
            {
                IsCloud        = ChkCloudCustomer.IsChecked == true,
                InstallRms     = ChkRms.IsChecked == true,
                InstallJms     = ChkJms.IsChecked == true,
                InstallMfr     = ChkMfr.IsChecked == true,
                InstallCad     = ChkCad.IsChecked == true,
                InstallOsmct   = ChkOsmct.IsChecked == true,

                ShortcutPath       = TxtShortcutPath.Text.TrimEnd('\\', '/'),
                RmsJmsPath         = TxtRmsJmsPath.Text.TrimEnd('\\', '/'),
                MoblanPath         = TxtMoblanPath.Text.TrimEnd('\\', '/'),
                MupdatePath        = TxtMupdatePath.Text.TrimEnd('\\', '/'),
                OsmctInstallPath   = TxtOsmctInstallPath.Text.TrimEnd('\\', '/'),
                SharedPath         = TxtSharedPath.Text.TrimEnd('\\', '/'),
                ReportViewerPath   = TxtReportViewerPath.Text.TrimEnd('\\', '/'),
                CadPath            = TxtCadPath.Text.TrimEnd('\\', '/'),
                CadCloudPath       = TxtCadCloudPath.Text.TrimEnd('\\', '/'),

                SuperionPath   = TxtSuperionPath.Text.TrimEnd('\\', '/'),
                MobfilesPath   = TxtMobfilesPath.Text.TrimEnd('\\', '/'),
                FoxtmpPath     = TxtFoxtmpPath.Text.TrimEnd('\\', '/'),
                OssimobPath    = TxtOssimobPath.Text.TrimEnd('\\', '/'),

                RmsShortcutName       = TxtRmsName.Text,
                RmsCloudShortcutName  = TxtRmsCloudName.Text,
                JmsShortcutName       = TxtJmsName.Text,
                JmsCloudShortcutName  = TxtJmsCloudName.Text,
                MfrShortcutName       = TxtMfrName.Text,
                MfrCloudShortcutName  = TxtMfrCloudName.Text,
                CadShortcutName       = TxtCadName.Text,
                OsmctShortcutName     = TxtOsmctName.Text,
                OsmctCloudShortcutName = TxtOsmctCloudName.Text,
            });
        }

        // ── Entry point ──────────────────────────────────────────────────────────

        private void RunInstall(InstallConfig cfg)
        {
            if (!ValidatePaths(cfg))
                return;

            if (cfg.InstallRms)
                SetupRms(cfg);
            else
                Log("Install RMS set to false. Skipping.", LogLevel.Warning);

            if (cfg.InstallJms)
                SetupJms(cfg);
            else
                Log("Install JMS set to false. Skipping.", LogLevel.Warning);

            if (cfg.InstallMfr)
                SetupMfr(cfg);
            else
                Log("Install MFR set to false. Skipping.", LogLevel.Warning);

            if (cfg.InstallCad)
                SetupCad(cfg);
            else
                Log("Install CAD set to false. Skipping.", LogLevel.Warning);

            if (cfg.InstallOsmct)
                SetupOsmct(cfg);
            else
                Log("Install OSMCT set to false. Skipping.", LogLevel.Warning);

            Log("Done.", LogLevel.Success);
        }

        // ── Validation ───────────────────────────────────────────────────────────

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
                    if (cfg.IsCloud)
                        AssertPath(cfg.CadCloudPath, "CAD Cloud Installer Path");
                    else
                        AssertPath(cfg.CadPath, "CAD Path (on-prem)");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Configuration error: " + ex.Message, LogLevel.Error);
                Log("Please fix the path and try again.", LogLevel.Error);
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

            InstallMsi(Path.Combine(cfg.SharedPath, "ONESolution RMS and JMS Components.msi"),
                "RMS and JMS Components");
            InstallExe(Path.Combine(cfg.SharedPath, "vc_redist.x64.exe"), "/S /v/qn",
                "VC Redistributable");
            InstallOdbc(cfg.SharedPath);

            EnsureFolder(cfg.FoxtmpPath);
            EnsureFolder(cfg.SuperionPath);
        }

        private void SetupJms(InstallConfig cfg)
        {
            string name = cfg.IsCloud ? cfg.JmsCloudShortcutName : cfg.JmsShortcutName;
            CreateShortcut(cfg.ShortcutPath, name,
                Path.Combine(cfg.RmsJmsPath, "onesolutionjms.exe"), cfg.RmsJmsPath);

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
            InstallExe(Path.Combine(cfg.ReportViewerPath, "reportviewer.exe"), "/S /v/qn",
                "Report Viewer");

            if (cfg.IsCloud)
            {
                InstallExe(cfg.CadCloudPath, "/S /v/qn", "CAD Cloud Workstation Installer");
            }
            else
            {
                CreateShortcut(cfg.ShortcutPath, cfg.CadShortcutName,
                    Path.Combine(cfg.CadPath, "onesolutioncad.exe"), cfg.CadPath);
                EnsureFolder(cfg.FoxtmpPath);
            }
        }

        private void SetupOsmct(InstallConfig cfg)
        {
            Log("Installing OSMCT...", LogLevel.Info);
            int code = RunProcess(
                Path.Combine(cfg.OsmctInstallPath, "onesolutionmctsetup.exe"), "/S /v/qn");
            if (code == 0)
                Log("OSMCT installer completed successfully.", LogLevel.Success);
            else
                Log($"OSMCT installer exited with code {code}.", LogLevel.Warning);

            string name = cfg.IsCloud ? cfg.OsmctCloudShortcutName : cfg.OsmctShortcutName;
            CreateShortcut(cfg.ShortcutPath, name,
                @"C:\ossimob\osupdater.exe", @"C:\ossimob");

            EnsureFolder(cfg.FoxtmpPath);
            EnsureFolder(cfg.SuperionPath);
            EnsureFolder(cfg.OssimobPath);
            EnsureFolder(cfg.MobfilesPath);

            Log("Launching mupdate...", LogLevel.Info);
            try
            {
                Process.Start(Path.Combine(cfg.MupdatePath, "mupdate.exe"));
            }
            catch (Exception ex)
            {
                Log("Failed to launch mupdate: " + ex.Message, LogLevel.Error);
            }
        }

        // ── Installer helpers ─────────────────────────────────────────────────────

        private void InstallMsi(string msiPath, string displayName)
        {
            Log($"Installing {displayName}...", LogLevel.Info);
            int code = RunProcess("msiexec.exe", $"/i \"{msiPath}\" /qn /norestart");
            ReportExitCode(code, displayName);
        }

        private void InstallOdbc(string sharedPath)
        {
            Log("Installing MS ODBC SQL Driver 17...", LogLevel.Info);
            int code = RunProcess("msiexec.exe",
                $"/i \"{Path.Combine(sharedPath, "msodbcsql64_17.msi")}\" /qn /norestart IACCEPTMSODBCSQLLICENSETERMS=YES");
            ReportExitCode(code, "ODBC Driver 17");
        }

        private void InstallExe(string exePath, string args, string displayName)
        {
            Log($"Installing {displayName}...", LogLevel.Info);
            int code = RunProcess(exePath, args);
            ReportExitCode(code, displayName);
        }

        private void ReportExitCode(int code, string name)
        {
            if (code == 1638)
                Log($"{name} is already installed.", LogLevel.Success);
            else if (code == 1603)
                Log($"Newer version of {name} is already installed.", LogLevel.Success);
            else if (code == 0)
                Log($"{name} installed successfully.", LogLevel.Success);
            else
                Log($"{name} installer exited with code {code}.", LogLevel.Warning);
        }

        private int RunProcess(string fileName, string arguments = "")
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (Process proc = Process.Start(psi))
            {
                if (proc == null)
                {
                    Log($"Failed to start process: {fileName}", LogLevel.Error);
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
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(lnkPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = workingDir;
                shortcut.Save();
                Log($"Shortcut created: {lnkPath}", LogLevel.Success);
            }
            catch (Exception ex)
            {
                Log($"Failed to create shortcut '{name}': {ex.Message}", LogLevel.Error);
            }
        }

        // ── Folder creation + permissions ─────────────────────────────────────────

        private void EnsureFolder(string folderPath)
        {
            try
            {
                Log($"Checking folder: {folderPath}", LogLevel.Info);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    Log($"Created folder: {folderPath}", LogLevel.Success);
                }

                SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
                DirectorySecurity acl = dirInfo.GetAccessControl();
                FileSystemAccessRule rule = new FileSystemAccessRule(
                    everyone,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);
                acl.AddAccessRule(rule);
                dirInfo.SetAccessControl(acl);

                Log($"Granted Full Control to Everyone on: {folderPath}", LogLevel.Success);
            }
            catch (Exception ex)
            {
                Log($"Failed to configure folder '{folderPath}': {ex.Message}", LogLevel.Error);
            }
        }

        // ── Logging ───────────────────────────────────────────────────────────────

        private enum LogLevel { Info, Success, Warning, Error }

        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            Dispatcher.Invoke(() =>
            {
                Brush brush;
                if (level == LogLevel.Success)
                    brush = new SolidColorBrush(Color.FromRgb(87, 200, 105));
                else if (level == LogLevel.Warning)
                    brush = new SolidColorBrush(Color.FromRgb(220, 160, 60));
                else if (level == LogLevel.Error)
                    brush = new SolidColorBrush(Color.FromRgb(230, 80, 80));
                else
                    brush = new SolidColorBrush(Color.FromRgb(100, 180, 240));

                Run run = new Run($"[{DateTime.Now:HH:mm:ss}] {message}") { Foreground = brush };
                Paragraph para = new Paragraph(run) { Margin = new Thickness(0) };
                RichTxtLog.Document.Blocks.Add(para);
                RichTxtLog.ScrollToEnd();
            });
        }
    }
}
