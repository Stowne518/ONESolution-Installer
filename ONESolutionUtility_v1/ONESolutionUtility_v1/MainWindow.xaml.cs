using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ONESolutionUtility_v1
{
    public partial class MainWindow : Window
    {
        private readonly WorkstationInstaller _installer;

        public MainWindow()
        {
            InitializeComponent();
            MaxHeight  = SystemParameters.WorkArea.Height;
            _installer = new WorkstationInstaller(Log);
        }

        // ── Button handlers ───────────────────────────────────────────────────────

        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            BtnInstall.IsEnabled = false;
            ClearLog();

            InstallConfig config = BuildConfig();

            Thread thread = new Thread(() =>
            {
                try
                {
                    _installer.RunInstall(config);
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

        private void GenFilePaths_Click(object sender, RoutedEventArgs e) => FillPaths();

        private void BtnClear_Click(object sender, RoutedEventArgs e) => ClearLog();
		private void BtnPreInstallCloudOsmct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CopyDir(FileServerFQDN.Text + "\\filesync\\rms\\mobmast\\", Migration_Txtossimobcloudpath.Text);
            } 
            catch (Exception ex)
            {
                Log($"Cloud OSMCT not successfully installed: {ex.Message}", LogLevel.Error);
            }
		}

        private void BtnInstallCloudOsmct_Click(object sender, RoutedEventArgs e)
        {
            BtnInstallCloudOsmct.IsEnabled = false;
            string ossimobPath = Migration_Txtossimobpath.Text;
            string ossimobCloudPath = Migration_Txtossimobcloudpath.Text;

            // TODO: Create more protection around existing folders. The Directory.Move method doesn't like it when a directory already exists.
            // Need a backup to use the CopyDir and delete the old one if the folder is already there or something similar.
            Thread thread = new Thread(() =>
            {
                try
                {
                    // 1. Move current ossimob to ossimob_backup
                    RenameDir(ossimobPath, ossimobPath + "_backup");

                    // 2. If we succeed, move ossimob_cloud to ossimob
                    RenameDir(ossimobCloudPath, ossimobPath);
                }
                catch (Exception ex)
                {
                    Log($"Cloud OSMCT not successfully installed: {ex.Message}", LogLevel.Error);
                }
                finally
                {
                    Dispatcher.Invoke(() => BtnInstallCloudOsmct.IsEnabled = true);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

		// ── Cloud customer toggle ─────────────────────────────────────────────────

		private void ChkCloudCustomer_Checked(object sender, RoutedEventArgs e)
        {
            if (FileServerGroup == null) return;

            FileServerGroup.Header              = "FileSync";
            AnimateColumnWidth(FileSyncServerGrid.ColumnDefinitions[1], 600);
            RmsLabel.Visibility                 = Visibility.Collapsed;
            CadLabel.Visibility                 = Visibility.Collapsed;
            TxtRMSFolder.Text                   = "";
            TxtRMSFolder.Visibility             = Visibility.Collapsed;
            TxtCADFolder.Text                   = "";
            TxtCADFolder.Visibility             = Visibility.Collapsed;
            BtnPreInstallCloudOsmct.IsEnabled   = true;
            BtnInstallCloudOsmct.IsEnabled      = true;

		}

        private void ChkCloudCustomer_Unchecked(object sender, RoutedEventArgs e)
        {
            if (FileServerGroup == null) return;

            FileServerGroup.Header              = "FileShare";
            AnimateColumnWidth(FileSyncServerGrid.ColumnDefinitions[1], 120);
            RmsLabel.Visibility                 = Visibility.Visible;
            CadLabel.Visibility                 = Visibility.Visible;
            TxtRMSFolder.Visibility             = Visibility.Visible;
            TxtCADFolder.Visibility             = Visibility.Visible;
			BtnPreInstallCloudOsmct.IsEnabled   = false;
			BtnInstallCloudOsmct.IsEnabled      = false;
		}

        private InstallConfig BuildConfig()
        {
            return new InstallConfig
            {
                IsCloud      = ChkCloudCustomer.IsChecked == true,
                InstallRms   = ChkRms.IsChecked == true,
                InstallJms   = ChkJms.IsChecked == true,
                InstallMfr   = ChkMfr.IsChecked == true,
                InstallCad   = ChkCad.IsChecked == true,
                InstallOsmct = ChkOsmct.IsChecked == true,

                ShortcutPath     = TxtShortcutPath.Text.TrimEnd('\\', '/'),
                RmsJmsPath       = TxtRmsJmsPath.Text.TrimEnd('\\', '/'),
                MoblanPath       = TxtMoblanPath.Text.TrimEnd('\\', '/'),
                MupdatePath      = TxtMupdatePath.Text.TrimEnd('\\', '/'),
                OsmctInstallPath = TxtOsmctInstallPath.Text.TrimEnd('\\', '/'),
                SharedPath       = TxtSharedPath.Text.TrimEnd('\\', '/'),
                ReportViewerPath = TxtReportViewerPath.Text.TrimEnd('\\', '/'),
                CadPath          = TxtCadPath.Text.TrimEnd('\\', '/'),

                SuperionPath = TxtSuperionPath.Text.TrimEnd('\\', '/'),
                MobfilesPath = TxtMobfilesPath.Text.TrimEnd('\\', '/'),
                FoxtmpPath   = TxtFoxtmpPath.Text.TrimEnd('\\', '/'),
                OssimobPath  = TxtOssimobPath.Text.TrimEnd('\\', '/'),

                RmsShortcutName        = TxtRmsName.Text,
                RmsCloudShortcutName   = TxtRmsCloudName.Text,
                JmsShortcutName        = TxtJmsName.Text,
                JmsCloudShortcutName   = TxtJmsCloudName.Text,
                MfrShortcutName        = TxtMfrName.Text,
                MfrCloudShortcutName   = TxtMfrCloudName.Text,
                CadShortcutName        = TxtCadName.Text,
                OsmctShortcutName      = TxtOsmctName.Text,
                OsmctCloudShortcutName = TxtOsmctCloudName.Text,
            };
        }

        private void FillPaths()
        {
            string server = FileServerFQDN.Text;

            if (ChkCloudCustomer.IsChecked == true)
            {
                TxtRmsJmsPath.Text       = $"\\\\{server}\\FileSync\\rms\\onesolutionrms";
                TxtMoblanPath.Text       = $"\\\\{server}\\FileSync\\rms\\moblan\\mfr";
                TxtMupdatePath.Text      = $"\\\\{server}\\FileSync\\rms\\mupdate";
                TxtOsmctInstallPath.Text = $"\\\\{server}\\FileSync\\rms\\mobmast\\onesolutionmct\\setup";
                TxtSharedPath.Text       = $"\\\\{server}\\FileSync\\rms\\shared";
                TxtReportViewerPath.Text = $"\\\\{server}\\FileSync\\cad\\onesolutioncad\\setup";
                TxtCadPath.Text          = $"\\\\{server}\\FileSync\\cad\\std install";
            }
            else
            {
                string rmsFolder = TxtRMSFolder.Text;
                string cadFolder = TxtCADFolder.Text;
                TxtRmsJmsPath.Text       = $"\\\\{server}\\{rmsFolder}\\onesolutionrms";
                TxtMoblanPath.Text       = $"\\\\{server}\\{rmsFolder}\\moblan\\mfr";
                TxtMupdatePath.Text      = $"\\\\{server}\\{rmsFolder}\\mupdate";
                TxtOsmctInstallPath.Text = $"\\\\{server}\\{rmsFolder}\\mobmast\\onesolutionmct\\setup";
                TxtSharedPath.Text       = $"\\\\{server}\\{rmsFolder}\\shared";
                TxtReportViewerPath.Text = $"\\\\{server}\\{cadFolder}\\onesolutioncad\\setup";
                TxtCadPath.Text          = $"\\\\{server}\\{cadFolder}\\std install";
            }
        }

        // ── Logging ───────────────────────────────────────────────────────────────

        private void ClearLog()
        {
			if (MigrationRichTxtLog.IsVisible)
				MigrationRichTxtLog.Document.Blocks.Clear();
            else
				RichTxtLog.Document.Blocks.Clear();
		}

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

                var run  = new Run($"[{DateTime.Now:HH:mm:ss}] {message}") { Foreground = brush };
                var para = new Paragraph(run) { Margin = new Thickness(0) };

                // Figure out 
                if (MigrationRichTxtLog.IsVisible)
                {
                    MigrationRichTxtLog.Document.Blocks.Add(para);
                    MigrationRichTxtLog.ScrollToEnd();
				}
                else
                {
                    RichTxtLog.Document.Blocks.Add(para);
                    RichTxtLog.ScrollToEnd();
                }
            });
        }
        private void CopyDir(string source, string destination)
        {
            try
            {
                // Create the destination directory if it doesn't exist
                if (!Directory.Exists(destination))
                {
                    Log($"'{destination}' does not exist, attempting to create.", LogLevel.Info);
                    Directory.CreateDirectory(destination);
                    Log($"'{destination}' created successfully!", LogLevel.Success);
                }
                else
                {
                    Log($"'{destination}' already exists", LogLevel.Info);
                }

                Log($"Attempting to create all subdirectories", LogLevel.Info);
                // Copy all subdirectories
                foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                {
                    string destDir = dirPath.Replace(source, destination);
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                        Log($"Created '{destDir}'", LogLevel.Success);
					}
                    else
                    {
						Log($"'{destDir}' already exists - skipping.", LogLevel.Info);
					}
				}
                Log("All subdirectories exist. Attempoting file copy now.",LogLevel.Success);

                // Copy all files
                foreach (string filePath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
                {
                    string destFile = filePath.Replace(source, destination);
                    if(File.Exists(destFile))
                    {
                        Log($"'{filePath}' exists in '{destination}' - overwritting.", LogLevel.Info);
                    }
                    File.Copy(filePath, destFile, true);
					Log($"'{filePath}' copied successfully.", LogLevel.Success);
				}

                Log($"Successfully copied '{source}' to '{destination}'!", LogLevel.Success);
            }
            catch (Exception ex)
            {
                Log($"Error during file copy operations: {ex.Message}", LogLevel.Error);
            }
        }

        private void RenameDir(string source, string destination)
        {

            Log($"Attempting to move '{source}' to '{destination}'", LogLevel.Info);
            try
            {
                if (Directory.Exists(destination))
                {
                    Log($"'{destination}' does not exist, attempting to create.", LogLevel.Info);
                    Directory.Move(destination, destination + System.DateTime.Now.ToShortDateString());
                    Log($"'{destination + System.DateTime.Now.ToShortDateString()}' created successfully!", LogLevel.Success);
                }
                else
                {
                    Directory.Move(source, destination);
    				Log($"'{source}' successfully moved to '{destination}'.", LogLevel.Success);
                }
			}
            catch (Exception ex)
            {
                Log($"'{source}' unsuccessfully moved to '{destination}': {ex.Message}.", LogLevel.Error);
            }
        }

        // ── Animation helper ──────────────────────────────────────────────────────

		public void AnimateColumnWidth(ColumnDefinition column, double toWidth, double durationSeconds = 0.3)
		{
			double fromWidth = column.Width.Value;
			double delta = toWidth - fromWidth;
			int frames = (int)(120 * durationSeconds); // 120 FPS
			int currentFrame = 0;
			var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(durationSeconds / frames)
            };

            timer.Tick += (s, e) =>
            {
                currentFrame++;
                double easedProgress = easing.Ease((double)currentFrame / frames);
                column.Width = new GridLength(fromWidth + delta * easedProgress, GridUnitType.Pixel);

                if (currentFrame >= frames)
                {
                    column.Width = new GridLength(toWidth, GridUnitType.Pixel);
                    timer.Stop();
                }
            };

            timer.Start();
        }
    }
}
