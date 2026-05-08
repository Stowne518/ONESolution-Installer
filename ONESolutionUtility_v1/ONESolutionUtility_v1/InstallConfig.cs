namespace ONESolutionUtility_v1
{
    internal class InstallConfig
    {
        public bool IsCloud      { get; set; }
        public bool InstallRms   { get; set; }
        public bool InstallJms   { get; set; }
        public bool InstallMfr   { get; set; }
        public bool InstallCad   { get; set; }
        public bool InstallOsmct { get; set; }

        // Network paths
        public string ShortcutPath     { get; set; }
        public string RmsJmsPath       { get; set; }
        public string MoblanPath       { get; set; }
        public string MupdatePath      { get; set; }
        public string OsmctInstallPath { get; set; }
        public string SharedPath       { get; set; }
        public string ReportViewerPath { get; set; }
        public string CadPath          { get; set; }

        // Local folders
        public string SuperionPath { get; set; }
        public string MobfilesPath { get; set; }
        public string FoxtmpPath   { get; set; }
        public string OssimobPath  { get; set; }

        // Shortcut names
        public string RmsShortcutName        { get; set; }
        public string RmsCloudShortcutName   { get; set; }
        public string JmsShortcutName        { get; set; }
        public string JmsCloudShortcutName   { get; set; }
        public string MfrShortcutName        { get; set; }
        public string MfrCloudShortcutName   { get; set; }
        public string CadShortcutName        { get; set; }
        public string OsmctShortcutName      { get; set; }
        public string OsmctCloudShortcutName { get; set; }
    }
}
