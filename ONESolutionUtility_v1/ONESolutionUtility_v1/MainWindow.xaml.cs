using System;
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
            Log("Pre-Install Cloud OSMCT not implemented yet.", LogLevel.Warning);
        }

        private void BtnInstallCloudOsmct_Click(object sender, RoutedEventArgs e)
        {
            Log("Install Cloud OSMCT not implemented yet.", LogLevel.Warning);
        }

        // ── Cloud customer toggle ─────────────────────────────────────────────────

        private void ChkCloudCustomer_Checked(object sender, RoutedEventArgs e)
        {
            if (FileServerGroup == null) return;

            FileServerGroup.Header = "FileSync";
            AnimateColumnWidth(FileSyncServerGrid.ColumnDefinitions[1], 600);
            RmsLabel.Visibility    = Visibility.Collapsed;
            CadLabel.Visibility    = Visibility.Collapsed;
            TxtRMSFolder.Text      = "";
            TxtRMSFolder.Visibility = Visibility.Collapsed;
            TxtCADFolder.Text      = "";
            TxtCADFolder.Visibility = Visibility.Collapsed;
        }

        private void ChkCloudCustomer_Unchecked(object sender, RoutedEventArgs e)
        {
            if (FileServerGroup == null) return;

            FileServerGroup.Header = "FileShare";
            AnimateColumnWidth(FileSyncServerGrid.ColumnDefinitions[1], 120);
            RmsLabel.Visibility     = Visibility.Visible;
            CadLabel.Visibility     = Visibility.Visible;
            TxtRMSFolder.Visibility = Visibility.Visible;
            TxtCADFolder.Visibility = Visibility.Visible;
        }

        // ── Config + path helpers ─────────────────────────────────────────────────

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
                RichTxtLog.Document.Blocks.Add(para);
                RichTxtLog.ScrollToEnd();
            });
        }

        // ── Animation helper ──────────────────────────────────────────────────────

        private void AnimateColumnWidth(ColumnDefinition column, double toWidth, double durationSeconds = 0.3)
        {
            double fromWidth    = column.Width.Value;
            double delta        = toWidth - fromWidth;
            int    frames       = (int)(120 * durationSeconds);
            int    currentFrame = 0;
            var    easing       = new CubicEase { EasingMode = EasingMode.EaseInOut };

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
