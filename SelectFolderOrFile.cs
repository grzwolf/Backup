using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;                           // Path
using System.Threading;                    // sleep   

namespace Backup
{
    public partial class SelectFolderOrFile: Form
    {
        string m_DefaultPath = "";

        public string ReturnPath { get; set; }
        public List<string> ReturnFiles = new List<string>();

        public string DefaultPath
        {
            set
            {
                m_DefaultPath = value;
                this.textBox1.Text = value;
                this.fileSystemTreeView1.DefaultPath = value;
            }
        }

        public SelectFolderOrFile()
        {
            InitializeComponent();
            ReturnPath = "";
            ReturnFiles.Clear(); 

            this.fileSystemTreeView1.WantClose += new EventHandler<FileSystemTreeView.WantCloseEventArgs>(fileSystemTreeView1_WantClose);
            this.fileSystemTreeView1.SelectionChanged += new EventHandler<FileSystemTreeView.SelectionChangedEventArgs>(fileSystemTreeView1_SelectionChanged);
            this.fileSystemTreeView1.NetworkScanProgress += new EventHandler<FileSystemTreeView.NetworkScanEventArgs>(fileSystemTreeView1_NetworkScanProgress);

            this.progressBar1.Step = 1;
            this.progressBar1.Value = 0;
        }

        public bool AutoNetworkScan
        {
            get
            {
                return fileSystemTreeView1.AutoNetworkScan;
            }
            set
            {
                fileSystemTreeView1.AutoNetworkScan = value;
            }
        }

        // force refresh by parent (after media change) via call thru public method: delayed execution via timer was needed due to strange exceptions
        public void RefreshRequest(string msg)
        {
            this.timerDelayedRefresh.Start();
        }
        private void timerDelayedRefresh_Tick( object sender, EventArgs e )
        {
            this.timerDelayedRefresh.Stop();
            buttonRefresh_Click(null, null);
        }

        // network scanning (windows\winsxs too) may take a long time
        private void timerShowBreakButton_Tick( object sender, EventArgs e )
        {
            this.timerShowBreakButton.Stop();
            this.buttonBreak.Visible = true;
        }
        private void fileSystemTreeView1_NetworkScanProgress( object sender, FileSystemTreeView.NetworkScanEventArgs nsea )
        {
            if ( nsea.Current == 0 ) {
                this.progressBar1.Maximum = (int)nsea.Maximal;
                this.buttonNewFolder.Enabled = false;
                this.buttonRefresh.Enabled = false;
                this.buttonSelect.Enabled = false;
                this.timerShowBreakButton.Start();
                if ( (nsea.Current == 0) && (nsea.Maximal == 0) ) {
                    this.timerShowBreakButton.Stop();
                    this.buttonBreak.Visible = false;
                    this.buttonNewFolder.Enabled = true;
                    this.buttonRefresh.Enabled = true;
                    this.buttonSelect.Enabled = true;
                }
            } else {
                try {
                    this.progressBar1.Value = (int)nsea.Current;
                } catch ( Exception ) { ;}
            }
        }

        // fileSystemTreeView sent a message to select&close the dialog, happens when user right clicks an item 
        private void fileSystemTreeView1_WantClose( object sender, FileSystemTreeView.WantCloseEventArgs wcea )
        {
            if ( !this.buttonSelect.Enabled ) {
                return;
            }

            this.fileSystemTreeView1.Break = true;
            ReturnPath = this.fileSystemTreeView1.SelectedPath;
            if ( this.textBox1.Text.StartsWith("My Computer") ) {
                ReturnPath = "";
            }
            ReturnFiles = this.fileSystemTreeView1.SelectedFiles;
            if ( ReturnFiles.Count == 0 ) {
                ReturnFiles.Add(ReturnPath);
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // current selected path/file was chaged by fileSystemTreeView
        private void fileSystemTreeView1_SelectionChanged( object sender, FileSystemTreeView.SelectionChangedEventArgs scea )
        {
            this.textBox1.Text = scea.Selection;
            if ( scea.Selection.Length == 0 ) {
                this.textBox1.Text = m_DefaultPath;
            }
        }

        // select and close
        private void buttonSelect_Click( object sender, EventArgs e )
        {
            //ReturnPath = this.fileSystemTreeView1.SelectedPath;
            ReturnPath = this.textBox1.Text;
            ReturnFiles = this.fileSystemTreeView1.SelectedFiles;
            if ( ReturnFiles.Count == 0 ) {
                ReturnFiles.Add(ReturnPath);
            }

            if ( this.textBox1.Text.StartsWith("My Computer") ) {
                ReturnPath = m_DefaultPath;
            }
        }

        // create a new folder
        private void buttonNewFolder_Click( object sender, EventArgs e )
        {
            if ( this.textBox1.Text.StartsWith("My Computer") ) {
                return;
            }

            string newFolder = Path.Combine(this.textBox1.Text, "New Folder");
            int counter = 0;
            while ( Directory.Exists(newFolder) ) {
                newFolder = Path.Combine(this.textBox1.Text, "New Folder(" + counter++ + ")");
            }
            try {
                Directory.CreateDirectory(newFolder);
                string newNodeName = Path.GetFileName(newFolder);
                this.fileSystemTreeView1.Refresh(newNodeName);
            } catch (Exception) {
                ;
            }
        }

        // close dialog
        private void buttonCancel_Click( object sender, EventArgs e )
        {
            ReturnPath = "";
            ReturnFiles.Clear();
            this.fileSystemTreeView1.Break = true;
            Thread.Sleep(100);
        }
        protected override bool ProcessDialogKey( Keys keyData )
        {
            if ( Form.ModifierKeys == Keys.None && keyData == Keys.Enter) {
                buttonSelect_Click(null, null);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            if ( Form.ModifierKeys == Keys.None && keyData == Keys.Escape ) {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        // break current search processes
        private void buttonBreak_Click( object sender, EventArgs e )
        {
            this.fileSystemTreeView1.Break = true;
            Thread.Sleep(200); 
            this.buttonBreak.Visible = false;
        }

        // refresh treeview and listview
        private void buttonRefresh_Click( object sender, EventArgs e )
        {
            if ( sender == null ) {
                this.fileSystemTreeView1.Refresh("mediachanged");
            } else {
                this.fileSystemTreeView1.Refresh("");
            }
        }

        private void buttonMapFolder_Click( object sender, EventArgs e )
        {
            //string sNetDrive = "";
            //if ( this.textBox1.Text.StartsWith("\\\\") ) {
            //    sNetDrive = this.textBox1.Text;
            //}
            //NetworkMapping nmdlg = new NetworkMapping(sNetDrive);
            //DialogResult dlr = nmdlg.ShowDialog(this);
            //nmdlg.Dispose();
        }

        private void buttonNetwork_Click( object sender, EventArgs e )
        {
            //this.buttonNetwork.Text = "Network";
            //this.fileSystemTreeView1.DefaultIP = "";
            //IpEtc dlg = new IpEtc();
            //dlg.ShowDialog();
            //string ip = dlg.ReturnValueIpString;
            //if ( ip.Length > 0 ) {
            //    this.buttonNetwork.Text = ip;
            //    this.fileSystemTreeView1.DefaultIP = ip;
            //}
        }

    }
}
