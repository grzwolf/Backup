using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;            // culture info
using System.Runtime.InteropServices;  // dll
using Microsoft.Win32;                 // sleep & wakeup
using Microsoft.Win32.SafeHandles;
using System.Threading;

namespace Backup
{
    public partial class MainWindow : Form {

        // app power status
        enum AppStat {
            Undefined,
            ManualStart,
            PowerModeResume
        }

        AppStat _appStat = AppStat.Undefined;
        string _backupFolder = "";
        string _lastPath = "";
        bool _run = false;
        string _lastBackup = "1970.01.01_00:00:00";
        SelectFolderOrFile _sff = new SelectFolderOrFile();
        WakeUP _wup = new WakeUP();
        static string _logFileName = Application.ExecutablePath + ".log";
        static bool _bLogfile = false;

        public MainWindow() {
            InitializeComponent();

            this.Location = new Point(100, 100);

            // list of monitored but disappeared items
            List<string> disappearedItems = new List<string>();

            // INI: prepare read from ini
            IniFile ini = new IniFile(System.Windows.Forms.Application.ExecutablePath + ".ini");
            // INI: backup folder
            _backupFolder = ini.IniReadValue("General", "BackupFolder", "D:\\BackUp");
            // INI: include list
            string tmp = "";
            tmp = ini.IniReadValue("Include", "FolderCount", "0");
            int includeFolderCount = int.Parse(tmp);
            for ( int i = 0; i < includeFolderCount; i++ ) {
                tmp = ini.IniReadValue("Include", "Folder" + i.ToString(), "");
                if ( tmp.Length > 0 ) {
                    if ( System.IO.File.Exists(tmp) ) {
                        this.listViewInclude.Items.Add(tmp);
                        this.listViewInclude.Items[this.listViewInclude.Items.Count - 1].Checked = true;
                    } else {
                        if ( System.IO.Directory.Exists(tmp) ) {
                            this.listViewInclude.Items.Add(tmp);
                            this.listViewInclude.Items[this.listViewInclude.Items.Count - 1].Checked = true;
                        } else {
                            // item was part of the ini file but has disappeared afterwards
                            disappearedItems.Add(tmp);
                            continue;
                        }
                    }
                }
            }
            // disappeared monitored items
            if ( disappearedItems.Count > 0 ) {
                DialogResult dlgr = MessageBox.Show("Previously selected monitored items do not exist anymore.\n\nDo you want to see a list of the disappeared items?", "Attention", MessageBoxButtons.YesNo);
                if ( dlgr == System.Windows.Forms.DialogResult.Yes ) {
                    string msg = "";
                    foreach ( string s in disappearedItems ) {
                        msg += s + "\n";
                    }
                    Clipboard.SetText(msg);
                    MessageBox.Show(msg, "List of disappeared items - copied to ClipBoard");
                }
            }
            // INI: exclude list
            tmp = ini.IniReadValue("Exclude", "FolderCount", "0");
            int excludeFolderCount = int.Parse(tmp);
            for ( int i = 0; i < excludeFolderCount; i++ ) {
                tmp = ini.IniReadValue("Exclude", "Folder" + i.ToString(), "");
                if ( tmp.Length > 0 ) {
                    this.listViewExclude.Items.Add(tmp);
                    this.listViewExclude.Items[this.listViewExclude.Items.Count - 1].Checked = true;
                }
            }
            // INI: info about last backup time
            _lastBackup = ini.IniReadValue("General", "LastBackup", "1970.01.01_00:00:00");
            // INI: read auto execute status
            tmp = ini.IniReadValue("General", "ExecuteAtStart", "false");
            this.checkBoxAutoExecute.Checked = bool.Parse(tmp);
            // INI: time of next backup
            this.dateTimePicker.Value = DateTime.Now;
            string[] arr = ini.IniReadValue("General", "ExecBackupTime", "02:00").Split(':');
            if ( arr.Length >= 2 ) {
                this.dateTimePicker.Value = new DateTime(this.dateTimePicker.Value.Year, this.dateTimePicker.Value.Month, this.dateTimePicker.Value.Day, int.Parse(arr[0]), int.Parse(arr[1]), 00);
            }
            // INI: backup at time
            tmp = ini.IniReadValue("General", "ExecBackup", "False");
            this.checkBoxAtTime.Checked = bool.Parse(tmp);
            // INI: log start
            tmp = ini.IniReadValue("General", "writeLogfile", "True");
            _bLogfile = bool.Parse(tmp);
            logTextLn(DateTime.Now, "---Start---");

            // register sleep / wakeup event
            SystemEvents.PowerModeChanged += OnPowerChange;

            // show autostart mode
            this.checkBoxRegisterAutostart.Checked = GetStartupActive();

            // add "about entry"
            SetupSystemMenu();

            // auto execute 1x at app start
            if ( this.checkBoxAutoExecute.Checked ) {
                // exec backup in copy mode
                logTextLn(DateTime.Now, "executing backup at app start");
                string message = backupFiles(true);
                this.Text = "BackUp Files and Folders - " + message;
                logTextLn(DateTime.Now, message);
                logTextLn(DateTime.Now, "backup at app start done");
            }

            // will be called, as soon as MainForm is shown
            this.Shown += MainForm_Shown;
        }

        // on first show
        private void MainForm_Shown(object sender, EventArgs e) {
            _appStat = AppStat.ManualStart;
        }

        // sleep & wakeup
        private void OnPowerChange(object s, PowerModeChangedEventArgs e) {
            //            MessageBox.Show(e.ToString());
            switch ( e.Mode ) {
                case PowerModes.Resume:
                    if ( this.checkBoxAtTime.Checked ) {
                        _appStat = AppStat.PowerModeResume;
                    }
                    break;
                case PowerModes.Suspend:
                    break;
            }
        }

        // manually exec backup
        private void buttonExecute_Click(object sender, EventArgs e) {
            // allow interrupt/break backup 
            if ( !this.buttonExecute.Text.StartsWith("Execute") ) {
                _run = false;
                return;
            }
            this.buttonExecute.Text = "- break -";

            // prepare
            Cursor.Current = Cursors.WaitCursor;
            ModifyProgressBarColor.SetState(this.progressBar, 2);

            // exec backup in copy mode
            logTextLn(DateTime.Now, "exec backup manually");
            string message = backupFiles(true);
            this.Text = "BackUp Files and Folders - " + message;

            // finish
            this.buttonExecute.Text = "Execute Backup";
            Cursor.Current = Cursors.Default;

            // show result
            int state = 2;
            if ( message.StartsWith("Success") ) {
                state = 1;
            }
            ModifyProgressBarColor.SetState(this.progressBar, state);
            this.Text = "BackUp Files and Folders - " + message;
            logTextLn(DateTime.Now, this.Text);

            MessageBox.Show("Backup done");
        }

        // central function: could be started in "copy mode" copyMode=true OR "test mode" copyMode=false
        string backupFiles(bool copyMode) {
            // exec backup in copy mode
            this.buttonIncludeAdd.Enabled = false;
            this.buttonIncludeRemove.Enabled = false;
            this.buttonExcludeAdd.Enabled = false;
            this.buttonExcludeRemove.Enabled = false;
            _run = true;
            string message = backupFilesCore(copyMode);
            _run = false;
            this.buttonIncludeAdd.Enabled = true;
            this.buttonIncludeRemove.Enabled = true;
            this.buttonExcludeAdd.Enabled = true;
            this.buttonExcludeRemove.Enabled = true;
            return message;
        }
        string backupFilesCore(bool copyMode) {
            // return if sff is visible to prevent messing up the lists
            if ( _sff.Visible ) {
                return "skipped";
            }
            // loop include list and build lSource from it, which will contain all files&folders to backup
            this.progressBar.Value = 0;
            List<string> lSource = new List<string>();
            foreach ( ListViewItem lvi in this.listViewInclude.Items ) {
                Application.DoEvents();
                // skip not checked items
                if ( !lvi.Checked ) {
                    continue;
                }
                // get source files
                string src = lvi.Text;
                if ( System.IO.File.Exists(src) ) {
                    // single file
                    ListViewItem lvitem = this.listViewExclude.FindItemWithText(src);
                    if ( lvitem == null ) {
                        lSource.Add(src);
                    } else {
                        if ( !lvitem.Checked ) {
                            lSource.Add(src);
                        }
                    }
                } else {
                    // get all files and folders belonging to the list entry, if entry == directory
                    List<String> lst = new List<string>();
                    string path = src;
                    if ( path.Length == 2 ) {
                        path += "\\";
                    }
                    GrzTools.FastFileFind.FindFiles(ref lst, path, "*", true, ref _run);
                    for ( int i = lst.Count - 1; i >= 0; i-- ) {
                        Application.DoEvents();
                        if ( !_run ) {
                            return "break";
                        }
                        // loop exclude list
                        foreach ( ListViewItem lve in this.listViewExclude.Items ) {
                            Application.DoEvents();
                            if ( !_run ) {
                                return "break";
                            }
                            // skip not checked items
                            if ( !lve.Checked ) {
                                continue;
                            }
                            // sanity checks 
                            if ( lst.Count == 0 ) {
                                break;
                            }
                            if ( i < 0 ) {
                                break;
                            }
                            if ( i > lst.Count - 1 ) {
                                break;
                            }
                            // exclude item 
                            if ( lst[i].StartsWith(lve.Text) ) {
                                lst.RemoveAt(i);
                                break;
                            }
                        }
                    }
                    // make source list entries
                    if ( lst.Count == 0 ) {
                        lSource.Add(src);
                    } else {
                        lSource.AddRange(lst);
                    }
                    if ( lst.Count == 0 ) {
                        return "no folders to backup";
                    }
                }
            }
            if ( lSource.Count == 0 ) {
                return "nothing to backup";
            }

            // test integrity VERSUS copy
            this.progressBar.Value = 0;
            this.progressBar.Maximum = lSource.Count();
            if ( this.progressBar.Maximum < 4 ) {
                this.progressBar.Maximum = lSource.Count() * 4;
                this.progressBar.Step = 4;
            }
            string msg = "Success:";
            int copyCount = 0;
            int skipCount = 0;
            int errCount = 0;
            int needBackup = 0;
            if ( copyMode ) {
                System.IO.Directory.CreateDirectory(_backupFolder);
            }
            foreach ( string src in lSource ) {
                if ( !_run ) {
                    return "break";
                }
                try {
                    // build destination filename
                    string destination = src;
                    destination = destination.Replace(":\\", "_");
                    destination = System.IO.Path.Combine(_backupFolder, destination);
                    // we only copy files
                    if ( System.IO.File.Exists(src) ) {
                        // create directory for destination file
                        string newDir = System.IO.Path.GetDirectoryName(destination);
                        if ( copyMode ) {
                            System.IO.Directory.CreateDirectory(newDir);
                        }
                        // check whether copy is needed
                        bool copy = false;
                        if ( getFileStatus(src) == getFileStatus(destination) ) {
                            skipCount++;
                        } else {
                            copy = true;
                        }
                        // finally copy the file
                        if ( copy ) {
                            if ( copyMode ) {
                                // real file copy f&f
                                System.Threading.Tasks.Task.Factory.StartNew(() => {
                                    System.IO.File.Copy(src, destination, true);
                                    copyCount++;
                                });
                                // real file copy 
//                                System.IO.File.Copy(src, destination, true);
//                                copyCount++;
                            } else {
                                // inc backup needed counter
                                needBackup++;
                            }
                        }
                    } else {
                        // create empty directory
                        if ( copyMode ) {
                            System.IO.Directory.CreateDirectory(destination);
                        }
                        skipCount++;
                    }
                    // progress
                    this.progressBar.PerformStep();
                    Application.DoEvents();
                } catch {
                    msg = "FAIL: ";
                    errCount++;
                }
            }

            // grant a cooperative 2s wait for f&f file copy
            if ( copyMode ) {
                CountdownEvent countdown = new CountdownEvent(2000);
                countdown.Wait(2000);
                do {
                    Application.DoEvents();
                    countdown.Signal();
                } while ( countdown.CurrentCount > 0 );
            }

            // finish output message
            string message = String.Format("{0} {1}x copy / {2}x skip / {3}x error / {4} total count", msg, copyCount, skipCount, errCount, lSource.Count());
            if ( copyMode ) {
                if ( copyCount > 0 ) {
                    IniFile ini = new IniFile(System.Windows.Forms.Application.ExecutablePath + ".ini");
                    _lastBackup = DateTime.Now.ToString("yyyy.MM.dd_HH:mm:ss");
                    ini.IniWriteValue("General", "LastBackup", _lastBackup);
                }
            } else {
                if ( needBackup == 0 ) {
                    message = String.Format("backup from {0} is up to date", _lastBackup);
                } else {
                    message = String.Format("backup is needed for {0} out of {1} items", needBackup, lSource.Count());
                }
            }

            return message;
        }

        // file status is based on "file size" + "time last write" (reduced to minutes - !ext. drive is sometimes a few seconds off!)
        string getFileStatus(string file) {
            string strStatus = "";
            try {
                System.IO.FileInfo fi = new System.IO.FileInfo(file);
                strStatus = fi.Length.ToString() + fi.LastWriteTime.ToString("yyyyMMddHHmm");
            } catch {
                strStatus = "error";
            }
            return strStatus;
        }

        // add files&folders to backup
        private void buttonIncludeAdd_Click(object sender, EventArgs e) {
            _sff = new SelectFolderOrFile();
            _sff.Location = new Point(this.Location.X + this.Size.Width + 5, this.Location.Y);
            _sff.Text = "Select Folder or File";
            _sff.DefaultPath = _lastPath;
            DialogResult dlr = _sff.ShowDialog(this);
            if ( dlr != System.Windows.Forms.DialogResult.OK ) {
                return;
            }
            if ( _sff.ReturnFiles.Count > 0 ) {
                _lastPath = System.IO.Path.GetDirectoryName(_sff.ReturnFiles[0]);
                bool needRefresh = false;
                foreach ( string item in _sff.ReturnFiles ) {
                    if ( this.listViewInclude.FindItemWithText(item) == null ) {
                        // add item to include list
                        this.listViewInclude.Items.Add(item);
                        this.listViewInclude.Items[this.listViewInclude.Items.Count - 1].Checked = true;
                        needRefresh = true;
                    }
                }
                if ( needRefresh ) {
                    checkBackupStatus();
                }
            }
        }
        // remove files&folders to backup
        private void buttonIncludeRemove_Click(object sender, EventArgs e) {
            foreach ( ListViewItem lvi in this.listViewInclude.SelectedItems ) {
                this.listViewInclude.Items.Remove(lvi);
            }
        }

        // add files&folders to exclude from backup
        private void buttonExcludeAdd_Click(object sender, EventArgs e) {
            _sff = new SelectFolderOrFile();
            _sff.Location = new Point(this.Location.X + this.Size.Width + 5, this.Location.Y);
            _sff.Text = "Select Folder or File";
            _sff.DefaultPath = _lastPath;
            DialogResult dlr = _sff.ShowDialog(this);
            if ( dlr != System.Windows.Forms.DialogResult.OK ) {
                return;
            }
            if ( _sff.ReturnFiles.Count > 0 ) {
                _lastPath = System.IO.Path.GetDirectoryName(_sff.ReturnFiles[0]);
                foreach ( string item in _sff.ReturnFiles ) {
                    if ( this.listViewExclude.FindItemWithText(item) == null ) {
                        this.listViewExclude.Items.Add(item);
                        this.listViewExclude.Items[this.listViewExclude.Items.Count - 1].Checked = true;
                    }
                }
            }
        }
        // remove files&folders to exclude from backup
        private void buttonExcludeRemove_Click(object sender, EventArgs e) {
            foreach ( ListViewItem lvi in this.listViewExclude.SelectedItems ) {
                this.listViewExclude.Items.Remove(lvi);
            }
        }

        // main form closing
        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e) {
            logTextLn(DateTime.Now, "--- saving INI ---");
            // in case something is still active
            _run = false;
            // INI: prepare write ini
            IniFile ini = new IniFile(System.Windows.Forms.Application.ExecutablePath + ".ini");
            // INI: write backup folder
            ini.IniWriteValue("General", "BackupFolder", _backupFolder);
            // INI: delete include folder items
            string tmp = "";
            tmp = ini.IniReadValue("Include", "FolderCount", "0");
            int includeFolderCount = int.Parse(tmp);
            for ( int i = 0; i < includeFolderCount; i++ ) {
                ini.IniWriteValue("Include", "Folder" + i.ToString(), null);
            }
            // INI: write include list count
            includeFolderCount = this.listViewInclude.Items.Count;
            // INI: write include list
            ini.IniWriteValue("Include", "FolderCount", includeFolderCount.ToString());
            for ( int i = 0; i < includeFolderCount; i++ ) {
                ini.IniWriteValue("Include", "Folder" + i.ToString(), this.listViewInclude.Items[i].Text);
            }
            // INI: delete exclude folder items
            tmp = ini.IniReadValue("Exclude", "FolderCount", "0");
            int excludeFolderCount = int.Parse(tmp);
            for ( int i = 0; i < excludeFolderCount; i++ ) {
                ini.IniWriteValue("Exclude", "Folder" + i.ToString(), null);
            }
            // INI: write exclude list count
            excludeFolderCount = this.listViewExclude.Items.Count;
            // INI: write exclude list
            ini.IniWriteValue("Exclude", "FolderCount", excludeFolderCount.ToString());
            for ( int i = 0; i < excludeFolderCount; i++ ) {
                ini.IniWriteValue("Exclude", "Folder" + i.ToString(), this.listViewExclude.Items[i].Text);
            }
            // INI: backup at time
            ini.IniWriteValue("General", "ExecBackupTime", this.dateTimePicker.Value.ToString("HH:mm"));
            // INI: backup at time
            ini.IniWriteValue("General", "ExecBackup", this.checkBoxAtTime.Checked.ToString());
            // log
            logTextLn(DateTime.Now, "--- End ---");
        }

        // status update
        private void checkBackupStatus() {
            // progressbar color is initially red while checking
            ModifyProgressBarColor.SetState(this.progressBar, 2);
            // exec backup in test mode
            string message = backupFiles(false);
            // progressbar color: 1=green, 2=red, 3=yellow
            int state = 2;
            if ( message.StartsWith("backup is needed") ) {
                state = 3;
            }
            if ( message.EndsWith("is up to date") ) {
                state = 1;
            }
            // update title bar text
            this.Text = "BackUp Files and Folders - " + message;
            logTextLn(DateTime.Now, this.Text);
            ModifyProgressBarColor.SetState(this.progressBar, state);
        }

        // manage autostart mode
        private void checkBoxRegisterAutostart_CheckedChanged(object sender, EventArgs e) {
            SetStartup(this.checkBoxRegisterAutostart.Checked);
        }
        private void SetStartup(bool active) {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if ( active ) {
                rk.SetValue("GRE_Backup", Application.ExecutablePath.ToString());
            } else {
                rk.DeleteValue("GRE_Backup", false);
            }
        }
        private bool GetStartupActive() {
            bool active = false;
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string app = (string)rk.GetValue("GRE_Backup");
            if ( app == Application.ExecutablePath ) {
                active = true;
            }
            return active;
        }

        // manage execute 1x at app start
        private void checkBoxAutoExecute_CheckedChanged(object sender, EventArgs e) {
            // INI: prepare write ini
            IniFile ini = new IniFile(System.Windows.Forms.Application.ExecutablePath + ".ini");
            // INI: write auto execute status
            ini.IniWriteValue("General", "ExecuteAtStart", this.checkBoxAutoExecute.Checked.ToString());
        }

        // reset UI
        private void buttonReset_Click(object sender, EventArgs e) {
            _run = false;
            this.Text = "BackUp Files and Folders";
            this.progressBar.Value = 0;
            this.progressBar.Maximum = 0; 
        }

        // exec manual check
        private void buttonCheckBackup_Click(object sender, EventArgs e) {
            checkBackupStatus();
        }

        // show about in system menu
        [DllImport("user32.dll")]
        private static extern int GetSystemMenu(int hwnd, int bRevert);
        [DllImport("user32.dll")]
        private static extern int AppendMenu(int hMenu, int Flagsw, int IDNewItem, string lpNewItem);
        private void SetupSystemMenu() {
            // get handle to system menu
            int menu = GetSystemMenu(this.Handle.ToInt32(), 0);
            // add a separator
            AppendMenu(menu, 0xA00, 0, null);
            // add an item with a unique ID
            AppendMenu(menu, 0, 12345, "About BackUp");
        }
        protected override void WndProc(ref System.Windows.Forms.Message m) {
            // show About box: WM_SYSCOMMAND is 0x112
            if ( m.Msg == 0x112 ) {
                // check for added menu item ID
                if ( m.WParam.ToInt32() == 12345 ) {
                    // show About box here...
                    AboutBox dlg = new AboutBox();
                    dlg.ShowDialog();
                    dlg.Dispose();
                }
            }
            // it is essentiell to call the base behaviour
            base.WndProc(ref m);
        }

        // auto exec backup at a given time
        private void dateTimePicker_ValueChanged(object sender, EventArgs e) {
            checkBoxAtTime_CheckedChanged(null, null);
        }
        private void checkBoxAtTime_CheckedChanged(object sender, EventArgs e) {
            if ( this.checkBoxAtTime.Checked ) {
                if ( (this.dateTimePicker.Value.Date == DateTime.Now.Date) && (this.dateTimePicker.Value.Hour > DateTime.Now.Hour) ) {
                    this.dateTimePicker.Value.AddDays(1);
                }
                _wup = new WakeUP();
                _wup.Woken += WakeUP_Woken;
                _wup.SetWakeUpTime(this.dateTimePicker.Value);
                logTextLn(DateTime.Now, "setting up wakeup event: " + this.dateTimePicker.Value.ToString());
                logTextLn(DateTime.Now, "wakeup event enabled");
            } else {
                _wup.Woken -= WakeUP_Woken;
                logTextLn(DateTime.Now, "wakeup event disabled");
            }
        }
        private void WakeUP_Woken(object sender, EventArgs e) {
            if ( this.checkBoxAtTime.Checked ) {
                logTextLn(DateTime.Now, "wakeup event with 'Backup At Time' enabled detected");
                if ( this.dateTimePicker.Value.ToString("HH:mm") == DateTime.Now.ToString("HH:mm") ) {
                    logTextLn(DateTime.Now, "exec backup after wakeup event");
                    string message = backupFiles(true);
                    this.Text = "BackUp Files and Folders - " + message;
                    this.dateTimePicker.Value.AddDays(1);
                    if ( _appStat == AppStat.PowerModeResume ) {
                        logTextLn(DateTime.Now, "PC back to sleep after daily backup executed");
                        Application.SetSuspendState(PowerState.Suspend, false, false);
                    } else {
                        logTextLn(DateTime.Now, "daily backup executed while PC was on");
                    }
                } else {
                    logTextLn(DateTime.Now, "wakeup event with 'Backup At Time' detected, but time doesn't match");
                }
            } else {
                logTextLn(DateTime.Now, "wakeup event with 'Backup At Time' disabled detected");
            }
        }

        // logger methods
        public static void logTextLn(DateTime now, string logtxt, bool logToFile = true) {
            logtxt = now.ToString("dd.MM.yyyy HH:mm:ss_fff ", CultureInfo.InvariantCulture) + logtxt;
            logText(logtxt + "\r\n", logToFile);
        }
        static void logText(string logtxt, bool logToFile = true) {
            if ( logToFile ) {
                logTextToFile(logtxt);
            }
        }
        static void logTextToFile(string logtxt) {
            if ( !_bLogfile ) {
                return;
            }
            try {
                System.IO.StreamWriter lsw = System.IO.File.AppendText(_logFileName);
                lsw.Write(logtxt);
                lsw.Close();
            } catch {; }
        }

        // select folder to backup dialog
        private void buttonFolders_Click(object sender, EventArgs e) {
            // show 'Select Folder Dialog'
            _sff = new SelectFolderOrFile();
            _sff.Location = new Point(this.Location.X + this.Size.Width + 5, this.Location.Y);
            _sff.Text = "Select Folder where the Backup shall be stored";
            _sff.DefaultPath = _backupFolder;
            DialogResult dlr = _sff.ShowDialog(this);
            if ( dlr != System.Windows.Forms.DialogResult.OK ) {
                return;
            }
            if ( _sff.ReturnFiles.Count > 0 ) {
                if ( _backupFolder != _sff.ReturnPath ) {
                    _backupFolder = _sff.ReturnPath;
                    // INI: prepare write to ini
                    IniFile ini = new IniFile(System.Windows.Forms.Application.ExecutablePath + ".ini");
                    // INI: write out backup folder
                    ini.IniWriteValue("General", "BackupFolder", _backupFolder);
                }
            }
        }

    }

    // allows to change the progressbar color: https://stackoverflow.com/questions/778678/how-to-change-the-color-of-progressbar-in-c-sharp-net-3-5
    public static class ModifyProgressBarColor
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage( IntPtr hWnd, uint Msg, IntPtr w, IntPtr l );
        public static void SetState( ProgressBar pBar, int state )
        {
            try {
                SendMessage(pBar.Handle, 1040, (IntPtr)state, IntPtr.Zero);
            } catch { ;}
        }
    }

    // imho the easiest (but outdated) way to manage setup info by ini file
    public class IniFile
    {
        public string path;
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        public IniFile(string INIPath)
        {
            path = INIPath;
        }
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }
        public string IniReadValue(string Section, string Key, string DefaultValue)
        {
            StringBuilder retVal = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, DefaultValue, retVal, 255, this.path);
            return retVal.ToString();
        }
    }

    // https://www.codeproject.com/articles/49798/wake-the-pc-from-standby-or-hibernation
    class WakeUP
    {
        [DllImport("kernel32.dll")]
        public static extern SafeWaitHandle CreateWaitableTimer(IntPtr lpTimerAttributes,
                                                                  bool bManualReset,
                                                                string lpTimerName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWaitableTimer(SafeWaitHandle hTimer,
                                                    [In] ref long pDueTime,
                                                              int lPeriod,
                                                           IntPtr pfnCompletionRoutine,
                                                           IntPtr lpArgToCompletionRoutine,
                                                             bool fResume);

        public event EventHandler Woken;

        private BackgroundWorker bgWorker = new BackgroundWorker();

        public WakeUP()
        {
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
            bgWorker.RunWorkerCompleted +=
              new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
        }

        public void SetWakeUpTime(DateTime time)
        {
            bgWorker.RunWorkerAsync(time.ToFileTime());
        }

        void bgWorker_RunWorkerCompleted(object sender,
                      RunWorkerCompletedEventArgs e)
        {
            if ( Woken != null ) {
                Woken(this, new EventArgs());
            }
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            long waketime = (long)e.Argument;

            using ( SafeWaitHandle handle =
                      CreateWaitableTimer(IntPtr.Zero, true,
                      this.GetType().Assembly.GetName().Name.ToString() + "Timer") ) {
                if ( SetWaitableTimer(handle, ref waketime, 0,
                                       IntPtr.Zero, IntPtr.Zero, true) ) {
                    using ( EventWaitHandle wh = new EventWaitHandle(false,
                                                           EventResetMode.AutoReset) ) {
                        wh.SafeWaitHandle = handle;
                        wh.WaitOne();
                    }
                } else {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

    }
}