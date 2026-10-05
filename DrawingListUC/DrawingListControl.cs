using ACadSharp;
using ACadSharp.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static ACadSharp.Objects.SortEntitiesTable;
using Application = System.Windows.Forms.Application;
using Color = ACadSharp.Color;

namespace DrawingListUC
{
    public partial class DrawingListControl : UserControl
    {
        // ============ SCRIPT CONSTANTS - PDF VÀ PNG ============
        // ============ SCRIPT CONSTANTS - PDF VÀ PNG ============
        //const string PDF_SCRIPT = @"(setq fileName(substr (getvar 'dwgname) 1 (- (strlen (getvar 'dwgname))4)))(setq fileName (strcat (getvar ""dwgprefix"") filename "".pdf""))filedia 0(command ""-PLOT"" ""YES"" ""<acet:cLayoutName>"" ""Dwg To PDF.pc3"" ""ANSI expand B (11.00 x 17.00 Inches)"" ""Inches"" ""Landscape"" ""NO"" ""Extents"" ""Fit"" ""Center"" ""Yes"" ""."" ""Yes"" ""NO"" ""NO"" ""NO"" filename ""NO"" ""YES"")filedia 1";
        //const string PNG_SCRIPT = @"(setq fileName(substr (getvar 'dwgname) 1 (- (strlen (getvar 'dwgname))4)))(setq fileName (strcat (getvar ""dwgprefix"") filename "".png""))filedia 0(command ""-PLOT"" ""YES"" ""<acet:cLayoutName>"" ""PublishToWeb PNG.pc3"" ""Sun Hi-Res (1600.00 x 1280.00 Pixels)"" ""Landscape"" ""NO"" ""Extents"" ""Fit"" ""Center"" ""YES"" ""."" ""YES"" ""As displayed"" ""YES"" filename ""NO"" ""YES"")filedia 1";
        // ===================================================
        const string PDF_SCRIPT = @"(setq fileName(substr (getvar 'dwgname) 1 (- (strlen (getvar 'dwgname))4)))
        (setq fileName (strcat (getvar ""dwgprefix"") filename "".pdf""))
        filedia
        0
        (command ""-PLOT"" ""YES"" ""<acet:cLayoutName>"" ""Dwg To PDF.pc3"" ""ANSI expand B (11.00 x 17.00 Inches)"" ""Inches"" ""Landscape"" ""NO"" ""Extents"" ""Fit"" ""Center"" ""Yes"" ""."" ""Yes"" ""NO"" ""NO"" ""NO"" filename ""NO"" ""YES"")
        filedia
        1";

        const string PNG_SCRIPT = @"(setq fileName(substr (getvar 'dwgname) 1 (- (strlen (getvar 'dwgname))4)))
        (setq fileName (strcat (getvar ""dwgprefix"") filename "".png""))
        filedia
        0
        (command ""-PLOT"" ""YES"" ""<acet:cLayoutName>"" ""PublishToWeb PNG.pc3"" ""Sun Hi-Res (1600.00 x 1280.00 Pixels)"" ""Landscape"" ""NO"" ""Display"" ""Fit"" ""Center"" ""YES"" ""."" ""YES"" ""As displayed"" ""YES"" filename ""NO"" ""YES"")
        filedia
        1";
        // ===================================================

        private ComboBox _layoutComboBox;
        public DrawingListControl()
        {
            InitializeComponent();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);

            // Add a ComboBox to the ListView for layout selection
            _layoutComboBox = new ComboBox();

            _layoutComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _layoutComboBox.Visible = false;

            DwgList.Controls.Add(_layoutComboBox);

            _layoutComboBox.SelectedIndexChanged += LayoutComboBox_SelectedIndexChanged;
            _layoutComboBox.Leave += LayoutComboBox_Leave;

            // Set default export format
            radioPDF.Checked = true;
        }

        // Holds the host application - WPF application
        private Object _hostApplication = null;

        // Active AutCAD object
        Object acadObject = null;

        // Track if ScriptPro launched AutoCAD (vs attaching to existing)
        private bool _weOwnTheAcadInstance = false;

        // AutoCAD object id (HWND)
        string _acadObjectId = "";

        // Track the PID of AutoCAD that ScriptPro launched (for cleanup/restart)
        int _acadProcessId = -1;

        // Timeout for each drawing
        int _timeoutSec = 10;

        // Start up script path
        string _startUpScript = "";

        // Log file path
        string _logFilePath = "";

        // Hold info about stoping the process
        bool _stopBatchProcess = false;

        //Log file class
        ReportLog bplog = null;

        // Thread which rund the batch process
        BackgroundWorker batchProcessThread;

        // Timeout thread
        BackgroundWorker _timeoutWk;

        // Timer to trigger AutoCAD restart
        System.Timers.Timer _timeout = null;

        // AutoCAD restart count
        int _restartDWGCount = 5;

        // Batch process options
        int _runOption = 0;

        // Progress bar value
        double pbValue = 0.0;

        // File count
        int fileCount = 0;

        // Version in BPL file - for future use...
        //2.0 version
        //3.0 saving runWithoutOpen
        const int BPLVersion = 3;

        // Options
        const int RUN_CHECKED = 0;
        const int RUN_SELECTED = 1;
        const int RUN_FAILED = 2;

        bool _checkAll = false;

        // Variable to flag killing of acad.
        static bool _killAcad = false;

        // To hold filedia & recover mode value
        Object _fd = null;
        Object _rm = null;
        Object _lf = null;

        // Script path to run
        string _scriptPath;

        // Current project opened
        string _projectName = "";

        // ScriptPro product version (from .bpl file header)
        string _scriptProProduct = "";

        bool searchAllDirectories = false;

        bool runWithoutOpen = false;

        int createImage = 2;

        bool diagnosticMode = false;

        //speed of the tool v2.1
        int _toolSpeed = 0;

        // Flag to save the project modification status
        bool _modified = false;

        // Temp color variable
        System.Drawing.Color itemColor;

        // Holds the info on batch process
        bool _isProcessRunning = false;

        // Thread input class.
        ThreadInput Threadinput = new ThreadInput();

        // Export format - PDF or PNG
        private string _exportFormat = "PDF"; // Default PDF

        // Const strings and ints
        const string dwgExt = "dwg";
        const string dxfExt = "dxf";
        const string currentDwg = "Current drawing is : ";
        const string keyFolderName = "<acet:cFolderName>";
        const string keyLayoutName = "<acet:cLayoutName>";
        const string keyBaseName = "<acet:cBaseName>";
        const string keyExtension = "<acet:cExtension>";
        const string keyFileName = "<acet:cFileName>";
        const string keyFullFileName = "<acet:cFullFileName>";

        const int OPEN_NEW_DWG = 1;
        const int CLOSE_DWG_SUCCESS = 2;
        const int CLOSE_DWG_FAILED = 3;

        // AutoCAD location and size.
        static int _left = 0;
        static int _top = 0;
        static int _width = 0;
        static int _height = 0;

        static string _currentDWG;
        static bool _imageCreated;

        //AutoCAD exe to run before starting the application
        //using ActiveX API.
        string acadExePath = "";

        //to run the selected version of application
        bool runSelectedExe = false;

        //to exit the application without showing the logfile
        bool silentExit = false;

        //to hold information on running script as commandline argument
        bool useCmdLine = false;

        //wizard mode
        bool wizardMode = false;

        // Some properties for the host application to use
        public bool Modified
        {
            set { _modified = value; }
            get { return _modified; }
        }

        public string ProjectName
        {
            set { _projectName = value; }
            get { return _projectName; }
        }

        public Object HostApplication
        {
            set { _hostApplication = value; }
            get { return _hostApplication; }
        }

        #region UserInterface Members

        // Resize the controls
        private void DwgList_SizeChanged(object sender, EventArgs e)
        {
            // Control resize, set the col widths...
            int width = DwgList.Width;

            if (wizardMode)
            {
                // DWG name
                //DwgList.Columns[0].Width = (int)(width * 0.23);

                //// Path
                //DwgList.Columns[1].Width = (int)(width * 0.73);
            }
            else
            {
                // DWG name
                DwgList.Columns[0].Width = (int)(width * 0.25);
                // Path
                DwgList.Columns[1].Width = (int)(width * 0.55);
                // Status
                DwgList.Columns[2].Width = (int)(width * 0.10);
                // Select Layout
                DwgList.Columns[3].Width = (int)(width * 0.10);
            }
        }

        public void DoInitialize()
        {
            // Make the process bar hidden by default
            BPbar.Visible = false;
            label_filename.Visible = false;

            ApplySettings();

            // Check the command line
            bool fileFound = false;
            bool startProcess = false;
            silentExit = false;

            string command = Environment.CommandLine;

            if (command.ToLower().Contains(".bpl"))
            {
                string[] args = Environment.GetCommandLineArgs();

                string strBPLname = "";
                foreach (string arg in args)
                {
                    string argLower = arg.ToLower();

                    // Skip the executable name (.exe or .dll)
                    if (argLower.Contains(".exe") || argLower.Contains(".dll"))
                        continue;

                    if (!fileFound)
                    {
                        if (argLower.Contains(".bpl"))
                        {
                            // Found .bpl file - use original casing, not lowercase
                            strBPLname = strBPLname.TrimEnd() + arg;
                            fileFound = true;
                        }
                        else
                        {
                            // Building path with spaces - use original casing
                            if (strBPLname.Length == 0)
                                strBPLname = arg + " ";
                            else
                                strBPLname = strBPLname + arg + " ";
                        }
                    }
                    else
                    {
                        // After finding .bpl, check for "run" and "exit" commands
                        if (argLower.Contains("run"))
                            startProcess = true;

                        if (argLower.Contains("exit"))
                            silentExit = true;
                    }
                }

                if (File.Exists(strBPLname))
                {
                    loadDWGList(strBPLname);
                    updateControls();

                    if (startProcess)
                    {
                        // Delay execution until UI is fully loaded
                        System.Windows.Forms.Timer startTimer = new System.Windows.Forms.Timer();
                        startTimer.Interval = 500; // 500ms delay
                        startTimer.Tick += (s, ev) =>
                        {
                            startTimer.Stop();
                            startTimer.Dispose();
                            runCheckedFiles();
                        };
                        startTimer.Start();
                    }
                }
                else
                {
                    MessageBox.Show(
                        $"Could not find project file:\n{strBPLname}\n\n" +
                        $"Command line: {command}\n\n" +
                        $"Parsed file: '{strBPLname}'",
                        "ScriptPro - File Not Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        void updateControls()
        {
            try
            {
                // Set radio buttons based on export format
                if (_exportFormat == "PDF")
                    radioPDF.Checked = true;
                else if (_exportFormat == "PNG")
                    radioPNG.Checked = true;
            }
            catch
            {
                _exportFormat = "PDF";
                _timeoutSec = 30;
                _startUpScript = "";
                _restartDWGCount = 30;
            }
        }

        public void ApplySettings()
        {
            _exportFormat = "PDF";
            _timeoutSec = 30;
            _startUpScript = "";
            _restartDWGCount = 30;

            string str = Properties.Settings.Default.SearchAllDirectories;
            searchAllDirectories = !str.Contains("false");

            str = Properties.Settings.Default.CreateImage;

            if (str.Contains("1"))
                createImage = 1;
            else if (str.Contains("0"))
                createImage = 0;
            else
                createImage = 2;
        }

        public void AddDWGFilesFromFolder()
        {
            FolderBrowserDialog folderdg = new FolderBrowserDialog();
            folderdg.ShowNewFolderButton = false;
            if (folderdg.ShowDialog() ==
                DialogResult.OK)
            {
                SearchOption fileselection =
                  SearchOption.TopDirectoryOnly;
                if (searchAllDirectories)
                    fileselection =
                      SearchOption.AllDirectories;

                string[] dwgFiles =
                  Directory.GetFiles(
                    folderdg.SelectedPath + "\\",
                    "*." + dwgExt,
                    fileselection
                  );

                foreach (string fileName in dwgFiles)
                {
                    AddDWGtoView(fileName, true);
                }

                string[] dxfFiles =
                  Directory.GetFiles(
                    folderdg.SelectedPath + "\\",
                    "*." + dxfExt,
                    SearchOption.AllDirectories
                  );

                foreach (string fileName in dxfFiles)
                {
                    AddDWGtoView(fileName, true);
                }

                _modified = true;
            }
        }

        public void AddDWGFiles()
        {
            OpenFileDialog BPFileOpenDlg =
              new OpenFileDialog();
            BPFileOpenDlg.Filter = "Drawing Files (*.dwg, *.dxf)|*.dwg;*.dxf";
            BPFileOpenDlg.Multiselect = true;
            BPFileOpenDlg.Title = "Select files to add";

            if (BPFileOpenDlg.ShowDialog() == DialogResult.OK)
            {
                string[] FileNames =
                  BPFileOpenDlg.FileNames;

                foreach (string fileName in FileNames)
                {
                    AddDWGtoView(fileName, true);
                }

                _modified = true;
            }
        }

        private void ContextDWGAddFile_Click
            (object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem =
              sender as ToolStripMenuItem;

            if (menuItem.Name == "DWGAddFile" ||
                menuItem.Name == "ContextDWGAddFile")
                AddDWGFiles();
            else
                AddDWGFilesFromFolder();
        }

        private void ContextDWGAddFolder_Click(
          object sender, EventArgs e
        )
        {
            AddDWGFilesFromFolder();
        }

        private void AddDWGtoView(string fileName, bool bCheck)
        {
            string name = Path.GetFileName(fileName);
            //List<string> layouts = DwgLayoutReader.GetLayouts(fileName);
            // string path = Path.GetDirectoryName(fileName);

            bool add = true;
            foreach (ListViewItem addedItem in DwgList.Items)
            {
                TagData tag = (TagData)addedItem.Tag;
                if (tag.DwgName == fileName)
                {
                    add = false;
                    break;
                }
            }

            if (add)
            {
                ListViewItem item = new ListViewItem(name, 0);
                item.Checked = bCheck;
                item.SubItems.Add(fileName);
                item.SubItems.Add("");
                item.SubItems.Add("");
                TagData tag = new TagData();
                tag.DwgName = fileName;
                tag.Layouts = DwgLayoutReader.GetLayouts(fileName);
                if (tag.Layouts.Count > 0)
                {
                    tag.SelectedLayout = tag.Layouts[0];
                    item.SubItems[3].Text = tag.SelectedLayout;
                }
                item.Tag = tag;
                DwgList.Items.Add(item);
            }
        }

        // Enable/disable the context menu items
        private void DwgContextMenu_Opening(
          object sender, CancelEventArgs e
        )
        {
            if (_isProcessRunning)
            {
                e.Cancel = true;
                return;
            }

            if (wizardMode)
            {
                DwgContextMenu.Items[8].Visible = false;
                DwgContextMenu.Items[7].Visible = false;
                DwgContextMenu.Items[6].Visible = false;
                DwgContextMenu.Items[5].Visible = false;
                DwgContextMenu.Items[4].Visible = false;
                DwgContextMenu.Items[3].Visible = false;
                DwgContextMenu.Items[2].Visible = false;
            }
            else
            {
                ToolStripMenuItem runStrip =
                  DwgContextMenu.Items[8] as ToolStripMenuItem;

                if (DwgList.SelectedItems.Count == 0)
                {
                    DwgContextMenu.Items[0].Enabled = true;
                    DwgContextMenu.Items[1].Enabled = false;
                    DwgContextMenu.Items[2].Enabled = false;

                    runStrip.DropDownItems[1].Enabled = false;
                }
                else
                {
                    DwgContextMenu.Items[0].Enabled = false;
                    DwgContextMenu.Items[1].Enabled = true;

                    runStrip.DropDownItems[1].Enabled = true;

                    ListView.SelectedListViewItemCollection selItems =
                      DwgList.SelectedItems;

                    ListViewItem firstItem = null;
                    bool bEnabled = true;

                    foreach (ListViewItem item in selItems)
                    {
                        if (firstItem == null)
                            firstItem = item;

                        if (firstItem.Checked != item.Checked)
                            bEnabled = false;
                    }

                    DwgContextMenu.Items[2].Enabled = bEnabled;

                    if (firstItem != null)
                    {
                        if (firstItem.Checked)
                            DwgContextMenu.Items[2].Text = "Skip";
                        else
                            DwgContextMenu.Items[2].Text = "Include";
                    }
                }

                runStrip.DropDownItems[0].Enabled =
                  (DwgList.CheckedItems.Count != 0);

                bool enabled = false;
                foreach (ListViewItem item in DwgList.Items)
                {
                    TagData data = (TagData)item.Tag;

                    if (!data.status)
                    {
                        enabled = true;
                        break;
                    }
                }

                runStrip.DropDownItems[2].Enabled = enabled;

                if (DwgList.Items.Count == 0)
                {
                    DwgContextMenu.Items[3].Enabled = false;
                    DwgContextMenu.Items[8].Enabled = false;
                }
                else
                {
                    DwgContextMenu.Items[3].Enabled = true;
                    DwgContextMenu.Items[8].Enabled = true;
                }
            }
        }

        // Remove the selected DWG
        public void RemoveSelectedDWG()
        {
            // Remove the drawings from the list control

            ListView.SelectedListViewItemCollection selItems =
              DwgList.SelectedItems;

            foreach (ListViewItem item in selItems)
            {
                // Remove the item from listview
                DwgList.Items.Remove(item);
            }

            _modified = true;
        }

        //
        private void RemoveDWG_Click(object sender, EventArgs e)
        {
            RemoveSelectedDWG();
        }

        // Mark the selected DWG as skip
        public void SkipSelectedDWG()
        {
            ListView.SelectedListViewItemCollection selItems =
              DwgList.SelectedItems;

            foreach (ListViewItem item in selItems)
            {
                // Remove the item from listview
                item.Checked = !item.Checked;
            }
            _modified = true;
        }

        //
        private void DwgList_ItemChecked(
          object sender, ItemCheckedEventArgs e
        )
        {
            _modified = true;
        }

        private void SkipDWG_Click(object sender,
            EventArgs e)
        {
            SkipSelectedDWG();
        }

        private void chToolStripMenuItem_Click(
          object sender, EventArgs e
        )
        {
            foreach (ListViewItem item in DwgList.Items)
            {
                item.Checked = _checkAll;
            }
            _checkAll = !_checkAll;
        }

        // Context menu options
        private void saveDWGListToolStripMenuItem_Click(
          object sender, EventArgs e
        )
        {
            saveDWGList(false);
        }

        public void setOptions()
        {
            OptionsDlg dlg = new OptionsDlg();

            dlg.setProjectSetting(
              _startUpScript, _timeoutSec.ToString(),
              _logFilePath, _restartDWGCount.ToString()
            );

            dlg.DiagnosticMode = diagnosticMode;
            dlg.toolSpeed = Convert.ToInt32(_toolSpeed * 0.001);
            dlg.acadExePath = acadExePath;
            dlg.RunWithoutOpen = runWithoutOpen;
            dlg.UseScriptAsCmdLine = useCmdLine;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _startUpScript = dlg.IniScript;
                _timeoutSec = dlg.timeout;
                _logFilePath = dlg.logFilePath;
                _restartDWGCount = dlg.reStartCount;
                searchAllDirectories = dlg.SearchAllDirectories;
                runWithoutOpen = dlg.RunWithoutOpen;

                createImage = dlg.nCreateImage;
                diagnosticMode = dlg.DiagnosticMode;
                _toolSpeed = dlg.toolSpeed * 1000;

                acadExePath = dlg.acadExePath;

                if (acadExePath.Length != 0)
                    runSelectedExe = true;

                useCmdLine = dlg.UseScriptAsCmdLine;

                _modified = true;
            }
        }

        #endregion

        #region ReadDrawingList Members

        // ============ ghi file temp ============
        private bool CreateScriptFile()
        {
            try
            {
                if (string.IsNullOrEmpty(_exportFormat))
                {
                    MessageBox.Show(
                        "Hãy chọn định dạng export (PDF hoặc PNG).",
                        "ScriptPro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }

                // Xác định script content dựa vào format
                string scriptContent = (_exportFormat == "PDF") ? PDF_SCRIPT : PNG_SCRIPT;

                // Tạo file script tạm thời
                _scriptPath = Path.Combine(Path.GetTempPath(), $"ScriptPro_Export_{_exportFormat}_{Guid.NewGuid().ToString().Substring(0, 8)}.scr");

                using (StreamWriter writer = new StreamWriter(_scriptPath, false, Encoding.UTF8))
                {
                    writer.Write(scriptContent);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo script file: {ex.Message}", "ScriptPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        // =============================================

        // Event handler cho Radio Buttons
        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (radioPDF.Checked)
            {
                _exportFormat = "PDF";
                _modified = true;
            }
            else if (radioPNG.Checked)
            {
                _exportFormat = "PNG";
                _modified = true;
            }
        }

        // Read the BPL file  
        private void readGeneralSection(StreamReader SR)
        {
            string[] lines;
            try
            {
                // General_Start
                string version = "";
                string linetext = SR.ReadLine();

                // Read version, 
                linetext = SR.ReadLine();
                lines = linetext.Split('*');
                version = lines[1];

                // Read product
                linetext = SR.ReadLine();
                lines = linetext.Split('*');
                _scriptProProduct = lines[1];

                // Read export format (thay vì Script)
                linetext = SR.ReadLine();
                lines = linetext.Split('*');
                _exportFormat = lines[1];

                // Read timeout file
                linetext = SR.ReadLine();
                lines = linetext.Split('*');
                _timeoutSec = Convert.ToInt32(lines[1]);

                // Read ReStart file
                linetext = SR.ReadLine();
                lines = linetext.Split('*');
                _restartDWGCount = Convert.ToInt32(lines[1]);

                // Read start up file
                linetext = SR.ReadLine();
                lines = linetext.Split('*');
                _startUpScript = lines[1];

                // Read log file
                linetext = SR.ReadLine();
                lines = linetext.Split('*');
                _logFilePath = lines[1];

                Int32 _ver = Convert.ToInt32(version);
                if (_ver >= 2) //for version 2 and higher
                {
                    //read the AutoCAD exe path
                    linetext = SR.ReadLine();
                    lines = linetext.Split('*');

                    string exePath = lines[1];
                    acadExePath = exePath;

                    // If .bpl file has a specific AutoCAD path, we should use it
                    if (!string.IsNullOrWhiteSpace(acadExePath))
                    {
                        runSelectedExe = true;
                    }

                    //read the sleep amount
                    linetext = SR.ReadLine();
                    lines = linetext.Split('*');
                    _toolSpeed = Convert.ToInt32(lines[1]);
                }

                //for version 3 and higher
                runWithoutOpen = false;
                if (_ver >= 3)
                {
                    //check if the script to run on empty drawing
                    linetext = SR.ReadLine();
                    lines = linetext.Split('*');

                    string withOutfileOpen = lines[1];
                    runWithoutOpen = Convert.ToBoolean(withOutfileOpen);
                }

                useCmdLine = isHeadlessAcad(acadExePath);

                // Read General_End
                linetext = SR.ReadLine();
            }
            catch { }
        }

        // Load script pro project file...
        public void loadFromSCPfile()
        {
            // Clear the drawing list first...
            DwgList.Items.Clear();

            OpenFileDialog openDlg = new OpenFileDialog();
            openDlg.Filter =
              "ScriptPro project files (*.scp) |*.scp;";
            openDlg.Title =
              "Load ScriptPro project files";

            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                StreamReader SR = File.OpenText(openDlg.FileName);
                string linetext = "";
                string[] lines;

                // General
                linetext = SR.ReadLine();

                // Complete
                linetext = SR.ReadLine();

                // Script file
                linetext = SR.ReadLine();
                lines = linetext.Split('=');
                _scriptPath = lines[1];

                // Timeout
                linetext = SR.ReadLine();
                lines = linetext.Split('=');
                _timeoutSec = Convert.ToInt32(lines[1]);

                // Log...
                linetext = SR.ReadLine();

                // Log file name
                linetext = SR.ReadLine();
                lines = linetext.Split('=');

                _logFilePath = Path.GetDirectoryName(lines[1]);

                // Use UNC
                linetext = SR.ReadLine();

                // Read drawings...
                linetext = SR.ReadLine();
                while (linetext != null)
                {
                    if (linetext.Length != 0)
                    {
                        if (!linetext.Contains("[FileList]"))
                        {
                            lines = linetext.Split('\t');
                            string DWG = lines[1] + lines[0];

                            bool bcheeck = true;
                            if (lines[2].Contains("Skip"))
                            {
                                bcheeck = false;
                            }
                            AddDWGtoView(DWG, bcheeck);
                        }
                    }
                    linetext = SR.ReadLine();
                }
            }
        }

        // New list...
        public void newDWGList()
        {
            // Clear the drawing list
            DwgList.Items.Clear();

            // New project
            _projectName = "";
            _modified = false;
            acadExePath = "";
            runWithoutOpen = false;
            useCmdLine = false;
            _exportFormat = "PDF";
            radioPDF.Checked = true;
        }

        // Loads the passed drawing (bpl) file
        public void loadDWGList(string filename)
        {
            StreamReader SR = File.OpenText(filename);
            string linetext = "";
            try
            {
                readGeneralSection(SR);
            }
            catch
            {
                return;
            }

            string[] lines;

            // DWGList_Start
            linetext = SR.ReadLine();

            // First drawing
            linetext = SR.ReadLine();
            while (linetext != null)
            {
                if (linetext.Contains("DWGList_End"))
                    break;

                lines = linetext.Split(',');

                if (lines.Length == 2)
                {
                    if (Convert.ToInt32(lines[1]) == 0)
                        AddDWGtoView(lines[0], false);
                    else
                        AddDWGtoView(lines[0], true);
                }
                if (lines.Length == 1)
                {
                    AddDWGtoView(lines[0], true);
                }

                linetext = SR.ReadLine();
            }
            SR.Close();

            ProjectName = filename;
            _modified = false;
        }

        // Ask the user for the bpl file to load
        public void loadDWGList()
        {
            // Clear the drawing list first...
            OpenFileDialog openDlg = new OpenFileDialog();
            openDlg.Filter = "Drawing list (*.bpl) |*.bpl;";
            openDlg.Title = "Drawing list";

            if (File.Exists(_projectName))
                openDlg.InitialDirectory = Path.GetDirectoryName(_projectName);
            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                DwgList.Items.Clear();
                runWithoutOpen = false;
                useCmdLine = false;

                loadDWGList(openDlg.FileName);
            }
        }

        // Context menu options
        private void loadDWGListToolStripMenuItem_Click(
          object sender, EventArgs e
        )
        {
            loadDWGList();
        }

        public void writeDWGList(string strProjectName, bool failed)
        {
            try
            {
                StreamWriter sw = File.CreateText(strProjectName);

                // First write all the general infomation
                sw.WriteLine("[General_Start]");
                sw.WriteLine("Version*" + BPLVersion.ToString());
                sw.WriteLine("Product*" + "3.0");
                // Write export format instead of script path
                sw.WriteLine("ExportFormat*" + _exportFormat);
                sw.WriteLine("TimeOut*" + _timeoutSec.ToString());
                sw.WriteLine("RestartCount*" + _restartDWGCount.ToString());
                sw.WriteLine("IniScript*" + _startUpScript);
                sw.WriteLine("LogFileName*" + _logFilePath);
                sw.WriteLine("AutoCADPath*" + acadExePath);
                sw.WriteLine("Sleep*" + _toolSpeed.ToString());
                sw.WriteLine("RunwithoutOpen*" + runWithoutOpen.ToString());

                sw.WriteLine("[General_End]");

                sw.WriteLine("[DWGList_Start]");
                foreach (ListViewItem item in DwgList.Items)
                {
                    TagData data = (TagData)item.Tag;
                    string strCheck = "0";
                    if (item.Checked)
                    {
                        strCheck = "1";
                    }
                    if (failed)
                    {
                        if (data.status == false)
                            sw.WriteLine(data.DwgName + "," + strCheck);
                    }
                    else
                    {
                        sw.WriteLine(data.DwgName + "," + strCheck);
                    }
                }
                sw.WriteLine("[DWGList_End]");

                sw.Close();
            }
            catch (System.Exception ex)
            {
                if (failed == false)
                    MessageBox.Show(ex.Message);
            }

        }

        // Save the BPL list, called from save as save as
        public void saveDWGList(bool saveAs)
        {
            bool showDialog = false;

            if (saveAs)
            {
                showDialog = true;
            }
            else
            {
                if (_projectName.Length == 0)
                    showDialog = true;
            }

            if (showDialog)
            {
                SaveFileDialog saveDlg = new SaveFileDialog();
                saveDlg.Filter = "Drawing list (*.bpl) |*.bpl;";
                saveDlg.Title = "Drawing list";
                saveDlg.OverwritePrompt = true;

                if (saveDlg.ShowDialog() == DialogResult.OK)
                {
                    _projectName = saveDlg.FileName;
                    writeDWGList(_projectName, false);
                }
            }
            _modified = false;
        }

        #endregion

        #region RunBatchProcess Members

        // Run the batch process for checked files
        public void runCheckedFiles()
        {
            if (DwgList.Items.Count == 0)
                return;

            if (DwgList.CheckedItems.Count == 0)
                return;

            // Remove the list
            Threadinput._FileInfolist.Clear();

            foreach (ListViewItem item in DwgList.Items)
            {
                if (item.Checked)
                {
                    FileInfo info = new FileInfo();
                    TagData data = (TagData)item.Tag;
                    info._fileName = data.DwgName;
                    info._selectedLayout = data.SelectedLayout;
                    info._index = item.Index;
                    Threadinput._FileInfolist.Add(info);
                }
            }

            _stopBatchProcess = false;
            StartBatchProcess(false, RUN_CHECKED);
        }

        // Context menu option
        private void toolStripMenuItem2_Click(
          object sender, EventArgs e
        )
        {
            runCheckedFiles();
        }

        // Run the selected DWG files
        public void runSelectedFiles()
        {
            if (DwgList.Items.Count == 0)
                return;

            if (DwgList.SelectedItems.Count == 0)
            {
                MessageBox.Show(
                          "Try after selecting the required files",
                          "ScriptPro", MessageBoxButtons.OK
                        );

                return;
            }

            // Remove the list
            Threadinput._FileInfolist.Clear();

            ListView.SelectedListViewItemCollection selItems =
              DwgList.SelectedItems;

            foreach (ListViewItem item in selItems)
            {
                FileInfo info = new FileInfo();
                TagData data = (TagData)item.Tag;
                info._fileName = data.DwgName;
                info._selectedLayout = data.SelectedLayout;
                info._index = item.Index;
                Threadinput._FileInfolist.Add(info);
            }
            _stopBatchProcess = false;

            StartBatchProcess(false, RUN_SELECTED);
        }

        // Context menu option to run the selected
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            runSelectedFiles();
        }

        // Run only failed dwg files
        public void runFailedFiles()
        {
            if (DwgList.Items.Count == 0)
                return;

            // Remove the list
            Threadinput._FileInfolist.Clear();

            bool run = false;
            foreach (ListViewItem item in DwgList.Items)
            {
                TagData data = (TagData)item.Tag;
                if (!data.status)
                {
                    FileInfo info = new FileInfo();
                    info._fileName = data.DwgName;
                    info._selectedLayout = data.SelectedLayout;
                    info._index = item.Index;
                    item.SubItems[2].Text = "";
                    item.ForeColor = System.Drawing.Color.Black;
                    Threadinput._FileInfolist.Add(info);
                    run = true;
                }
            }

            if (run)
            {
                _stopBatchProcess = false;
                StartBatchProcess(false, RUN_FAILED);
            }
            else
            {
                MessageBox.Show(
                "No failed files",
                "ScriptPro", MessageBoxButtons.OK
              );
            }
        }

        private void failToolStripMenuItem_Click(
          object sender, EventArgs e
        )
        {
            runFailedFiles();
        }

        public void stopProcess()
        {
            _stopBatchProcess = true;
        }

        #endregion

        #region AutoCAD_related Members

        // Initialize UI to start the batch process
        void Initialize_start(int userOption)
        {
            BPbar.Visible = true;
            label_filename.Visible = true;

            UpdateHostApplicationUI(true);

            //make sure start the selected exe first..
            if (acadExePath.Length != 0)
                runSelectedExe = true;

            if (userOption == RUN_SELECTED)
            {
                ListView.SelectedListViewItemCollection selItems =
                  DwgList.SelectedItems;

                foreach (ListViewItem item in selItems)
                {
                    item.SubItems[2].Text = "";
                    item.ForeColor = System.Drawing.Color.Black;
                }

            }
            else if (userOption == RUN_CHECKED)
            {
                foreach (ListViewItem item in DwgList.CheckedItems)
                {
                    item.SubItems[2].Text = "";
                    item.ForeColor = System.Drawing.Color.Black;
                }
            }
            else if (userOption == RUN_FAILED)
            {
                foreach (ListViewItem item in DwgList.Items)
                {
                    TagData data = (TagData)item.Tag;
                    if (!data.status)
                    {
                        item.SubItems[2].Text = "";
                        item.ForeColor = System.Drawing.Color.Black;
                    }
                }
            }

            DwgList.Refresh();

            if (_timeout == null && !useCmdLine)
            {
                _timeout = new System.Timers.Timer();
                _timeout.AutoReset = false;
                _timeout.Elapsed +=
                  new System.Timers.ElapsedEventHandler(_timeout_Elapsed);
            }

            // Start the timer
            if (_timeoutWk == null && !useCmdLine)
            {
                _timeoutWk = new BackgroundWorker();
                _timeoutWk.DoWork += new DoWorkEventHandler(_timeoutWk_DoWork);
                _timeoutWk.ProgressChanged +=
                  new ProgressChangedEventHandler(
                    _timeoutWk_ProgressChanged
                  );
                _timeoutWk.WorkerReportsProgress = true;
                _timeoutWk.WorkerSupportsCancellation = true;
                _timeoutWk.RunWorkerAsync(null);
            }

            if (batchProcessThread == null)
            {
                batchProcessThread = new BackgroundWorker();
                batchProcessThread.DoWork +=
                  new DoWorkEventHandler(
                    batchProcessThread_DoWork
                  );
                batchProcessThread.RunWorkerCompleted +=
                  new RunWorkerCompletedEventHandler(
                    batchProcessThread_RunWorkerCompleted
                  );
                batchProcessThread.ProgressChanged +=
                  new ProgressChangedEventHandler(
                    batchProcessThread_ProgressChanged
                  );
                batchProcessThread.WorkerReportsProgress = true;
                batchProcessThread.WorkerSupportsCancellation = true;
            }

            this.BPbar.Value = 0;
            label_filename.Text = "";

            if (_logFilePath.Length == 0)
            {

                // Set the user temp directory...
                _logFilePath = Path.GetTempPath();
            }

            try
            {
                if (Directory.Exists(_logFilePath))
                    bplog = new ReportLog(_logFilePath, _projectName);
            }
            catch
            {
                bplog = null;
            }
        }

        //function to check whether application 
        //is a AutoCAD or headless exe (at present accoreconsole.exe)
        static public bool isHeadlessAcad(string strExePath)
        {
            string strFileName = Path.GetFileName(strExePath);
            strFileName = strFileName.ToLower();

            if (strFileName.Length == 0)
                return false;

            if (strFileName.Contains("acad.exe"))
                return false;

            return true;
        }

        /// <summary>
        /// Starts or attaches to AutoCAD based on user settings.
        /// </summary>
        private bool startAutoCAD(bool isRestart = false)
        {
            if (useCmdLine)
                return true;

            // If this is a restart, give extra time for COM cleanup after previous AutoCAD quit
            if (isRestart)
            {
                Thread.Sleep(3000); // Wait for COM cleanup (increased for Release builds)
            }

            _weOwnTheAcadInstance = false;  // Reset ownership flag
            _acadProcessId = -1;  // Reset PID tracking
            int pid = -1;
            try
            {
                // SCENARIO 1: User selected specific exe from wizard (honor this across restarts)
                if (runSelectedExe && !string.IsNullOrWhiteSpace(acadExePath) && File.Exists(acadExePath))
                {
                    //get all running autocad process.
                    HashSet<(int, string)> idNameSet = AcadComUtils.SnapshotAcadPids();
                    //check if any existing AutoCAD process is running with the same exe path.
                    if (idNameSet != null)
                    {
                        var existing = idNameSet.FirstOrDefault(kv =>
                            kv.Item2.Equals(acadExePath, StringComparison.OrdinalIgnoreCase));

                        if (existing.Item1 != 0)
                        {
                            // Found a running AutoCAD with the same version
                            // Attach to existing instance first
                            acadObject = AcadComUtils.TryGetActiveObjectForExePath(acadExePath);
                            _weOwnTheAcadInstance = false;  // User's instance, don't close it
                            _acadProcessId = -1;  // We don't own it, don't track PID

                            if (acadObject != null)
                            {
                                AcadComUtils.SetVisible(acadObject, true);
                                object h = acadObject.GetType().InvokeMember("HWND",
                                    BindingFlags.GetProperty, null, acadObject, null);
                                _acadObjectId = h?.ToString() ?? string.Empty;

                                // Get product name from running COM object (e.g., "AutoCAD Plant 3D 2026")
                                string productName = AcadComUtils.GetProductNameFromExePath(acadExePath);
                                try
                                {
                                    object appName = acadObject.GetType().InvokeMember("Name",
                                        BindingFlags.GetProperty, null, acadObject, null);
                                    if (appName != null)
                                    {
                                        string runningProductName = appName.ToString();
                                        if (!string.IsNullOrWhiteSpace(runningProductName))
                                        {
                                            productName = runningProductName;  // Use actual running product name
                                        }
                                    }
                                }
                                catch { /* Fallback to registry name if COM query fails */ }

                                // Only show dialog on first launch, not during restarts
                                if (!isRestart)
                                {
                                    MessageBox.Show(
                                        $"{productName} is already running (PID: {existing.Item1}).\n\n" +
                                        "ScriptPro will use the existing instance.\n\n" +
                                        "⚠️ IMPORTANT:\n" +
                                        "• AutoCAD will NOT be closed or restarted automatically\n" +
                                        "• The restart counter (if configured) will NOT apply\n" +
                                        "• For large batches, let ScriptPro launch AutoCAD instead\n\n" +
                                        "AutoCAD will remain running after ScriptPro finishes.",
                                        "ScriptPro - Using Existing AutoCAD",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning
                                    );
                                }

                                return true;
                            }
                        }
                    }

                    // No existing same-version instance - launch new
                    pid = AcadComUtils.StartAcadExe(acadExePath);
                    AcadComUtils.WaitForMainWindow(pid, timeoutMs: 90_000);

                    // Wait for AutoCAD to fully initialize and register COM
                    // Longer wait on restart since previous instance just quit
                    Thread.Sleep(isRestart ? 5000 : 3000);

                    // Get the version-specific ProgID for the exe we just launched
                    var acadVersion = AcadComUtils.GetAutoCADVersionFromExe(acadExePath);

                    // Build the exact ProgID for this version (e.g., "AutoCAD.Application.25.1")
                    string progIdToUse = (acadVersion != null)
                        ? $"AutoCAD.Application.{acadVersion.Major}.{acadVersion.Minor}"
                        : "AutoCAD.Application"; // Fallback if version detection failed

                    // Retry attaching to the AutoCAD instance we just launched
                    // Since we launched a specific exe and have its PID, we know exactly what to look for
                    int maxRetries = 15;
                    for (int retry = 0; retry < maxRetries && acadObject == null; retry++)
                    {
                        try
                        {
                            // Get the COM object using the version-specific ProgID
                            acadObject = AcadComUtils.TryGetActiveObject(progIdToUse);

                            if (acadObject != null)
                            {
                                // Verify it's the instance we just launched by checking PID
                                int foundPid = AcadComUtils.GetProcessIdFromComObject(acadObject);
                                if (foundPid == pid)
                                {
                                    // Success! This is our AutoCAD
                                    _weOwnTheAcadInstance = true;
                                    _acadProcessId = pid;
                                    break;
                                }
                                else
                                {
                                    // Wrong instance - this shouldn't happen since we use version-specific ProgID
                                    // But handle it gracefully by continuing retry loop
                                    acadObject = null;
                                }
                            }
                        }
                        catch
                        {
                            // COM not ready yet, will retry
                            acadObject = null;
                        }

                        if (acadObject == null && retry < maxRetries - 1)
                        {
                            Thread.Sleep(2000); // Wait 2 seconds between retries
                        }
                    }

                    // If we failed to attach after all retries, reset ownership
                    if (acadObject == null)
                    {
                        _weOwnTheAcadInstance = false;
                        _acadProcessId = -1;
                    }
                }
                else
                {
                    // SCENARIO 2: No specific EXE selected - try attach to existing, else launch new
                    var anyRunning = AcadComUtils.TryGetAnyRunningAcad();
                    if (anyRunning != null)
                    {
                        acadObject = anyRunning;
                        _weOwnTheAcadInstance = false;  // User's instance
                        _acadProcessId = -1;  // We don't own it, don't track PID

                        // Only show dialog on first launch, not during restarts
                        if (!isRestart)
                        {
                            MessageBox.Show(
                                "ScriptPro will use the existing AutoCAD instance.\n\n" +
                                "⚠️ IMPORTANT:\n" +
                                "• AutoCAD will NOT be closed or restarted automatically\n" +
                                "• The restart counter (if configured) will NOT apply\n" +
                                "• For large batches, let ScriptPro launch AutoCAD instead\n\n" +
                                "AutoCAD will remain running after ScriptPro finishes.",
                                "ScriptPro - Using Existing AutoCAD",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning
                            );
                        }
                    }
                    else
                    {
                        // No AutoCAD running - create new via COM (latest version)
                        acadObject = AcadComUtils.CreateLatestAutoCADInstance();
                        _weOwnTheAcadInstance = true;  // We created it

                        // Get PID for tracking
                        _acadProcessId = AcadComUtils.GetProcessIdFromComObject(acadObject);
                    }
                }

                if (acadObject == null)
                {
                    return false;
                }

                // Make visible and capture HWND
                AcadComUtils.SetVisible(acadObject, true);

                object hwnd = acadObject.GetType().InvokeMember(
                    "HWND",
                    BindingFlags.GetProperty,
                    null, acadObject, null
                );

                _acadObjectId = hwnd?.ToString() ?? string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                string msg = ex.InnerException?.Message ?? ex.Message;

                MessageBox.Show(
                    $"Failed to start/attach AutoCAD.\n\nError: {msg}\n\n" +
                    "Please verify:\n" +
                    "1) Your app runs as x64\n" +
                    "2) AutoCAD is installed & licensed\n" +
                    "3) Run with same elevation as AutoCAD (admin vs non-admin)",
                    "ScriptPro - AutoCAD COM",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                // Only kill AutoCAD if WE launched it
                if (pid != -1 && _weOwnTheAcadInstance)
                {
                    try
                    {
                        Process proc = Process.GetProcessById(pid);
                        proc.Kill();
                    }
                    catch
                    {
                        //ignore
                    }
                }

                return false;
            }
        }

        //This function finds the presence of Keywords and 
        //nested scripts
        int checkNestedScripts(string scriptFile)
        {

            System.IO.StreamReader sr = new System.IO.StreamReader(scriptFile);
            string str = sr.ReadToEnd();
            //str = str.ToLower();
            sr.Close();
            sr.Dispose();

            int nReturn = 0;
            try
            {
                if (str.Contains(keyFolderName) ||
                    str.Contains(keyBaseName) ||
                    str.Contains(keyExtension) ||
                    str.Contains(keyFileName) ||
                    str.Contains(keyFullFileName) ||
                    str.Contains(keyLayoutName))
                {
                    nReturn = 1; //contains key word
                }

                if (str.Contains("call") || str.Contains("Call"))
                {
                    nReturn = 2; //nested script
                }
            }
            catch
            {
            }

            return nReturn;
        }

        //This function replaces Keywords.
        //
        bool replaceKeyWords(string scriptFile, ref string newFile, string dwgName, string selectedLayout)
        {
            bool keyWordAdded = false;

            try
            {
                newFile = Path.GetTempPath() + "KeywordTemp.scr";

                System.IO.StreamReader sr = new System.IO.StreamReader(scriptFile);
                string str = sr.ReadToEnd();
                //str = str.ToLower();
                sr.Close();
                sr.Dispose();

                if (str.Contains(keyLayoutName))
                {
                    str = str.Replace(keyLayoutName, selectedLayout);
                    keyWordAdded = true;
                }

                if (str.Contains(keyFolderName))
                {
                    string folder = Path.GetDirectoryName(dwgName);

                    DirectoryInfo info = Directory.GetParent(folder);

                    if (info != null)
                        str = str.Replace(keyFolderName, folder);
                    else
                    {
                        folder = folder.TrimEnd('\\');
                        str = str.Replace(keyFolderName, folder);
                    }

                    keyWordAdded = true;
                }

                if (str.Contains(keyBaseName))
                {
                    string basename = Path.GetFileNameWithoutExtension(dwgName);
                    str = str.Replace(keyBaseName, basename);
                    keyWordAdded = true;
                }

                if (str.Contains(keyExtension))
                {
                    string ext = Path.GetExtension(dwgName);
                    str = str.Replace(keyExtension, ext);
                    keyWordAdded = true;
                }

                if (str.Contains(keyFileName))
                {
                    string name = Path.GetFileName(dwgName);
                    str = str.Replace(keyFileName, name);
                    keyWordAdded = true;
                }

                if (str.Contains(keyFullFileName))
                {
                    //string fullName = Path.GetFileName(dwgName);
                    str = str.Replace(keyFullFileName, dwgName);
                    keyWordAdded = true;
                }

                if (keyWordAdded)
                {
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(newFile, false);
                    sw.Write(str);
                    sw.Close();
                    sw.Dispose();
                }
            }
            catch
            {
                keyWordAdded = false;
            }

            return keyWordAdded;
        }

        // hàm tạo thư mục để lưu ảnh 
        private string CreateFolder(string filename)
        {
            try
            {
                string foldername = "PNG_PDF";
                string scriptPath = Path.GetDirectoryName(filename);
                DirectoryInfo di = new DirectoryInfo(scriptPath);
                // Create the directory only if it does not already exist.
                if (!di.Exists)
                    di.Create();

                // Create a subdirectory in the directory just created.
                DirectoryInfo dis = di.CreateSubdirectory(foldername);
                // Directory.CreateDirectory((path));
                return dis.FullName;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error creating directory: " + ex);
            }
            return "";
        }

        // This function will create a new script file in
        // the user's temp Directory for nested scripts only 
        bool processNestedScripts(string scriptPath, string scriptFile, ref string newFile,
                            ref bool errorInScript)
        {
            bool useTemp = false;
            try
            {
                string tempFile = Path.GetTempPath() + "NestedTemp1.scr";
                StreamReader sr = File.OpenText(scriptFile);
                StreamWriter sw = new StreamWriter(tempFile, false);
                string linetext = "";
                string scriptFolder = Path.GetDirectoryName(scriptFile);

                try
                {
                    linetext = sr.ReadLine();

                    while (linetext != null)
                    {
                        //string strLower = linetext.ToLower();
                        if (linetext.Contains("call") || linetext.Contains("Call"))
                        {
                            //
                            string[] lines = linetext.Split(' ');

                            if (string.Compare(lines[0], "call") != 0 &&
                                string.Compare(lines[0], "Call") != 0)
                            {
                                sw.WriteLine(linetext);
                            }
                            else
                            {
                                string strNested = null;

                                string strFileName = "";
                                int nLength = lines.Length;

                                for (int nString = 1; nString < nLength; nString++)
                                {
                                    if (strFileName.Length == 0)
                                        strFileName = lines[nString];
                                    else
                                        strFileName = strFileName + " " + lines[nString];
                                }

                                if (File.Exists(strFileName))
                                    strNested = strFileName;
                                else //relative path...
                                    strNested = scriptPath + "\\" + strFileName;

                                //Find the nested file.
                                //if failed to find, then write the complete
                                //line to script. This will be error...
                                if (File.Exists(strNested))
                                {
                                    StreamReader srNested = File.OpenText(strNested);

                                    string textNested = srNested.ReadLine();

                                    while (textNested != null)
                                    {
                                        sw.WriteLine(textNested);
                                        textNested = srNested.ReadLine();
                                    }

                                    srNested.Close();
                                    srNested.Dispose();

                                    useTemp = true;
                                }
                                else
                                {
                                    //will be error...
                                    sw.WriteLine(linetext);
                                    errorInScript = true;
                                }
                            }
                        }
                        else
                        {
                            sw.WriteLine(linetext);
                        }
                        linetext = sr.ReadLine();
                    }
                }
                catch { useTemp = false; }

                sr.Close();
                sr.Dispose();
                sw.Close();
                sw.Dispose();

                if (useTemp)
                {
                    newFile = Path.GetTempPath() + "NestedTemp.scr";
                    File.Copy(tempFile, newFile, true);

                    return true;
                }
            }
            catch { useTemp = false; }

            newFile = scriptFile;
            return false;
        }

        //Function to get active document. if no document present
        //this function will add a empty document
        object getActiveDocument(object acadObject)
        {
            object ActiveDocument = null;
            try
            {
                object AcadDocuments =
                  acadObject.GetType().InvokeMember(
                    "Documents",
                    BindingFlags.GetProperty,
                    null, acadObject, null
                  );

                int count =
                  (int)AcadDocuments.GetType().InvokeMember(
                    "Count",
                    BindingFlags.GetProperty,
                    null, AcadDocuments, null
                  );

                //if no document present
                if (count == 0)
                {
                    AcadDocuments.GetType().InvokeMember(
                                "Add",
                                BindingFlags.InvokeMethod,
                                null, AcadDocuments, null
                              );


                    Thread.Sleep(1000);
                }

                ActiveDocument =
                    acadObject.GetType().InvokeMember(
                      "ActiveDocument",
                      BindingFlags.GetProperty,
                      null, acadObject, null
                    );
            }
            catch
            {
            }

            return ActiveDocument;
        }

        // Function to start the process..
        bool StartBatchProcess(bool restarted, int userOption)
        {
            if (!restarted)
                _runOption = userOption;

            // Create script file based on PDF/PNG selection
            if (!CreateScriptFile()) // kiểm tra xem file script đã được tạo chưa, nếu chưa thì thông báo lỗi và trả về false, không tiếp tục chạy batch process
            {
                return false;
            }
            // ==========================================

            // Check if script file present
            if (!File.Exists(_scriptPath))
            {
                MessageBox.Show(
                  "Script file chưa được tạo. Vui lòng chọn định dạng (PDF hoặc PNG)."
                );

                return false;
            }

            bool startBP = false;
            try
            {
                if (!restarted)
                    Initialize_start(userOption);

                try
                {
                    Application.DoEvents();
                    this.SuspendLayout();
                }
                catch { }

                // Ensure runSelectedExe is set correctly on restart
                if (acadExePath.Length != 0)
                    runSelectedExe = true;

                // Start the AutoCAD (pass restarted flag to suppress dialogs on restart)
                if (!startAutoCAD(restarted))
                {
                    MessageBox.Show("Unable to start AutoCAD");

                    BPbar.Visible = false;
                    label_filename.Visible = false;

                    // Enable the application start button
                    UpdateHostApplicationUI(false);

                    return false;
                }

                try
                {
                    ResumeLayout();
                }
                catch
                {
                }

                // Get active document...
                object ActiveDocument = null;

                //No need to get the active document for
                //commandline scripts
                if (!useCmdLine)
                    ActiveDocument = getActiveDocument(acadObject);

                if (!restarted)
                {

                    if (!useCmdLine)
                    {
                        object[] OnedataArray = new object[1];
                        object[] TwoVariable = new object[2];

                        OnedataArray[0] = "FILEDIA";
                        _fd =
                          ActiveDocument.GetType().InvokeMember(
                            "GetVariable",
                            BindingFlags.InvokeMethod,
                            null, ActiveDocument, OnedataArray
                          );

                        OnedataArray[0] = "RECOVERYMODE";
                        _rm =
                          ActiveDocument.GetType().InvokeMember(
                            "GetVariable",
                            BindingFlags.InvokeMethod,
                            null, ActiveDocument, OnedataArray
                          );

                        OnedataArray[0] = "LOGFILEMODE";
                        _lf =
                          ActiveDocument.GetType().InvokeMember(
                            "GetVariable",
                            BindingFlags.InvokeMethod,
                            null, ActiveDocument, OnedataArray
                          );

                        TwoVariable[0] = "FILEDIA";
                        TwoVariable[1] = 0;
                        ActiveDocument.GetType().InvokeMember(
                          "SetVariable",
                          BindingFlags.InvokeMethod,
                          null, ActiveDocument, TwoVariable
                        );

                        // Set recovery mode to 0
                        TwoVariable[0] = "LOGFILEMODE";
                        TwoVariable[1] = 1;
                        ActiveDocument.GetType().InvokeMember(
                          "SetVariable",
                          BindingFlags.InvokeMethod,
                          null, ActiveDocument, TwoVariable
                        );

                        // Set log file mode to 1
                        TwoVariable[0] = "RECOVERYMODE";
                        TwoVariable[1] = 0;
                        ActiveDocument.GetType().InvokeMember(
                          "SetVariable",
                          BindingFlags.InvokeMethod,
                          null, ActiveDocument, TwoVariable
                        );
                    }

                    // Create the thread syn event
                    Threadinput.ThreadEvent = new AutoResetEvent(false);

                    if (!useCmdLine)
                        _timeout.Interval = _timeoutSec * 1000 + _toolSpeed * 2;

                    pbValue = 100.0 / Threadinput._FileInfolist.Count;
                    fileCount = 0;

                    Threadinput.scriptFile = _scriptPath;
                    Threadinput.startUpScript = _startUpScript;
                    Threadinput._restartDWGCount = _restartDWGCount;
                    Threadinput.logLocation = _logFilePath;
                    Threadinput.commnadLineExePath = acadExePath;
                    Threadinput.timeout = _timeoutSec;

                    Threadinput.nestedScript =
                        checkNestedScripts(_scriptPath);
                }

                // Set the new acad object....
                Threadinput.acadObject = acadObject;
                Threadinput.nCreateImage = createImage;
                Threadinput.bDiagnosticMode = diagnosticMode;

                // Start the thread again...
                batchProcessThread.RunWorkerAsync(Threadinput);

                startBP = true;
            }
            catch
            {
            }

            return startBP;
        }

        // Function to kill AutoCAD by process
        void KillAutoCAD()
        {
            // Kill AutoCAD
            if (useCmdLine)
            {
                //not possible...
                return;
            }

            try
            {
                //Make this application as Foreground application.
                //it is noticed that, if the AutoCAD is Foreground application
                //and showing some tooltips, proc.Kill() is unable to kill the
                //AutoCAD, so as a workaround, set the ScriptPro as Foreground
                //application and kill the AutoCAD.

                Type myType = this.HostApplication.GetType();
                MethodInfo myMethodInfo = myType.GetMethod("SetFocusToApplication");
                myMethodInfo.Invoke(HostApplication, null);
                Application.DoEvents();

                Process[] procs = Process.GetProcessesByName("acad");
                foreach (Process proc in procs)
                {
                    if (proc.MainWindowHandle.ToString() == _acadObjectId)
                    {
                        proc.Kill();
                        break;
                    }
                }
                acadObject = null;
            }
            catch { }
        }

        // Quits the ACAD on request
        void quitAcad(object acadObject, bool finalQuit)
        {
            if (!useCmdLine)
            {
                // If we attached to user's existing AutoCAD, don't close it
                if (!_weOwnTheAcadInstance)
                {
                    // Just release COM reference, leave AutoCAD open
                    return;
                }
                try
                {
                    // First close all documents...
                    object[] TwoVariable = new object[2];

                    // Get documents...
                    object AcadDocuments =
                      acadObject.GetType().InvokeMember(
                        "Documents",
                        BindingFlags.GetProperty,
                        null, acadObject, null
                      );

                    if (finalQuit)
                    {
                        // Add a new document if it is
                        //ending AutoCAD
                        AcadDocuments.GetType().InvokeMember(
                          "Add",
                          BindingFlags.InvokeMethod,
                          null, AcadDocuments, null
                        );
                    }

                    int count =
                      (int)AcadDocuments.GetType().InvokeMember(
                        "Count",
                        BindingFlags.GetProperty,
                        null, AcadDocuments, null
                      );

                    // Set the system variable back
                    if (finalQuit)
                    {
                        // Reset the variables
                        object ActiveDocument =
                          acadObject.GetType().InvokeMember(
                            "ActiveDocument",
                            BindingFlags.GetProperty,
                            null, acadObject, null
                          );

                        TwoVariable[0] = "FILEDIA";
                        TwoVariable[1] = _fd;
                        ActiveDocument.GetType().InvokeMember(
                          "SetVariable",
                          BindingFlags.InvokeMethod,
                          null, ActiveDocument, TwoVariable
                        );

                        TwoVariable[0] = "RECOVERYMODE";
                        TwoVariable[1] = _rm;
                        ActiveDocument.GetType().InvokeMember(
                          "SetVariable",
                          BindingFlags.InvokeMethod,
                          null, ActiveDocument, TwoVariable
                        );

                        // LOGFILEMODE
                        TwoVariable[0] = "LOGFILEMODE";
                        TwoVariable[1] = _lf;
                        ActiveDocument.GetType().InvokeMember(
                          "SetVariable",
                          BindingFlags.InvokeMethod,
                          null, ActiveDocument, TwoVariable
                        );

                        if (_bgPlotOriginal != null)
                        {
                            TwoVariable[0] = "BACKGROUNDPLOT";
                            TwoVariable[1] = _bgPlotOriginal;
                            ActiveDocument.GetType().InvokeMember(
                              "SetVariable",
                              BindingFlags.InvokeMethod,
                              null, ActiveDocument, TwoVariable
                            );
                            _bgPlotOriginal = null;
                        }
                    }

                    while (count > 0)
                    {
                        // Reset the variables....
                        object ActiveDocument =
                          acadObject.GetType().InvokeMember(
                            "ActiveDocument",
                            BindingFlags.GetProperty,
                            null, acadObject, null
                          );

                        // Close the drawing file - no need to save
                        // if required script file will have save...
                        TwoVariable[0] = false;
                        TwoVariable[1] = "";
                        ActiveDocument.GetType().InvokeMember(
                          "Close",
                          BindingFlags.InvokeMethod,
                          null, ActiveDocument, TwoVariable
                        );

                        count--;
                    }

                    acadObject.GetType().InvokeMember(
                      "Quit",
                      BindingFlags.InvokeMethod,
                      null, acadObject, null
                    );

                    // Wait for AutoCAD to fully exit before continuing
                    // This is critical for restarts to work properly
                    if (!finalQuit && _acadProcessId > 0)
                    {
                        // This is a restart scenario - wait for OUR specific AutoCAD process to exit
                        Thread.Sleep(2000); // Give AutoCAD time to start shutting down

                        // Wait up to 30 seconds for the specific AutoCAD process we own to exit
                        int waitCount = 0;
                        while (waitCount < 30)
                        {
                            try
                            {
                                Process proc = Process.GetProcessById(_acadProcessId);
                                if (proc.HasExited)
                                    break; // Our AutoCAD has exited
                            }
                            catch (ArgumentException)
                            {
                                // Process no longer exists - good!
                                break;
                            }

                            Thread.Sleep(1000);
                            waitCount++;
                        }

                        // Additional settling time for COM cleanup
                        Thread.Sleep(1000);
                    }
                }
                catch
                {
                    KillAutoCAD();

                    // After killing, also wait for cleanup
                    if (!finalQuit)
                    {
                        Thread.Sleep(2000);
                    }
                }
            }

            if (finalQuit)
            {
                if (_projectName.Length != 0 && _runOption == RUN_CHECKED)
                {
                    try
                    {
                        bool isFailed = false;
                        foreach (ListViewItem item in DwgList.Items)
                        {
                            TagData data = (TagData)item.Tag;

                            if (data.status == false)
                            {
                                isFailed = true;
                                break;
                            }

                        }

                        if (isFailed)
                        {
                            string name = Path.GetFileNameWithoutExtension(_projectName);
                            string path = Path.GetDirectoryName(_projectName);

                            string day = DateTime.Now.Day.ToString();
                            string hour = DateTime.Now.Hour.ToString();
                            string min = DateTime.Now.Minute.ToString();
                            string sec = DateTime.Now.Second.ToString();

                            string strProject = path + "\\" + name + "_" + day + "_" + hour + "_" +
                             min + "_" + sec + "_" + "failed.bpl";

                            writeDWGList(strProject, true);

                            //creation fails, then create in temp directory...
                            if (File.Exists(strProject) == false)
                            {
                                path = Path.GetTempPath();
                                strProject = path + "\\" + name + "_" + day + "_" + hour + "_" +
                                  min + "_" + sec + "_" + "failed.bpl";

                                writeDWGList(strProject, true);
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                if (silentExit)
                {
                    //no log file showing
                }
                else
                {
                    DialogResult result =
                    MessageBox.Show(
                      "Do you wish to view the log file?",
                      "ScriptPro", MessageBoxButtons.YesNo
                    );

                    if (result == DialogResult.Yes)
                    {
                        // Show the log file
                        if (bplog != null)
                        {
                            if (File.Exists(
                              bplog.getDetailLogFileName())
                            )
                            {
                                Process notePad = new Process();
                                notePad.StartInfo.FileName = "notepad.exe";
                                notePad.StartInfo.Arguments =
                                  bplog.getDetailLogFileName();
                                notePad.Start();
                            }
                        }
                    }
                }
            }
        }

        [DllImport("user32.dll")]
        static extern int GetForegroundWindow();

        // Helper function for creating the screen dump
        // this function makes AutoCAD topmost and gets its size
        void makeACADTopApplication(
          ref int left, ref int top, ref int width, ref int height
        )
        {
            if (useCmdLine)
            {
                //not possible...
                return;
            }

            try
            {
                object acadHwnd =
                  acadObject.GetType().InvokeMember(
                    "HWND",
                    BindingFlags.GetProperty,
                    null, acadObject, null
                  );

                int foreground = GetForegroundWindow();

                if (string.Compare(foreground.ToString(),
                    acadHwnd.ToString(), false) != 0)
                {
                    object WindowState =
                      acadObject.GetType().InvokeMember(
                        "WindowState",
                        BindingFlags.GetProperty,
                        null, acadObject, null
                      );

                    string strWindowState = WindowState.ToString();
                    object[] OnedataArry = new object[1];

                    // Minimized
                    if (string.Compare(strWindowState, "2", false) == 0)
                    {
                        OnedataArry[0] = 1;
                        acadObject.GetType().InvokeMember(
                          "WindowState",
                          BindingFlags.SetProperty,
                          null, acadObject, OnedataArry
                        );

                        Thread.Sleep(1000);
                    }
                    else
                    {
                        //minimise & maximise
                        OnedataArry[0] = 2;
                        acadObject.GetType().InvokeMember(
                          "WindowState",
                          BindingFlags.SetProperty,
                          null, acadObject, OnedataArry
                        );

                        Thread.Sleep(1000);

                        OnedataArry[0] = 1;
                        acadObject.GetType().InvokeMember(
                          "WindowState",
                          BindingFlags.SetProperty,
                          null, acadObject, OnedataArry
                        );

                        Thread.Sleep(1000);
                    }
                }

                left =
                  (int)acadObject.GetType().InvokeMember(
                    "WindowLeft",
                    BindingFlags.GetProperty,
                    null, acadObject, null
                  );

                top =
                  (int)acadObject.GetType().InvokeMember(
                    "WindowTop",
                    BindingFlags.GetProperty,
                    null, acadObject, null
                  );

                width =
                  (int)acadObject.GetType().InvokeMember(
                    "Width",
                    BindingFlags.GetProperty,
                    null, acadObject, null
                  );

                height =
                  (int)acadObject.GetType().InvokeMember(
                    "Height",
                    BindingFlags.GetProperty,
                    null, acadObject, null
                  );
            }
            catch { }
        }

        // Function to capture the AutoCAD screen image
        void captureScreen(
          string filename, string saveLocation,
          ref int left, ref int top, ref int width, ref int height
        )
        {
            if (useCmdLine)
            {
                //not possible...
                return;
            }

            if (filename.Length == 0)
                return;

            try
            {
                Graphics myGraphics = this.CreateGraphics();
                Size s = new Size(width, height);
                Bitmap image = new Bitmap(width, height, myGraphics);
                Graphics gfx = Graphics.FromImage(image);
                gfx.CopyFromScreen(left, top, 0, 0, s);

                string imagePath = Path.GetFileName(filename);
                imagePath = Path.GetFileNameWithoutExtension(imagePath);
                imagePath = saveLocation + "\\" + imagePath + ".jpg";

                image.Save(imagePath);
                image.Dispose();
            }
            catch { }
        }

        // To check if AutoCAD is is busy or not...
        bool IsAcadQuiescent(int tries, int sleep)
        {
            if (useCmdLine)
            {
                //not possible...
                return true;
            }
            bool ret = false;
            Thread.Sleep(sleep);


            int number = tries;

            if (tries == -1)
                number = 5;

            while (number > 0)
            {
                try
                {
                    object state =
                      acadObject.GetType().InvokeMember(
                        "GetAcadState",
                         BindingFlags.InvokeMethod,
                        null, acadObject, null
                      );

                    object quiescent =
                      state.GetType().InvokeMember(
                        "IsQuiescent",
                        BindingFlags.GetProperty,
                        null, state, null
                      );

                    ret = (bool)quiescent;

                    if (ret)
                        break;
                    else
                        Thread.Sleep(sleep); // 1 sec

                    if (tries != -1)
                        number = number - 1;

                    if (acadObject == null)
                    {
                        ret = false;
                        break;
                    }
                }
                catch
                {
                    Thread.Sleep(sleep); // 1 secs
                    number = number - 1;

                    if (acadObject == null)
                    {
                        ret = false;
                        break;
                    }
                    else
                    {
                        getActiveDocument(acadObject);
                    }
                }
            }
            return ret;
        }

        #endregion

        //Main function starts the provided Acad application passing drawing file 
        //amd script as argumnets.
        bool RunScriptAsCommandlineArgument(string ApplicationPath,
            string drawingFilePath, string scriptFilePath,
            int maxWaitInMilliSeconds, ref string commandline)
        {
            bool bDone = true;
            commandline = " Error while reading log file for " +
                                      drawingFilePath + "\n";
            try
            {

                string ApplicationArguments = "";

                //check if user selected application is AutoCAD or headleass AutoCAD
                bool bHeadlessAcad = isHeadlessAcad(ApplicationPath);

                if (bHeadlessAcad)
                {
                    // Kill the console.exe if it is already running...
                    Process[] processes =
                        Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ApplicationPath));
                    foreach (Process proc in processes)
                    {
                        proc.Kill();
                    }

                    ApplicationArguments = $"/i \"{drawingFilePath}\" /s \"{scriptFilePath}\" /l en-US";
                }
                else
                {
                    //add quit at the end of script file...
                    //as we need to quit the AutoCAD after processing the script
                    string newFile = Path.GetTempPath() + "commandline.scr";

                    System.IO.StreamReader sr = new System.IO.StreamReader(scriptFilePath);
                    string str = sr.ReadToEnd();
                    str = str.ToLower();
                    sr.Close();
                    sr.Dispose();
                    //add quit...
                    //str = str + "_quit\n" + "_yes\n";
                    str = string.Format("{0}_quit{1}_yes{1}", str, Environment.NewLine);


                    System.IO.StreamWriter sw = new System.IO.StreamWriter(newFile, false);
                    sw.Write(str);
                    sw.Close();
                    sw.Dispose();

                    ApplicationArguments = string.Format("/i \"{0}\" /b \"{1}\"",
                       drawingFilePath, newFile);
                }

                //start the process... No ActiveX API
                Process ProcessObj = new Process();
                ProcessObj.StartInfo.FileName = ApplicationPath;
                ProcessObj.StartInfo.Arguments = ApplicationArguments;
                ProcessObj.StartInfo.UseShellExecute = false;
                ProcessObj.StartInfo.CreateNoWindow = true;
                ProcessObj.StartInfo.RedirectStandardOutput = true;

                // The standard output buffer becomes full and so the processes blocks and never ends. This code fixes that
                // from: http://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
                StringBuilder output = new StringBuilder();
                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                {
                    ProcessObj.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                            outputWaitHandle.Set();
                        else
                            output.AppendLine(e.Data);
                    };
                    ProcessObj.Start();
                    ProcessObj.BeginOutputReadLine();

                    if (ProcessObj.WaitForExit(maxWaitInMilliSeconds) && outputWaitHandle.WaitOne(maxWaitInMilliSeconds))
                    {
                        // success
                        output.Replace("\0", "");       // output has \0 characters between each normal character - replace them
                    }
                    else
                    {
                        // timeout
                    }

                    ProcessObj.CancelOutputRead();
                }

                //sleep for 2 second
                Thread.Sleep(2000);

                try
                {
                    //Read the commandline log for headless exe for logging purpose
                    Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ApplicationPath));
                    if (processes.Length != 0)
                    {
                        //kill the applications
                        foreach (Process proc in processes)
                        {
                            //process still alive
                            if (proc.Id == ProcessObj.Id)
                            {
                                //failed...
                                bDone = false;
                                proc.Kill();
                            }
                        }
                    }
                    else
                    {
                        if (bHeadlessAcad)
                            commandline = output.ToString();
                    }
                }
                catch
                {
                    bDone = false;
                }

                return bDone;
            }
            catch
            {
                bDone = false;
            }

            return bDone;
        }


        //Main function which goes through the file list and run the script on each file, 
        //called from batchProcessThread_DoWork
        void batchProcessThread_DoWork_CommandlineArgument(ref ThreadInput input,
                            ref BackgroundWorker worker)
        {
            foreach (FileInfo info in input._FileInfolist)
            {
                int reportStatus = 0;

                try
                {
                    // Report start of the process...
                    worker.ReportProgress(OPEN_NEW_DWG, info);

                    // Wait till timer is started
                    Threadinput.ThreadEvent.WaitOne();

                    // Check for cancelation..
                    if (batchProcessThread.CancellationPending)
                        break;

                    Thread.Sleep(100);

                    string scriptFile = input.scriptFile;
                    bool errorInScript = false;

                    if (input.nestedScript != 0)
                    {
                        string strOldScr = input.scriptFile;

                        if (!replaceKeyWords(strOldScr, ref scriptFile, info._fileName, info._selectedLayout))
                        {
                            //No Keywords, so set back the file name
                            scriptFile = input.scriptFile;
                        }
                    }
                    if (input.nestedScript == 2) //nested    
                    {
                        string strOldScr = scriptFile;
                        string scriptPath = Path.GetDirectoryName(input.scriptFile);
                        while (processNestedScripts(scriptPath, strOldScr, ref scriptFile, ref errorInScript))
                        {
                            strOldScr = scriptFile;

                            //nested scripts may add key words
                            if (replaceKeyWords(strOldScr, ref scriptFile, info._fileName, info._selectedLayout))
                            {
                                strOldScr = scriptFile;
                            }
                        }
                    }

                    //run the script on AutoCAD/headless AutoCAD
                    if (RunScriptAsCommandlineArgument(input.commnadLineExePath, info._fileName, scriptFile,
                        input.timeout * 1000, ref info._logFile))
                        reportStatus = CLOSE_DWG_SUCCESS; //done
                    else
                        reportStatus = CLOSE_DWG_FAILED;//fail
                }
                catch
                {
                    reportStatus = CLOSE_DWG_FAILED; //fail
                }

                worker.ReportProgress(reportStatus, info);

                Threadinput.ThreadEvent.WaitOne();

                if (batchProcessThread.CancellationPending)
                    break;
            }


        }

        // Functions related to batch process thread

        // Main function for batch process...
        // Opens each drawing file and runs the selected script...
        private void batchProcessThread_DoWork(
          object sender, DoWorkEventArgs e
        )
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            ThreadInput input = (ThreadInput)e.Argument;

            //for commandline argument call different function
            if (useCmdLine)
            {
                batchProcessThread_DoWork_CommandlineArgument(ref input, ref worker);
                e.Result = true;
                return;
            }

            object[] OnedataArray = new object[1];
            object[] ThreeVariable = new object[3];
            object[] TwoVariable = new object[2];


            Object acadObject = input.acadObject;

            //Version 2.1
            int getActiveDoc = 5;
            object AcadDocuments = null;
            object ActiveDocument = null;

            while (getActiveDoc > 0)
            {

                // Get the AcadDocuments
                try
                {
                    AcadDocuments =
                      acadObject.GetType().InvokeMember(
                        "Documents",
                        BindingFlags.GetProperty,
                        null, acadObject, null
                      );

                    ActiveDocument =
                      acadObject.GetType().InvokeMember(
                        "ActiveDocument",
                        BindingFlags.GetProperty,
                        null, acadObject, null
                      );

                    break;
                }
                catch
                {
                    getActiveDoc--;

                    //sleep for 1 second
                    Thread.Sleep(1000);
                }
            }

            bool isAcadKilled = false;
            int reportStatus = 0;

            // Run the Threadinput.startUpScript...
            if (Threadinput.startUpScript.Length != 0)
            {
                CheckFileDiaSystemVariable(ActiveDocument);

                OnedataArray[0] =
                  "_.SCRIPT " + Threadinput.startUpScript + "\n";
                ActiveDocument.GetType().InvokeMember(
                  "SendCommand",
                  BindingFlags.InvokeMethod,
                  null, ActiveDocument, OnedataArray
                );
            }

            // 5 seconds so that AutoCAD is ready 
            IsAcadQuiescent(5, 1000);

            // Call Open
            int fileCount = 0;
            int DWGsprocessed = 0;
            bool returnResult = true;


            foreach (FileInfo info in input._FileInfolist)
            {
                fileCount++;

                if (info._processed)
                    continue;

                string selectedLayout = info._selectedLayout;

                isAcadKilled = false;
                _imageCreated = false;
                try
                {
                    // Report start of the process...
                    worker.ReportProgress(OPEN_NEW_DWG, info);

                    // Wait till timer is started
                    Threadinput.ThreadEvent.WaitOne();

                    // Check for cancelation..
                    if (batchProcessThread.CancellationPending)
                        break;

                    Thread.Sleep(100);

                    if (Threadinput.nCreateImage != 2) // No image capture
                        makeACADTopApplication(
                          ref _left, ref _top, ref _width, ref _height
                        );

                    //version 2.1
                    Thread.Sleep(500 + _toolSpeed); //100

                    using (new Heartbeat(KeepAlive, OPEN_MAX_SEC, HeartbeatPeriodMs()))
                    {
                        //do not open the document, if user wants to run
                        //the script on dummy/empty document
                        if (!runWithoutOpen)
                        {
                            ThreeVariable[0] = info._fileName;//Name
                            ThreeVariable[1] = false; //ReadOnly
                            ThreeVariable[2] = " "; //Password
                            ActiveDocument =
                            AcadDocuments.GetType().InvokeMember(
                             "Open",
                             BindingFlags.InvokeMethod,
                             null, AcadDocuments, ThreeVariable
                           );
                        }
                        else
                        {
                            ActiveDocument = getActiveDocument(acadObject);
                        }

                        bool ready = WaitAcadReady(60);
                        Trace("after open, ready=" + ready);
                        if (!ready)
                            info._diagLog += "[WARN] IsQuiescent vẫn false sau 60s, vẫn thử xuất." + Environment.NewLine;

                        //if (!WaitAcadReady(300))
                        //    throw new Exception("AutoCAD chưa sẵn sàng sau khi mở file.");

                    }
                    
                    //add on 4-1-2011 - for ACA testing....
                    //Thread.Sleep(500 + _toolSpeed); //100
                    //version 2.1
                    // DiagnosticMode, now show a message box
                    // may be better UI later....

                    if (Threadinput.bDiagnosticMode)
                    {
                        // Show the message box and hold the screen...
                        MessageBox.Show(
                          "Press OK to continue...",
                          "Diagnostic mode", MessageBoxButtons.OK
                        );
                    }

                    // Read the system variable LOGFILENAME
                    OnedataArray[0] = "LOGFILENAME";
                    info._logFile =
                      (string)ActiveDocument.GetType().InvokeMember(
                      "GetVariable",
                      BindingFlags.InvokeMethod,
                      null, ActiveDocument, OnedataArray
                    );

                    // Sleep for 0.5 seconds...
                    Thread.Sleep(500 + _toolSpeed);
                    CheckFileDiaSystemVariable(ActiveDocument);

                    //1.0.2 
                    string scriptFile = input.scriptFile;
                    bool errorInScript = false;

                    if (input.nestedScript != 0)
                    {
                        string strOldScr = input.scriptFile;

                        if (!replaceKeyWords(strOldScr, ref scriptFile, info._fileName, info._selectedLayout))
                        {
                            //No Keywords, so set back the file name
                            scriptFile = input.scriptFile;
                        }
                    }
                    if (input.nestedScript == 2) //nested    
                    {
                        string strOldScr = scriptFile;
                        string scriptPath = Path.GetDirectoryName(input.scriptFile);
                        while (processNestedScripts(scriptPath, strOldScr, ref scriptFile, ref errorInScript))
                        {
                            strOldScr = scriptFile;

                            //nested scripts may add key words
                            if (replaceKeyWords(strOldScr, ref scriptFile, info._fileName, info._selectedLayout))
                            {
                                strOldScr = scriptFile;
                            }
                        }
                    }

                    if (_exportFormat == "PNG")
                    {
                        using (new Heartbeat(KeepAlive, EXPORT_MAX_SEC, HeartbeatPeriodMs()))
                        {
                            string pngLog;
                            bool pngOk = ExportPngViaCom(ActiveDocument, info, out pngLog);
                            info._diagLog += pngLog;
                            if (!pngOk) errorInScript = true;
                        }
                    }
                    else if (_exportFormat == "PDF")
                    {
                        using (new Heartbeat(KeepAlive, EXPORT_MAX_SEC, HeartbeatPeriodMs()))
                        {
                            string pdfLog;
                            bool pdfOk = ExportPdfViaCom(ActiveDocument, info, out pdfLog);
                            info._diagLog += pdfLog;
                            if (!pdfOk) errorInScript = true;
                        }
                    }
                    else
                    {
                        string escapedPath = scriptFile.Replace("\\", "/");
                        string scriptCmd = $"_.SCRIPT \"{escapedPath}\"\n";

                        bool scriptSent = false;
                        int maxRetries = 3;
                        for (int attempt = 0; attempt < maxRetries; attempt++)
                        {
                            try
                            {
                                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] BEFORE SCRIPT (attempt {attempt + 1}/{maxRetries}): {info._fileName}");
                                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Script: {escapedPath}");

                                IsAcadQuiescent(10, 1000);

                                OnedataArray[0] = scriptCmd;
                                ActiveDocument.GetType().InvokeMember(
                                  "SendCommand",
                                  BindingFlags.InvokeMethod,
                                  null, ActiveDocument, OnedataArray
                                );

                                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AFTER SCRIPT SENT: {info._fileName}");
                                scriptSent = true;
                                break;
                            }
                            catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is COMException comEx)
                            {
                                int RPC_UNAVAILABLE = unchecked((int)0x800706BE);
                                if (comEx.HResult == RPC_UNAVAILABLE)
                                {
                                    Console.WriteLine($"⚠ RPC Unavailable (attempt {attempt + 1}/{maxRetries})");
                                    if (attempt < maxRetries - 1)
                                    {
                                        Thread.Sleep(3000);
                                        IsAcadQuiescent(5, 1000);
                                        continue;
                                    }
                                    else throw;
                                }
                                else throw;
                            }
                        }

                        if (scriptSent)
                        {
                            Thread.Sleep(5000);
                            IsAcadQuiescent(-1, 1000);
                        }
                        else
                        {
                            throw new Exception("Không thể gửi lệnh SCRIPT");
                        }
                    }

                    // Thread.Sleep( 3000 );

                    if (Threadinput.nCreateImage == 0)
                    {
                        _imageCreated = true;
                        captureScreen(
                          info._fileName, input.logLocation,
                          ref _left, ref _top, ref _width, ref _height
                        );
                    }

                    // DiagnosticMode, now show a message box
                    // may be better UI later....
                    if (Threadinput.bDiagnosticMode)
                    {
                        // Show the message box and hold the screen...
                        MessageBox.Show(
                          "Press OK to continue...",
                          "Diagnostic mode", MessageBoxButtons.OK
                        );
                    }


                    // Close the drawing file - no need to save
                    // if required script file will have save...
                    if (!runWithoutOpen)
                    {
                        TwoVariable[0] = false;
                        TwoVariable[1] = "";
                        ActiveDocument.GetType().InvokeMember(
                          "Close",
                          BindingFlags.InvokeMethod,
                          null, ActiveDocument, TwoVariable
                        );
                    }

                    Thread.Sleep(500);

                    if (!errorInScript)
                        reportStatus = CLOSE_DWG_SUCCESS; //done
                    else
                        reportStatus = CLOSE_DWG_FAILED; //failed
                }
                catch
                {
                    //failed
                    if (!_imageCreated && Threadinput.nCreateImage == 1)
                    {
                        captureScreen(
                          info._fileName, input.logLocation,
                          ref _left, ref _top, ref _width, ref _height
                        );
                    }
                    // Either fail to open drawing
                    // AutoCAD killed...
                    // AutoCAD crashed...
                    reportStatus = CLOSE_DWG_FAILED; //failed
                    isAcadKilled = true;
                }

                DWGsprocessed++;
                worker.ReportProgress(reportStatus, info);
                Threadinput.ThreadEvent.WaitOne();

                // AutoCAD is either killed, crashed OR busy....
                if (isAcadKilled)
                {
                    Process[] procs = Process.GetProcessesByName("acad");
                    foreach (Process proc in procs)
                    {
                        if (string.Compare(proc.MainWindowHandle.ToString(),
                       _acadObjectId, false) == 0)
                        {
                            // AutoCAD is showing some dialog
                            // or is not responding...
                            isAcadKilled = false;
                            returnResult = true;
                            break;
                        }
                    }
                }
                // If AutoCAD is killed, then break
                if (isAcadKilled)
                    break;

                // Check for cancellation..
                if (batchProcessThread.CancellationPending)
                    break;

                // DWG 
                if (DWGsprocessed == Threadinput._restartDWGCount)
                {
                    isAcadKilled = false;
                    returnResult = false;
                    break;
                }
            }

            // Check AutoCAD...
            if (!isAcadKilled)
            {
                e.Result = returnResult;
            }
            else
            {
                // AutoCAD is no more so return always false
                e.Result = false;
            }

        }

        //to check the file dia system variable
        void CheckFileDiaSystemVariable(object ActiveDocument)
        {
            try
            {
                object[] OnedataArray = new object[1];
                OnedataArray[0] = "FILEDIA";
                short fd =
                  (short)ActiveDocument.GetType().InvokeMember(
                    "GetVariable",
                    BindingFlags.InvokeMethod,
                    null, ActiveDocument, OnedataArray
                  );


                if (fd == 1)
                {
                    //reset the variable
                    _fd = fd;

                    object[] TwoVariable = new object[2];
                    TwoVariable[0] = "FILEDIA";
                    TwoVariable[1] = 0;
                    ActiveDocument.GetType().InvokeMember(
                      "SetVariable",
                      BindingFlags.InvokeMethod,
                      null, ActiveDocument, TwoVariable
                    );
                }
            }
            catch { }
        }
        // Batch process end call back...
        // Called in main thread
        void batchProcessThread_RunWorkerCompleted(
          object sender, RunWorkerCompletedEventArgs e
        )
        {
            bool batchProcessCompleted = true;
            try
            {
                batchProcessCompleted = (bool)e.Result;
            }
            catch { }

            bool finalQuit = false;

            if (batchProcessCompleted)
            {
                finalQuit = true;
            }
            else
            {
                if (_stopBatchProcess)
                    finalQuit = true;
            }


            if (acadObject != null)
            {
                quitAcad(acadObject, finalQuit);

                acadObject = null;
            }
            else
            {
                if (finalQuit)
                {
                    // AutoCAD is no more... but we need to set
                    // a few settings like filedia, etc.
                    // So restart AutoCAD... (suppress dialog, this is cleanup)
                    startAutoCAD(isRestart: true);

                    // Set them and quit AutoCAD....
                    quitAcad(acadObject, finalQuit);
                }
            }

            if (batchProcessCompleted)
            {
                BPbar.Visible = false;
                label_filename.Visible = false;

                //Enable the application start button
                UpdateHostApplicationUI(false);

                //if user wants the exit of ScriptPro, then perform
                if (silentExit)
                {
                    //exit the application
                    Type myType = this.HostApplication.GetType();
                    MethodInfo myMethodInfo = myType.GetMethod("ExitApplication");
                    myMethodInfo.Invoke(HostApplication, null);
                }
            }
            else
            {
                while (batchProcessThread.IsBusy)
                {
                    // Wait till back ground thread comes out....
                }

                if (!_stopBatchProcess)
                {
                    // Restart the Batch Process.
                    // _stopBatchProcess = false;
                    StartBatchProcess(true, _runOption);
                }
                else
                {
                    BPbar.Visible = false;
                    label_filename.Visible = false;

                    // Enable the application start button
                    UpdateHostApplicationUI(false);
                }

                _stopBatchProcess = false;
            }
        }

        // Batch process threads process update call back
        // Called in main thread, we mainly update the UI 

        void batchProcessThread_ProgressChanged(
          object sender, ProgressChangedEventArgs e
        )
        {
            try
            {
                // Start of a new drawing
                if (e.ProgressPercentage == OPEN_NEW_DWG)
                {
                    // Start the timer
                    if (!useCmdLine)
                        _timeout.Start();

                    try
                    {
                        FileInfo info = (FileInfo)e.UserState;

                        _currentDWG = info._fileName;

                        int fileNumber = fileCount + 1;
                        label_filename.TextAlign = ContentAlignment.MiddleCenter;
                        label_filename.Text =
                fileNumber.ToString() + " / " +
                Threadinput._FileInfolist.Count.ToString();
                        label_filename.Invalidate();
                        info._logFile = "";

                        ListViewItem item = DwgList.Items[info._index];
                        item.EnsureVisible();
                        itemColor = item.BackColor;
                        item.BackColor = System.Drawing.Color.Gold;
                    }
                    catch { }
                }
                else
                {
                    // Stop the timer...
                    // End of drawing
                    if (!useCmdLine)
                        _timeout.Stop();

                    // Update the process bar
                    try
                    {
                        fileCount = fileCount + 1;
                        double dValue = pbValue * fileCount;

                        if (dValue > 100.0)
                            dValue = 100.0;

                        BPbar.Value = (int)dValue;
                    }
                    catch { }

                    // Get the file info
                    FileInfo info = (FileInfo)e.UserState;

                    // Get the log file details

                    // Find the file
                    ListViewItem item = DwgList.Items[info._index];

                    item.BackColor = itemColor;

                    // Get the file name
                    TagData data = (TagData)item.Tag;

                    if (e.ProgressPercentage == CLOSE_DWG_SUCCESS)
                    {
                        item.SubItems[2].Text = "Done";
                        info._status = true;
                        data.status = true;
                    }
                    else if (e.ProgressPercentage == CLOSE_DWG_FAILED)
                    {
                        item.SubItems[2].Text = "Failed";
                        item.ForeColor = System.Drawing.Color.Red;
                        info._status = false;
                        data.status = false;

                    }
                    info._timeDate = DateTime.Now.ToString();
                    info._processed = true;

                    if (bplog != null)
                    {
                        string acadLog = "";

                        if (useCmdLine)
                        {
                            //log
                            acadLog = info._logFile;
                            if (acadLog.Length == 0)
                            {
                                acadLog =
                                  "No detail log when running script as commandline argument for AutoCAD";
                            }

                            // Log the details
                            acadLog = acadLog.Replace("\b", "");

                            bplog.Log(
                              data.DwgName, acadLog, _projectName, data.status
                            );
                        }
                        else
                        {
                            if (!runWithoutOpen)
                            {

                                if (File.Exists(info._logFile))
                                {
                                    //
                                    try
                                    {
                                        StreamReader SR = File.OpenText(info._logFile);
                                        acadLog = SR.ReadToEnd();
                                        SR.Close();
                                    }
                                    catch { }
                                }

                                if (acadLog.Length == 0)
                                {
                                    acadLog =
                                      "Error while reading log file for " +
                                      info._fileName + "\n";
                                }

                                if (!string.IsNullOrEmpty(info._diagLog))
                                    acadLog = info._diagLog + Environment.NewLine + acadLog;

                                // Log the details
                                bplog.Log(
                                  data.DwgName, acadLog, _projectName, data.status
                                );
                            }
                            else
                            {
                                acadLog =
                                  "No detail log when running script without opening drawing file" +
                                           "\n";
                                bplog.Log(
                             data.DwgName, acadLog, _projectName, data.status
                           );
                            }
                        }
                    }

                }
                if (_stopBatchProcess)
                    batchProcessThread.CancelAsync();

                // Release the waiting thread

                Threadinput.ThreadEvent.Set();
            }
            catch { }
        }

        // Timer functions...

        // Timer elapsed function

        void _timeout_Elapsed(
          object sender, System.Timers.ElapsedEventArgs e
        )
        {
            //no work if commandline working
            if (useCmdLine)
                return;

            try
            {
                if (batchProcessThread.IsBusy)
                {
                    if (!diagnosticMode)
                    {
                        _timeout.Stop();
                        _killAcad = true;
                    }
                }
                else
                {
                    batchProcessThread.CancelAsync();
                }
            }
            catch { }
        }

        // Timeout thread main function
        void _timeoutWk_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker timer = sender as BackgroundWorker;

            while (!timer.CancellationPending) //infinite loop
            {
                Thread.Sleep(2000);//sleep 2 second

                // change on 20-10-2010...
                if (DrawingListControl._killAcad)
                    timer.ReportProgress(0, null);
            }
        }

        // Function which is called when time out occurs
        // This function is kill from Main thread, so you
        // kill AutoCAD here
        void _timeoutWk_ProgressChanged(
          object sender, ProgressChangedEventArgs e
        )
        {
            if (_killAcad)
            {
                // Take the screenshot if required
                try
                {
                    if (!_imageCreated && createImage == 1)
                    {
                        _imageCreated = true;
                        captureScreen(
                          _currentDWG, _logFilePath,
                          ref _left, ref _top, ref _width, ref _height
                        );
                    }
                }
                catch { }

                KillAutoCAD();
                _killAcad = false;

            }
        }

        // Function which updates the host application.
        // late binding is used... just in case we use any other 
        // host application
        void UpdateHostApplicationUI(bool processStarted)
        {
            try
            {
                Type myType = this.HostApplication.GetType();
                MethodInfo myMethodInfo = myType.GetMethod("ProcessStatus");

                ParameterInfo[] myParameters = myMethodInfo.GetParameters();
                object[] OnedataArray = new object[1];
                OnedataArray[0] = processStarted;
                myMethodInfo.Invoke(HostApplication, OnedataArray);

                _isProcessRunning = processStarted;
            }
            catch { }
        }


        // ================= PDF EXPORT VIA COM =================
        const string PDF_PC3 = "Dwg To PDF.pc3";
        const int AC_PLOT_LAYOUT = 5;
        const int AC_ROTATE_90_PDF = 1;
        const long PDF_MIN_BYTES = 30 * 1024;

        // Trần tuyệt đối: quá mức này thì để timer kill như cũ (phòng AutoCAD treo thật)
        const int OPEN_MAX_SEC = 120;     // 15 phút để mở 1 bản vẽ
        const int EXPORT_MAX_SEC = 120;  // 20 phút cho quét entity + plot

        int HeartbeatPeriodMs()
        {
            return Math.Max(500, Math.Min(3000, _timeoutSec * 1000 / 3));
        }

        // Chờ AutoCAD thật sự rảnh (bản cũ IsAcadQuiescent(120,1000) hết 2 phút là bỏ qua và plot luôn)
        bool WaitAcadReady(int maxSec)
        {
            DateTime end = DateTime.Now.AddSeconds(maxSec);
            while (DateTime.Now < end)
            {
                if (IsAcadQuiescent(3, 1000)) return true;
            }
            return false;
        }

        // ================= CẤU HÌNH PNG =================
        const string PNG_PC3 = "PublishToWeb PNG.pc3";

        // Margin = tỉ lệ của cạnh lớn nhất của bbox (0.02 = 2%)
        double _pngMarginRatio = 0.02;

        // Ngưỡng outlier
        const double PNG_MAD_MULTIPLIER = 20.0;
        const double PNG_MAD_FLOOR = 1.0;

        // Ít hơn số này thì không lọc outlier (MAD không đáng tin)
        const int PNG_MIN_ENTITIES_FOR_FILTER = 5;

        // Bỏ entity trên layer tắt/đóng băng và entity Visible = false
        bool _pngSkipHiddenLayers = true;

        //Giới hạn tổng số pixel (rộng x cao). 0 = không giới hạn (chọn lớn nhất, ví dụ 16K)
        //33177600 = 8K UHD (7680x4320)
        double _pngMaxPixelArea = 0;

        // Giá trị BACKGROUNDPLOT gốc, để khôi phục ở lần quit cuối
        object _bgPlotOriginal = null;

        // AcPlotType / AcPlotRotation / AcPlotScale (giá trị COM chuẩn)
        const int AC_PLOT_WINDOW = 4;

        const int AC_PLOT_DISPLAY = 0;
        const int AC_PLOT_EXTENTS = 1;
        const int AC_ROTATE_0 = 0;
        const int AC_ROTATE_90 = 1;
        const int AC_SCALE_TO_FIT = 0;

        class PngEntInfo
        {
            public string Handle, Layer, Type;
            public double MinX, MinY, MaxX, MaxY, Cx, Cy;
        }

        // using System.Drawing.Imaging;
        double PngInkRatio(string path, StringBuilder sb)
        {
            try
            {
                using (Bitmap bmp = new Bitmap(path))
                {
                    int W = bmp.Width, H = bmp.Height, step = Math.Max(1, Math.Max(W, H) / 4000);
                    BitmapData d = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    byte[] row = new byte[Math.Abs(d.Stride)];
                    long ink = 0, total = 0;
                    for (int y = 0; y < H; y += step)
                    {
                        Marshal.Copy(IntPtr.Add(d.Scan0, y * d.Stride), row, 0, W * 3);
                        for (int x = 0; x < W; x += step)
                        {
                            total++;
                            if (row[x * 3] < 250 || row[x * 3 + 1] < 250 || row[x * 3 + 2] < 250) ink++;
                        }
                    }
                    bmp.UnlockBits(d);
                    sb.AppendLine(string.Format("[PNG] Ảnh {0}x{1}, pixel không trắng = {2:P4}", W, H, (double)ink / total));
                    return (double)ink / total;
                }
            }
            catch (Exception ex) { sb.AppendLine("[PNG] Không đọc được ảnh: " + ex.Message); return -1; }
        }


        void ApplyPlotArea(dynamic doc, dynamic layout, int mode,
                   double minX, double minY, double maxX, double maxY, StringBuilder sb)
        {
            if (mode == AC_PLOT_WINDOW)
            {
                layout.SetWindowToPlot(new double[] { minX, minY }, new double[] { maxX, maxY });
                layout.PlotType = AC_PLOT_WINDOW;
            }
            else if (mode == AC_PLOT_EXTENTS)
            {
                layout.PlotType = AC_PLOT_EXTENTS;
            }
            else // Display: zoom vào vùng cần plot rồi plot đúng màn hình
            {
                doc.Application.ZoomWindow(new double[] { minX, minY, 0 }, new double[] { maxX, maxY, 0 });
                layout.PlotType = AC_PLOT_DISPLAY;
            }
            layout.PlotRotation = AC_ROTATE_0;
           // layout.CenterPlot = true;
            layout.CenterPlot = false;
            try { layout.PlotOrigin = new double[] { 0, 0 }; } catch { }
            layout.UseStandardScale = true;
            layout.StandardScale = AC_SCALE_TO_FIT;
            layout.PlotWithLineweights = true;
            sb.AppendLine("[PNG] Kiểu vùng plot: " + (mode == 4 ? "Window" : mode == 1 ? "Extents" : "Display"));
        }

        void ApplyPdfArea(dynamic doc, dynamic layout, int mode,
                  double minX, double minY, double maxX, double maxY, StringBuilder sb)
        {
            if (mode == AC_PLOT_WINDOW)
            {
                layout.SetWindowToPlot(new double[] { minX, minY }, new double[] { maxX, maxY });
                layout.PlotType = AC_PLOT_WINDOW;
            }
            else if (mode == AC_PLOT_EXTENTS)
            {
                layout.PlotType = AC_PLOT_EXTENTS;
            }
            else if (mode == AC_PLOT_DISPLAY)
            {
                doc.Application.ZoomWindow(new double[] { minX, minY, 0 }, new double[] { maxX, maxY, 0 });
                layout.PlotType = AC_PLOT_DISPLAY;
            }
            else
            {
                layout.PlotType = AC_PLOT_LAYOUT;
            }
            sb.AppendLine("[PDF] Kiểu vùng plot: " +
                (mode == AC_PLOT_WINDOW ? "Window" : mode == AC_PLOT_EXTENTS ? "Extents" :
                 mode == AC_PLOT_DISPLAY ? "Display" : "Layout"));
        }

        // ================= ENTRY POINT =================
        // Gọi từ batchProcessThread_DoWork sau khi Open + IsAcadQuiescent.
        bool ExportPngViaCom(object activeDocument, FileInfo info, out string log)
        {
            StringBuilder sb = new StringBuilder();
            bool ok = false;
            try
            {
                dynamic doc = activeDocument;
                string layoutName = info._selectedLayout;

                sb.AppendLine("[PNG] Drawing: " + info._fileName);
                if (string.IsNullOrWhiteSpace(layoutName))
                    throw new Exception("Chưa chọn Layout cho file này.");
                sb.AppendLine("[PNG] Layout: " + layoutName);

                // 1. Layout + activate
                dynamic layout = doc.Layouts.Item(layoutName);
                doc.ActiveLayout = layout;

                // 2. Plot sync (tránh IsAcadQuiescent trả về trước khi file xong)
                SetBackgroundPlotOff(doc);

                // 3. PC3 + media pixel lớn nhất
                layout.ConfigName = PNG_PC3;
                layout.RefreshPlotDeviceInfo();
                BoostLineweights(doc, layout, sb);
                //string mediaName; double mediaW, mediaH;
                //PickLargestPixelMedia(layout, sb, out mediaName, out mediaW, out mediaH);
                //layout.CanonicalMediaName = mediaName;

                // 4. Entity -> bbox
                List<PngEntInfo> ents = CollectEntityInfos(doc, layout, sb);
                if (ents.Count == 0)
                    throw new Exception("Không có entity hợp lệ để tính vùng plot.");

                sb.AppendLine(string.Format(
                    "[PNG] Unfiltered bbox: ({0:F3},{1:F3}) - ({2:F3},{3:F3}) from {4} entities",
                    ents.Min(x => x.MinX), ents.Min(x => x.MinY),
                    ents.Max(x => x.MaxX), ents.Max(x => x.MaxY), ents.Count));
                try
                {
                    sb.AppendLine("[PNG] WORLDUCS=" + doc.GetVariable("WORLDUCS"));
                    double[] em = (double[])doc.GetVariable("EXTMIN");
                    double[] ex2 = (double[])doc.GetVariable("EXTMAX");
                    sb.AppendLine(string.Format("[PNG] (chỉ để so sánh) EXTMIN=({0:F3},{1:F3}) EXTMAX=({2:F3},{3:F3})",
                        em[0], em[1], ex2[0], ex2[1]));
                }
                catch { }

                // 5. Median + MAD
                List<PngEntInfo> kept = FilterOutliersMedianMad(ents, sb);

                if (kept.Count == 0)
                    throw new Exception("Sau khi lọc outlier không còn entity nào.");

                // 6. Bbox cuối + margin
                double minX = kept.Min(e => e.MinX), minY = kept.Min(e => e.MinY);
                double maxX = kept.Max(e => e.MaxX), maxY = kept.Max(e => e.MaxY);
                double w = maxX - minX, h = maxY - minY;
                double pad = Math.Max(Math.Max(w, h) * _pngMarginRatio, 1.0);
                minX -= pad; minY -= pad; maxX += pad; maxY += pad;
                w = maxX - minX; h = maxY - minY;
                sb.AppendLine(string.Format(
                    "[PNG] Final window: ({0:F3},{1:F3}) - ({2:F3},{3:F3}) size {4:F3} x {5:F3}, margin {6:F3}",
                    minX, minY, maxX, maxY, w, h, pad));

                // 7. Cấu hình layout
                double[] ll = new double[] { minX, minY };
                double[] ur = new double[] { maxX, maxY };
                //layout.SetWindowToPlot(ll, ur);
                //layout.PlotType = AC_PLOT_WINDOW;

                ////bool winLandscape = w >= h;
                ////bool mediaLandscape = mediaW >= mediaH;
                ////layout.PlotRotation = (winLandscape == mediaLandscape) ? AC_ROTATE_0 : AC_ROTATE_90;
                //layout.PlotRotation = AC_ROTATE_0;   // không xoay: giữ hướng bản đồ

                //layout.CenterPlot = true;
                //layout.UseStandardScale = true;
                //layout.StandardScale = AC_SCALE_TO_FIT;
                //layout.PlotWithLineweights = true;

                string pngPath = Path.ChangeExtension(info._fileName, ".png");
                // Source - https://stackoverflow.com/a/42742496
                // Posted by Legends, modified by community. See post 'Timeline' for change history
                // Retrieved 2026-10-02, License - CC BY-SA 3.0
                string getFileName = Path.GetFileName(pngPath); // --> image.png
                string getFolder = CreateFolder(info._fileName);
                string FullFolder = Path.Combine(getFolder, getFileName);
                try { doc.Plot.QuietErrorMode = true; } catch { }
                try { doc.Plot.SetLayoutsToPlot(new object[] { (object)layout }); } catch { }

                int[] modes = { AC_PLOT_WINDOW, AC_PLOT_EXTENTS, AC_PLOT_DISPLAY };
                double[] caps = { _pngMaxPixelArea, 33177600 };

                foreach (int mode in modes)
                {
                    try { ApplyPlotArea(doc, layout, mode, minX, minY, maxX, maxY, sb); }
                    catch (Exception ex)
                    {
                        sb.AppendLine("[PNG] Áp kiểu vùng plot lỗi: " + (ex.InnerException ?? ex).Message);
                        continue;
                    }

                    foreach (double cap in caps)
                    {
                        try { if (File.Exists(FullFolder)) File.Delete(FullFolder); } catch { }

                        string mediaName;
                        PickBestPixelMedia(layout, w, h, cap, sb, out mediaName);
                        layout.CanonicalMediaName = mediaName;

                        sb.AppendLine("[PNG] PlotToFile -> " + FullFolder);
                        object plotResult = doc.Plot.PlotToFile(FullFolder, PNG_PC3);
                        sb.AppendLine("[PNG] PlotToFile returned: " + plotResult);

                        bool exists = false;
                        for (int i = 0; i < 60; i++)
                        {
                            if (File.Exists(FullFolder) && new System.IO.FileInfo(FullFolder).Length > 0) { exists = true; break; }
                            Thread.Sleep(500);
                        }
                        if (!exists) { sb.AppendLine("[PNG] Không thấy file, thử tiếp."); continue; }

                        double ink = PngInkRatio(FullFolder, sb);
                        if (ink > 0) { ok = true; break; }

                        sb.AppendLine("[PNG] Ảnh trắng, thử tiếp.");
                    }
                    if (ok) break;
                }
                sb.AppendLine(ok ? "[PNG] OK: ảnh có nội dung." : "[PNG] FAILED: mọi kiểu vùng plot đều ra ảnh trắng.");
            }
            catch (Exception ex)
            {
                Exception inner = ex.InnerException ?? ex;
                sb.AppendLine("[PNG] FAILED: " + inner.GetType().Name + " - " + inner.Message);
                ok = false;
            }

            log = sb.ToString();
            return ok;
        }

        // =====================================================================
        // ===== LÀM ĐẬM NÉT CHO PNG (giữ nguyên độ phân giải 16K) =====
        // ===== DÁN vào trong class DrawingListControl, ví dụ ngay TRƯỚC dòng =====
        // ===== "// ================= BACKGROUNDPLOT =================" =====
        // =====================================================================

        // Lineweight tối thiểu khi xuất PNG, đơn vị 1/100 mm (35 = 0.35 mm).
        // Giá trị hợp lệ của AutoCAD: 5, 9, 13, 15, 18, 20, 25, 30, 35, 40, 50, 53, 60, 70, 80, 90, 100, 106, 120, 140, 158, 200, 211
        // 0 = tắt (giữ nguyên lineweight của bản vẽ).
        // Chỉ NÂNG các nét mảnh hơn mức này; nét đã đậm hơn (ví dụ nét viền nhà) giữ nguyên.
        int _pngMinLineWeight = 35;

        // Thay đổi chỉ nằm trong bộ nhớ; bản vẽ được đóng bằng Close(false) nên KHÔNG bị lưu vào file DWG.
        void BoostLineweights(dynamic doc, dynamic layout, StringBuilder sb)
        {
            if (_pngMinLineWeight <= 0)
            {
                sb.AppendLine("[PNG][LW] Tắt: giữ nguyên lineweight của bản vẽ.");
                return;
            }

            // 1. Ghi lại cấu hình plot style: nếu có CTB gán lineweight theo màu thì lineweight của layer có thể bị ghi đè
            try
            {
                sb.AppendLine("[PNG][LW] StyleSheet='" + layout.StyleSheet
                    + "', PlotWithPlotStyles=" + layout.PlotWithPlotStyles
                    + ", PlotWithLineweights=" + layout.PlotWithLineweights
                    + ", ScaleLineweights=" + layout.ScaleLineweights);
            }
            catch (Exception ex)
            {
                sb.AppendLine("[PNG][LW] Không đọc được cấu hình plot style: " + (ex.InnerException ?? ex).Message);
            }

            // 2. LWDEFAULT: áp cho mọi đối tượng/layer đang dùng lineweight "Default"
            try
            {
                object old = doc.GetVariable("LWDEFAULT");
                try { doc.SetVariable("LWDEFAULT", (short)_pngMinLineWeight); }
                catch { doc.SetVariable("LWDEFAULT", _pngMinLineWeight); }
                sb.AppendLine("[PNG][LW] LWDEFAULT: " + old + " -> " + _pngMinLineWeight);
            }
            catch (Exception ex)
            {
                sb.AppendLine("[PNG][LW] Không đặt được LWDEFAULT: " + (ex.InnerException ?? ex).Message);
            }

            // 3. Layer có lineweight riêng mảnh hơn mức tối thiểu thì nâng lên
            int total = 0, raised = 0;
            try
            {
                dynamic layers = doc.Layers;
                int n = (int)layers.Count;
                for (int i = 0; i < n; i++)
                {
                    try
                    {
                        dynamic lay = layers.Item(i);
                        int lw = Convert.ToInt32(lay.Lineweight);
                        total++;
                        // Giá trị âm là ByLayer/ByBlock/Default (Default đã được LWDEFAULT xử lý ở trên)
                        if (lw >= 0 && lw < _pngMinLineWeight)
                        {
                            lay.Lineweight = _pngMinLineWeight;
                            raised++;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("[PNG][LW] Lỗi duyệt layer: " + (ex.InnerException ?? ex).Message);
            }

            sb.AppendLine(string.Format(
                "[PNG][LW] Layer: {0}, đã nâng lineweight: {1}; mức tối thiểu = {2} ({3:F2} mm).",
                total, raised, _pngMinLineWeight, _pngMinLineWeight / 100.0));
        }
        // ===== HẾT KHỐI LÀM ĐẬM NÉT =====

        // ================= BACKGROUNDPLOT =================
        void SetBackgroundPlotOff(dynamic doc)
        {
            try
            {
                if (_bgPlotOriginal == null)
                    _bgPlotOriginal = doc.GetVariable("BACKGROUNDPLOT");
                doc.SetVariable("BACKGROUNDPLOT", (short)0);
            }
            catch { }
        }

        // ================= MEDIA =================
        // Canonical name thường dạng: Sun_Hi-Res_(1600.00_x_1280.00_Pixels)
        static readonly System.Text.RegularExpressions.Regex PixelMediaRegex = new System.Text.RegularExpressions.Regex(
            @"(\d+(?:\.\d+)?)[_\s]*x[_\s]*(\d+(?:\.\d+)?)[_\s]*\)?[_\s]*Pixels",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);


        void PickBestPixelMedia(dynamic layout, double winW, double winH, double maxPixels,
                        StringBuilder sb, out string bestName)
        {
            Array names = (Array)layout.GetCanonicalMediaNames();
            bestName = null;
            double bestScore = -1;

            foreach (object o in names)
            {
                string n = o as string;
                if (string.IsNullOrEmpty(n)) continue;
                var m = PixelMediaRegex.Match(n);
                if (!m.Success) continue;

                double pw = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                double ph = double.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                if (maxPixels > 0 && pw * ph > maxPixels) continue;

                double s = Math.Min(pw / winW, ph / winH);
                double score = (winW * s) * (winH * s);      // số pixel thực sự chứa bản vẽ
                if (score > bestScore) { bestScore = score; bestName = n; }
            }

            if (bestName == null) throw new Exception("Không có media pixel phù hợp (cap=" + maxPixels + ").");
            sb.AppendLine(string.Format("[PNG] Media chọn: {0} (cap={1}, pixel hiệu dụng ~{2:F0})", bestName, maxPixels, bestScore));
        }

        // ================= ENTITY =================
        List<PngEntInfo> CollectEntityInfos(dynamic doc, dynamic layout, StringBuilder sb)
        {
            List<PngEntInfo> list = new List<PngEntInfo>();
            Dictionary<string, bool> layerVisible = new Dictionary<string, bool>();
            dynamic block = layout.Block;   // Model: block Model; Layout: block paper space
            int count = (int)block.Count;
            sb.AppendLine(string.Format("[PNG] Space: {0}, entity count: {1}",
                (bool)layout.ModelType ? "ModelSpace" : "PaperSpace", count));

            int skipped = 0, mtextSamples = 0;

            for (int i = 0; i < count; i++)
            {
                if (i % 200 == 0 && _timeout != null)
                {
                    _timeout.Stop();
                    _timeout.Start();   // còn chạy thì reset đồng hồ, chỉ kill khi đứng im thật
                }
                string handle = "?", layer = "?", type = "?";
                try
                {
                    dynamic ent = block.Item(i);
                    handle = (string)ent.Handle;
                    layer = (string)ent.Layer;
                    type = (string)ent.ObjectName;

                    if (type == "AcDbXline" || type == "AcDbRay")
                    {
                        skipped++;
                        sb.AppendLine(string.Format("[PNG] Skip {0} {1} {2}: vô hạn, không có bbox", handle, layer, type));
                        continue;
                    }

                    if (_pngSkipHiddenLayers)
                    {
                        bool visible = true;
                        try { visible = (bool)ent.Visible; } catch { }
                        if (!visible || !IsLayerVisible(doc, layer, layerVisible))
                        {
                            skipped++;
                            sb.AppendLine(string.Format("[PNG] Skip {0} {1} {2}: layer tắt/đóng băng hoặc entity ẩn", handle, layer, type));
                            continue;
                        }
                    }

                    object omin = null, omax = null;
                    ent.GetBoundingBox(out omin, out omax);
                    double[] mn = (double[])omin;
                    double[] mx = (double[])omax;

                    if (!IsFinite(mn[0]) || !IsFinite(mn[1]) || !IsFinite(mx[0]) || !IsFinite(mx[1]))
                    {
                        skipped++;
                        sb.AppendLine(string.Format("[PNG] Skip {0} {1} {2}: bbox không hợp lệ", handle, layer, type));
                        continue;
                    }

                    // Số liệu chẩn đoán MTEXT: so bbox với Width (khung)
                    if (type == "AcDbMText" && mtextSamples < 5)
                    {
                        mtextSamples++;
                        try
                        {
                            sb.AppendLine(string.Format(
                                "[PNG][MTEXT] {0}: bboxWidth={1:F3}, Width(frame)={2:F3}, Height={3:F3}",
                                handle, mx[0] - mn[0], (double)ent.Width, (double)ent.Height));
                        }
                        catch { }
                    }

                    list.Add(new PngEntInfo
                    {
                        Handle = handle,
                        Layer = layer,
                        Type = type,
                        MinX = mn[0],
                        MinY = mn[1],
                        MaxX = mx[0],
                        MaxY = mx[1],
                        Cx = (mn[0] + mx[0]) / 2.0,
                        Cy = (mn[1] + mx[1]) / 2.0
                    });
                }
                catch (Exception ex)
                {
                    Exception inner = ex.InnerException ?? ex;
                    int hr = (inner as COMException)?.HResult ?? 0;
                    if (hr == unchecked((int)0x800706BA) || hr == unchecked((int)0x800706BE)
                        || inner.Message.Contains("0x800706B"))
                        throw new Exception("AutoCAD mất kết nối (RPC) khi quét entity #" + i + "/" + count);

                    skipped++;
                    sb.AppendLine(string.Format("[PNG] Skip {0} {1} {2}: không lấy được bbox ({3})",
                        handle, layer, type, inner.Message));
                }
            }

            sb.AppendLine(string.Format("[PNG] Entity hợp lệ: {0}, bỏ qua: {1}", list.Count, skipped));
            return list;
        }

        bool IsLayerVisible(dynamic doc, string layerName, Dictionary<string, bool> cache)
        {
            bool v;
            if (cache.TryGetValue(layerName, out v)) return v;
            try
            {
                dynamic l = doc.Layers.Item(layerName);
                //v = (bool)l.LayerOn && !(bool)l.Freeze;
                v = (bool)l.LayerOn && !(bool)l.Freeze && (bool)l.Plottable;
            }
            catch { v = true; }
            cache[layerName] = v;
            return v;
        }

        static bool IsFinite(double d)
        {
            return !double.IsNaN(d) && !double.IsInfinity(d) && Math.Abs(d) < 1e15;
        }

        // ================= MEDIAN + MAD =================
        static double Median(List<double> values)
        {
            if (values.Count == 0) return 0;
            List<double> s = values.OrderBy(x => x).ToList();
            int n = s.Count;
            return (n % 2 == 1) ? s[n / 2] : (s[n / 2 - 1] + s[n / 2]) / 2.0;
        }

        List<PngEntInfo> FilterOutliersMedianMad(List<PngEntInfo> ents, StringBuilder sb)
        {
            if (ents.Count < PNG_MIN_ENTITIES_FOR_FILTER)
            {
                sb.AppendLine(string.Format("[PNG] Chỉ có {0} entity (< {1}), không lọc outlier.",
                    ents.Count, PNG_MIN_ENTITIES_FOR_FILTER));
                return ents;
            }

            double medX = Median(ents.Select(e => e.Cx).ToList());
            double medY = Median(ents.Select(e => e.Cy).ToList());
            double madX = Math.Max(Median(ents.Select(e => Math.Abs(e.Cx - medX)).ToList()), PNG_MAD_FLOOR);
            double madY = Math.Max(Median(ents.Select(e => Math.Abs(e.Cy - medY)).ToList()), PNG_MAD_FLOOR);
            double thX = PNG_MAD_MULTIPLIER * madX;
            double thY = PNG_MAD_MULTIPLIER * madY;

            sb.AppendLine(string.Format(
                "[PNG] Median=({0:F3},{1:F3}) MAD=({2:F3},{3:F3}) Threshold=({4:F3},{5:F3})",
                medX, medY, madX, madY, thX, thY));

            List<PngEntInfo> kept = new List<PngEntInfo>();
            foreach (PngEntInfo e in ents)
            {
                double dx = Math.Abs(e.Cx - medX);
                double dy = Math.Abs(e.Cy - medY);
                List<string> reasons = new List<string>();
                if (dx > thX) reasons.Add(string.Format("|X-median|={0:F3} > {1:F3}", dx, thX));
                if (dy > thY) reasons.Add(string.Format("|Y-median|={0:F3} > {1:F3}", dy, thY));

                // Entity lớn (rộng/cao > 5*MAD) là nội dung thật như khung tên, bảng, ranh giới: không loại
                double bw = e.MaxX - e.MinX, bh = e.MaxY - e.MinY;
                if (reasons.Count > 0 && (bw > 5 * madX || bh > 5 * madY))
                {
                    sb.AppendLine(string.Format("[PNG] GIỮ entity lớn dù lệch tâm: Handle={0} Layer={1} Type={2} ({3:F1} x {4:F1})",
                        e.Handle, e.Layer, e.Type, bw, bh));
                    reasons.Clear();
                }

                if (reasons.Count == 0)
                    kept.Add(e);
                else
                    sb.AppendLine(string.Format(
                        "[PNG] OUTLIER Handle={0} Layer={1} Type={2} Center=({3:F3},{4:F3}) Reason: {5}",
                        e.Handle, e.Layer, e.Type, e.Cx, e.Cy, string.Join("; ", reasons)));
            }

            sb.AppendLine(string.Format("[PNG] Giữ {0}/{1} entity, loại {2}.",
                kept.Count, ents.Count, ents.Count - kept.Count));
            return kept;
        }
        // ===== HẾT KHỐI PNG EXPORT =====


        // ================= PDF EXPORT VIA COM =================
        static readonly System.Text.RegularExpressions.Regex PdfMediaRegex = new System.Text.RegularExpressions.Regex(
            @"ISO_full_bleed_A[0-3]_\((\d+(?:\.\d+)?)_x_(\d+(?:\.\d+)?)_MM\)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        // Trang có ít hơn số lệnh vẽ này thì coi là trắng (log in số đo thật để hiệu chỉnh)
        const int PDF_MIN_PAINT_OPS = 20;

        // HWND của phiên AutoCAD đã được "làm nóng" cho plot PDF
        string _pdfWarmedFor = null;

        void Trace(string msg)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(Path.GetTempPath(), "ScriptPro_trace.log"),
                    DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + Environment.NewLine);
            }
            catch { }
        }

        void KeepAlive()
        {
            try
            {
                if (_timeout != null && !_killAcad) { _timeout.Stop(); _timeout.Start(); }
            }
            catch { }
        }

        // Chọn khổ A0-A3 cùng HƯỚNG với cửa sổ (không xoay bản vẽ -> giữ hướng Bắc lên trên).
        // Chỉ khi không có khổ cùng hướng mới dùng khổ khác hướng + xoay 90 độ.
        void PickPdfMedia(dynamic layout, double winW, double winH, StringBuilder sb,
                          out string bestName, out bool needRotate)
        {
            Array names = (Array)layout.GetCanonicalMediaNames();
            bool winLandscape = winW >= winH;

            string nameSame = null, nameRot = null;
            double fillSame = -1, areaSame = -1, fillRot = -1, areaRot = -1;

            foreach (object o in names)
            {
                string n = o as string;
                if (string.IsNullOrEmpty(n)) continue;
                var m = PdfMediaRegex.Match(n);
                if (!m.Success) continue;

                double pw = double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                double ph = double.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                bool sameOrient = (pw >= ph) == winLandscape;
                double mw = sameOrient ? pw : ph;
                double mh = sameOrient ? ph : pw;

                double s = Math.Min(mw / winW, mh / winH);
                double fill = (winW * s * winH * s) / (mw * mh);
                double area = pw * ph;

                if (sameOrient)
                {
                    if (fill > fillSame + 0.01 || (Math.Abs(fill - fillSame) <= 0.01 && area > areaSame))
                    { fillSame = fill; areaSame = area; nameSame = n; }
                }
                else
                {
                    if (fill > fillRot + 0.01 || (Math.Abs(fill - fillRot) <= 0.01 && area > areaRot))
                    { fillRot = fill; areaRot = area; nameRot = n; }
                }
            }

            if (nameSame != null)
            {
                bestName = nameSame; needRotate = false;
                sb.AppendLine(string.Format("[PDF] Khổ giấy: {0}, không xoay, độ lấp đầy={1:P0}", bestName, fillSame));
            }
            else if (nameRot != null)
            {
                bestName = nameRot; needRotate = true;
                sb.AppendLine(string.Format("[PDF] Khổ giấy (không có khổ cùng hướng): {0}, xoay 90, độ lấp đầy={1:P0}", bestName, fillRot));
            }
            else
            {
                bestName = null; needRotate = false;
                throw new Exception("Dwg To PDF.pc3 không có khổ ISO A0-A3.");
            }
        }

        // ---------- Chờ file PDF ghi xong ----------
        // Xong = tồn tại, kích thước không đổi qua 3 lần kiểm tra liên tiếp, và mở độc quyền được (AutoCAD đã nhả file).
        bool WaitPdfReady(string path, int maxSec, StringBuilder sb)
        {
            DateTime end = DateTime.Now.AddSeconds(maxSec);
            long last = -1;
            int stable = 0;
            while (DateTime.Now < end)
            {
                KeepAlive();
                try
                {
                    if (File.Exists(path))
                    {
                        long len = new System.IO.FileInfo(path).Length;
                        if (len > 0 && len == last) stable++; else stable = 0;
                        last = len;
                        if (stable >= 3)
                        {
                            try
                            {
                                using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                                return true;
                            }
                            catch (IOException) { stable = 0; }   // AutoCAD còn giữ file
                        }
                    }
                }
                catch { }
                Thread.Sleep(700);
            }
            sb.AppendLine("[PDF] Hết thời gian chờ file ổn định: " + path);
            return false;
        }

        // ---------- Đo nội dung trang PDF (không cần thư viện) ----------
        static byte[] TryInflate(byte[] raw)
        {
            if (raw.Length < 3) return null;
            try
            {
                using (MemoryStream ms = new MemoryStream(raw, 2, raw.Length - 2))   // bỏ 2 byte header zlib
                using (System.IO.Compression.DeflateStream ds =
                    new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Decompress))
                using (MemoryStream outp = new MemoryStream())
                {
                    ds.CopyTo(outp);
                    return outp.ToArray();
                }
            }
            catch { return null; }
        }

        // Đếm các lệnh vẽ của PDF: m l c v y (đường), re (hình chữ nhật), Tj TJ (chữ)
        static long CountPaintOps(byte[] b)
        {
            long n = 0;
            int i = 0, len = b.Length;
            while (i < len)
            {
                while (i < len && b[i] <= 32) i++;
                int st = i;
                while (i < len && b[i] > 32) i++;
                int tl = i - st;
                if (tl == 1)
                {
                    byte c = b[st];
                    if (c == (byte)'m' || c == (byte)'l' || c == (byte)'c' || c == (byte)'v' || c == (byte)'y') n++;
                }
                else if (tl == 2)
                {
                    if ((b[st] == (byte)'r' && b[st + 1] == (byte)'e') ||
                        (b[st] == (byte)'T' && (b[st + 1] == (byte)'j' || b[st + 1] == (byte)'J'))) n++;
                }
            }
            return n;
        }

        // Trả về true nếu đo được (tìm thấy luồng nội dung dạng chữ). Font (nhị phân) và CMap bị loại.
        bool PdfMeasure(string path, out int contentBytes, out long paintOps, out int contentStreams)
        {
            contentBytes = 0; paintOps = 0; contentStreams = 0;
            try
            {
                byte[] data = File.ReadAllBytes(path);
                Encoding lat = Encoding.GetEncoding("iso-8859-1");
                string txt = lat.GetString(data);      // latin-1: chỉ số ký tự == chỉ số byte
                int pos = 0;

                while (pos < txt.Length)
                {
                    int k = txt.IndexOf("stream", pos, StringComparison.Ordinal);
                    if (k < 0) break;
                    if (k >= 3 && string.CompareOrdinal(txt, k - 3, "end", 0, 3) == 0) { pos = k + 6; continue; }

                    int s = k + 6;
                    if (s < data.Length && data[s] == 13) s++;
                    if (s < data.Length && data[s] == 10) s++;
                    int endPos = txt.IndexOf("endstream", s, StringComparison.Ordinal);
                    if (endPos < 0) break;
                    int len = endPos - s;
                    pos = endPos + 9;
                    if (len < 2) continue;

                    byte[] raw = new byte[len];
                    Buffer.BlockCopy(data, s, raw, 0, len);
                    byte[] dec = TryInflate(raw) ?? raw;
                    if (dec.Length == 0) continue;

                    int printable = 0;
                    for (int i = 0; i < dec.Length; i++)
                    {
                        byte bb = dec[i];
                        if ((bb >= 32 && bb < 127) || bb == 9 || bb == 10 || bb == 13) printable++;
                    }
                    if (printable < dec.Length * 0.9) continue;            // font / ảnh / nhị phân

                    if (dec.Length < 300000)
                    {
                        string d = lat.GetString(dec);
                        if (d.IndexOf("begincmap", StringComparison.Ordinal) >= 0 ||
                            d.IndexOf("CIDInit", StringComparison.Ordinal) >= 0) continue;   // CMap
                    }

                    contentStreams++;
                    if (dec.Length > contentBytes) contentBytes = dec.Length;
                    paintOps += CountPaintOps(dec);
                }
                return contentStreams > 0;
            }
            catch { return false; }
        }

        // ---------- Xuất PDF ----------
        bool ExportPdfViaCom(object activeDocument, FileInfo info, out string log)
        {
            StringBuilder sb = new StringBuilder();
            bool ok = false;
            try
            {
                dynamic doc = activeDocument;
                string layoutName = info._selectedLayout;
                sb.AppendLine("[PDF] Drawing: " + info._fileName);
                if (string.IsNullOrWhiteSpace(layoutName))
                    throw new Exception("Chưa chọn Layout cho file này.");
                sb.AppendLine("[PDF] Layout: " + layoutName);

                dynamic layout = doc.Layouts.Item(layoutName);
                doc.ActiveLayout = layout;
                SetBackgroundPlotOff(doc);

                // Lần plot PDF đầu tiên của phiên AutoCAD này: chờ AutoCAD thật sự rảnh
                if (_pdfWarmedFor != _acadObjectId)
                {
                    sb.AppendLine("[PDF] Lần plot PDF đầu tiên của phiên AutoCAD này: chờ ổn định trước khi plot.");
                    IsAcadQuiescent(30, 500);
                    Thread.Sleep(2000);
                    KeepAlive();
                    _pdfWarmedFor = _acadObjectId;
                }

                layout.ConfigName = PDF_PC3;
                layout.RefreshPlotDeviceInfo();
                BoostLineweights(doc, layout, sb);

                bool isModel = (bool)layout.ModelType;
                string pdfPath = Path.ChangeExtension(info._fileName, ".pdf");
                string getFileName = Path.GetFileName(pdfPath); // --> image.png
                string getFolder = CreateFolder(info._fileName);
                string FullFolder = Path.Combine(getFolder, getFileName);

                double w = 0, h = 0;
                double minX = 0, minY = 0, maxX = 0, maxY = 0;
                string media = null;
                int rotation = AC_ROTATE_0;

                if (isModel)
                {
                    // Cửa sổ plot: dùng lại bbox + lọc outlier như PNG
                    List<PngEntInfo> ents = CollectEntityInfos(doc, layout, sb);
                    if (ents.Count == 0) throw new Exception("Không có entity hợp lệ để tính vùng plot.");
                    List<PngEntInfo> kept = FilterOutliersMedianMad(ents, sb);
                    if (kept.Count == 0) throw new Exception("Sau khi lọc outlier không còn entity nào.");

                    minX = kept.Min(e => e.MinX);
                    minY = kept.Min(e => e.MinY);
                    maxX = kept.Max(e => e.MaxX);
                    maxY = kept.Max(e => e.MaxY);

                    w = maxX - minX; h = maxY - minY;
                    double pad = Math.Max(Math.Max(w, h) * _pngMarginRatio, 1.0);
                    minX -= pad; minY -= pad; maxX += pad; maxY += pad;
                    w = maxX - minX; h = maxY - minY;
                    sb.AppendLine(string.Format("[PDF] Window: ({0:F3},{1:F3}) - ({2:F3},{3:F3})", minX, minY, maxX, maxY));

                    bool rotate;
                    PickPdfMedia(layout, w, h, sb, out media, out rotate);
                    rotation = rotate ? AC_ROTATE_90_PDF : AC_ROTATE_0;
                }
                else
                {
                    sb.AppendLine("[PDF] Paper space: plot theo Layout.");
                }

                try { doc.Plot.QuietErrorMode = true; } catch { }
                try { doc.Plot.SetLayoutsToPlot(new object[] { (object)layout }); } catch { }

                // Kế hoạch: Window 2 lần (lần 2 là retry cho trường hợp plot đầu ra trang trắng / file chưa ghi xong),
                // rồi mới Extents, Display. Layout giấy: Layout 2 lần.
                int[] plan = isModel
                    ? new int[] { AC_PLOT_WINDOW, AC_PLOT_WINDOW, AC_PLOT_EXTENTS, AC_PLOT_DISPLAY }
                    : new int[] { AC_PLOT_LAYOUT, AC_PLOT_LAYOUT };

                for (int attempt = 0; attempt < plan.Length && !ok; attempt++)
                {
                    int mode = plan[attempt];
                    sb.AppendLine(string.Format("[PDF] --- Lần thử {0}/{1} ---", attempt + 1, plan.Length));

                    if (attempt > 0)
                    {
                        Thread.Sleep(1500);
                        IsAcadQuiescent(20, 500);
                        try { doc.ActiveLayout = layout; } catch { }
                        try { layout.RefreshPlotDeviceInfo(); } catch { }
                    }

                    // Áp lại toàn bộ cấu hình mỗi lần thử (phòng khi AutoCAD reset sau lần plot trước)
                    try
                    {
                        if (isModel)
                        {
                            if (media != null) layout.CanonicalMediaName = media;
                            layout.PlotRotation = rotation;
                        }
                        layout.CenterPlot = true;
                        layout.UseStandardScale = true;
                        layout.StandardScale = AC_SCALE_TO_FIT;
                        layout.PlotWithLineweights = true;
                        ApplyPdfArea(doc, layout, mode, minX, minY, maxX, maxY, sb);
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("[PDF] Áp cấu hình plot lỗi: " + (ex.InnerException ?? ex).Message);
                        continue;
                    }

                    try { if (File.Exists(FullFolder)) File.Delete(FullFolder); }
                    catch (Exception exDel) { sb.AppendLine("[PDF] Không xóa được file cũ: " + exDel.Message); }

                    KeepAlive();
                    object r = null;
                    try { r = doc.Plot.PlotToFile(FullFolder, PDF_PC3); }
                    catch (Exception ex)
                    {
                        sb.AppendLine("[PDF] PlotToFile ném lỗi: " + (ex.InnerException ?? ex).Message);
                    }
                    KeepAlive();
                    sb.AppendLine("[PDF] PlotToFile returned: " + r + " -> " + FullFolder);

                    IsAcadQuiescent(20, 500);

                    if (!WaitPdfReady(FullFolder, 90, sb))
                    {
                        sb.AppendLine("[PDF] File chưa sẵn sàng hoặc không có, thử lại.");
                        continue;
                    }

                    long size = new System.IO.FileInfo(FullFolder).Length;
                    int contentBytes; long paintOps; int streams;
                    bool measured = PdfMeasure(FullFolder, out contentBytes, out paintOps, out streams);
                    sb.AppendLine(string.Format(
                        "[PDF] File {0} bytes; đo nội dung: measured={1}, contentStreams={2}, contentBytes={3}, paintOps={4}",
                        size, measured, streams, contentBytes, paintOps));

                    // Đo được thì dựa vào số lệnh vẽ; không đo được thì quay về luật kích thước file
                    bool good = measured ? (paintOps >= PDF_MIN_PAINT_OPS) : (size >= PDF_MIN_BYTES);
                    if (good)
                        ok = true;
                    else
                        sb.AppendLine("[PDF] Trang nghi là trắng, thử lại.");
                }

                sb.AppendLine(ok ? "[PDF] OK: file có nội dung."
                                 : "[PDF] FAILED: mọi lần thử đều ra PDF trắng/không có file.");
            }
            catch (Exception ex)
            {
                Exception inner = ex.InnerException ?? ex;
                sb.AppendLine("[PDF] FAILED: " + inner.GetType().Name + " - " + inner.Message);
                ok = false;
            }
            log = sb.ToString();
            return ok;
        }
        // ===== HẾT KHỐI PDF EXPORT =====

        private void DrawingListControl_Load(object sender, EventArgs e)
        {

        }

        public void wizardDWGList()
        {
            //Wizard myWizard = new Wizard();
            //myWizard.prepareForStep1();
            WizardForm myWizard = new WizardForm();

            if (myWizard.ShowDialog() == DialogResult.OK)
            {
                newDWGList();

                acadExePath = myWizard.acadPath;

                useCmdLine = isHeadlessAcad(acadExePath);

                foreach (string dwgName in myWizard.dwgList)
                {
                    AddDWGtoView(dwgName, true);
                }

                runSelectedExe = false;

                if (acadExePath.Length != 0)
                    runSelectedExe = true;

                if (myWizard.startScriptPro)
                {
                    runCheckedFiles();
                }

            }
        }

        public void hideControlsForWizard()
        {
            this.Controls.Remove(label_filename);
            this.Controls.Remove(BPbar);
            this.Controls.Remove(scriptGBox);

            wizardMode = true;
            DwgList.CheckBoxes = false;
            DwgList.Dock = DockStyle.Fill;

            DwgList.Columns.RemoveAt(2);

            int width = DwgList.Width;

            DwgList.Columns[0].Width = (int)(width * 0.25);

            // Path
            DwgList.Columns[1].Width = (int)(width * 0.65);
        }

        public void populateDWGlist(List<string> list)
        {
            foreach (ListViewItem item in DwgList.Items)
            {
                TagData data = (TagData)item.Tag;
                list.Add(data.DwgName);
            }
        }
        // Xử lý sự kiện khi người dùng click vào danh sách DWG
        private void DwgList_MouseClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = DwgList.HitTest(e.Location);

            if (hit.Item == null || hit.SubItem == null)
                return;

            int columnIndex = hit.Item.SubItems.IndexOf(hit.SubItem);

            // Chỉ xử lý column SelectLayout
            if (columnIndex != 3)
                return;

            ShowLayoutComboBox(hit.Item, hit.SubItem);
        }

        private void LayoutComboBox_Leave(object sender, EventArgs e)
        {
            _layoutComboBox.Visible = false;
            _layoutComboBox.Tag = null;
        }

        private void LayoutComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_layoutComboBox.Tag is not ListViewItem item)
                return;

            if (_layoutComboBox.SelectedItem == null)
                return;

            //TagData tag = item.Tag as TagData;
            TagData tag = (TagData)item.Tag;

            if (tag == null)
                return;

            string selectedLayout =
                _layoutComboBox.SelectedItem.ToString();

            tag.SelectedLayout = selectedLayout;

            // Cập nhật text đang hiển thị trên ListView
            item.SubItems[3].Text = selectedLayout;
        }

        private void ShowLayoutComboBox(
            ListViewItem item,
            ListViewItem.ListViewSubItem subItem)
        {
            TagData tag = item.Tag as TagData;

            if (tag == null)
                return;

            if (tag.Layouts == null || tag.Layouts.Count == 0)
                return;

            Rectangle rect = subItem.Bounds;

            _layoutComboBox.BeginUpdate();

            _layoutComboBox.Items.Clear();

            foreach (string layout in tag.Layouts)
            {
                _layoutComboBox.Items.Add(layout);
            }

            _layoutComboBox.EndUpdate();

            // Vị trí ComboBox = vị trí của SubItem SelectLayout
            _layoutComboBox.Bounds = rect;

            // Chọn Layout hiện tại
            if (!string.IsNullOrEmpty(tag.SelectedLayout))
            {
                int index = _layoutComboBox.Items.IndexOf(tag.SelectedLayout);

                if (index >= 0)
                {
                    _layoutComboBox.SelectedIndex = index;
                }
            }
            else
            {
                _layoutComboBox.SelectedIndex = 0;
            }

            _layoutComboBox.Tag = item;

            _layoutComboBox.Visible = true;
            _layoutComboBox.BringToFront();
            _layoutComboBox.Focus();
        }
    }

    public static class DwgLayoutReader
    {
        public static List<string> GetLayouts(string dwgPath)
        {
            List<string> layouts = new List<string>();

            try
            {
                using (DwgReader reader = new DwgReader(dwgPath))
                {
                    CadDocument document = reader.Read();

                    foreach (var layout in document.Layouts)
                    {
                        layouts.Add(layout.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi đọc Layout:\n\n" +
                    $"File: {dwgPath}\n\n" +
                    $"Message: {ex.Message}\n\n" +
                    $"Type: {ex.GetType().FullName}\n\n" +
                    $"StackTrace:\n{ex.StackTrace}",
                    "ACadSharp Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            return layouts;
        }
    }

    sealed class Heartbeat : IDisposable
    {
        readonly CancellationTokenSource _cts = new CancellationTokenSource();
        readonly Thread _thread;

        public Heartbeat(Action ping, int maxSec, int periodMs)
        {
            CancellationToken token = _cts.Token;
            _thread = new Thread(() =>
            {
                DateTime end = DateTime.UtcNow.AddSeconds(maxSec);
                while (!token.IsCancellationRequested && DateTime.UtcNow < end)
                {
                    try { ping(); } catch { }
                    token.WaitHandle.WaitOne(periodMs);
                }
            });
            _thread.IsBackground = true;
            _thread.Start();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _thread.Join(2000);   // đảm bảo không còn ping nào sau khi UI Stop timer
        }
    }

    class FileInfo
    {
        public FileInfo()
        {
            _index = -1;
            _processed = false;
            _logFile = "";
        }
        public string _fileName;
        public string _timeDate;
        public string _logFile;
        public bool _status;
        public int _index;
        public bool _processed;
        public string _selectedLayout;

        public string _diagLog = "";
    }

    class DrawingItem
    {
        public FileInfo File { get; set; }

        public List<string> Layouts { get; set; }

        public string SelectedLayout { get; set; }
    }

    class ThreadInput
    {

        public object acadObject;
        public List<FileInfo> _FileInfolist;
        public string scriptFile;
        public string startUpScript;
        public int _restartDWGCount;
        public AutoResetEvent ThreadEvent;
        public string logLocation;
        public int nCreateImage;
        public bool bDiagnosticMode;
        public string commnadLineExePath = "";
        public int timeout;

        //holds  
        //1 - when key words are used
        //2 - when nesting script
        //0 - for no keyword or nested...
        public int nestedScript;

        public ThreadInput()
        {
            _FileInfolist = new List<FileInfo>();
            ThreadEvent = null;

            nCreateImage = 2;
            bDiagnosticMode = true;
            nestedScript = 0;
        }
    }

    class TagData
    {
        public TagData()
        {
            _status = true;
            _layouts = new List<string>();
        }

        string _dwgName;
        bool _status;

        // Danh sách Layout của DWG
        List<string> _layouts;

        // Layout mà người dùng đang chọn
        string _selectedLayout;

        public string DwgName
        {
            set { _dwgName = value; }
            get { return _dwgName; }
        }

        public bool status
        {
            set { _status = value; }
            get { return _status; }
        }

        public List<string> Layouts
        {
            set { _layouts = value; }
            get { return _layouts; }
        }

        public string SelectedLayout
        {
            set { _selectedLayout = value; }
            get { return _selectedLayout; }
        }
    }

    public class ReportLog
    {
        const string logExt = "log";
        private string _logFile;
        private string _logDetailFile;

        public ReportLog()
        {
        }

        public ReportLog(string pathName, string projectName)
        {
            string day = DateTime.Now.Day.ToString();
            string hour = DateTime.Now.Hour.ToString();
            string min = DateTime.Now.Minute.ToString();
            string sec = DateTime.Now.Second.ToString();

            string name = "SPlog";

            if (projectName.Length != 0)
                name = Path.GetFileNameWithoutExtension(projectName);

            _logFile =
              pathName + "\\" + name + "_" + day + "_" + hour + "_" +
              min + "_" + sec + "." + logExt;

            StreamWriter sw =
              new StreamWriter(_logFile, true);
            sw.Flush();
            sw.Close();

            _logDetailFile =
             pathName + "\\" + name + "_Detail_" + day + "_" + hour + "_" +
             min + "_" + sec + "." + logExt;

            sw = new StreamWriter(_logDetailFile, true);

            sw.WriteLine("");
            sw.WriteLine("");
            sw.Flush();
            sw.Close();
        }

        public string getLogFileName()
        {
            return _logFile;
        }

        public string getDetailLogFileName()
        {
            return _logDetailFile;
        }

        public void setLogFileName(string fileName)
        {
            _logFile = fileName;
        }

        public void Log(string filename, string strAcadLog,
                string strProject, bool bResult)
        {
            try
            {
                string logMsg;

                if (bResult)
                    logMsg = "Done";
                else
                    logMsg = "Failed";

                StreamWriter sw = new StreamWriter(_logFile, true);
                sw.WriteLine(filename + "," + logMsg);
                sw.Flush();
                sw.Close();

                sw = new StreamWriter(_logDetailFile, true);
                sw.WriteLine("------------------------------------------------------------------------------");
                sw.WriteLine("------------------------------------------------------------------------------");

                sw.WriteLine("Project file: 	" + strProject);
                sw.WriteLine("Drawing file: 	" + filename);
                sw.WriteLine("");
                sw.WriteLine("Processed by Computer: 	"
                    + System.Environment.MachineName);
                sw.WriteLine("");
                sw.WriteLine("User name   : 	"
                    + System.Environment.UserName);
                sw.WriteLine("");

                sw.WriteLine("[ Status summary ]");
                sw.WriteLine(logMsg);
                sw.WriteLine("");//empty line
                sw.WriteLine("");

                sw.WriteLine(strAcadLog);
                sw.Flush();
                sw.Close();
            }
            catch { }
        }
    }
}