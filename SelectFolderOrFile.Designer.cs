namespace Backup
{
    partial class SelectFolderOrFile
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if ( disposing && (components != null) ) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SelectFolderOrFile));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.fileSystemTreeView1 = new Backup.FileSystemTreeView();
            this.buttonSelect = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonMapFolder = new System.Windows.Forms.Button();
            this.buttonNewFolder = new System.Windows.Forms.Button();
            this.buttonRefresh = new System.Windows.Forms.Button();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonBreak = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonNetwork = new System.Windows.Forms.Button();
            this.timerShowBreakButton = new System.Windows.Forms.Timer(this.components);
            this.timerDelayedRefresh = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.fileSystemTreeView1, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.buttonSelect, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.textBox1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.progressBar1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.buttonRefresh, 3, 3);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel4, 4, 3);
            this.tableLayoutPanel1.Controls.Add(this.buttonNetwork, 1, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // fileSystemTreeView1
            // 
            this.fileSystemTreeView1.AutoNetworkScan = false;
            this.tableLayoutPanel1.SetColumnSpan(this.fileSystemTreeView1, 5);
            this.fileSystemTreeView1.Cursor = System.Windows.Forms.Cursors.Default;
            this.fileSystemTreeView1.DefaultIP = "";
            resources.ApplyResources(this.fileSystemTreeView1, "fileSystemTreeView1");
            this.fileSystemTreeView1.Name = "fileSystemTreeView1";
            // 
            // buttonSelect
            // 
            this.buttonSelect.DialogResult = System.Windows.Forms.DialogResult.OK;
            resources.ApplyResources(this.buttonSelect, "buttonSelect");
            this.buttonSelect.Name = "buttonSelect";
            this.buttonSelect.UseVisualStyleBackColor = true;
            this.buttonSelect.Click += new System.EventHandler(this.buttonSelect_Click);
            // 
            // textBox1
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.textBox1, 5);
            resources.ApplyResources(this.textBox1, "textBox1");
            this.textBox1.Name = "textBox1";
            // 
            // progressBar1
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.progressBar1, 5);
            resources.ApplyResources(this.progressBar1, "progressBar1");
            this.progressBar1.Name = "progressBar1";
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.buttonMapFolder, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.buttonNewFolder, 0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // buttonMapFolder
            // 
            resources.ApplyResources(this.buttonMapFolder, "buttonMapFolder");
            this.buttonMapFolder.Name = "buttonMapFolder";
            this.buttonMapFolder.UseVisualStyleBackColor = true;
            this.buttonMapFolder.Click += new System.EventHandler(this.buttonMapFolder_Click);
            // 
            // buttonNewFolder
            // 
            resources.ApplyResources(this.buttonNewFolder, "buttonNewFolder");
            this.buttonNewFolder.Name = "buttonNewFolder";
            this.buttonNewFolder.UseVisualStyleBackColor = true;
            this.buttonNewFolder.Click += new System.EventHandler(this.buttonNewFolder_Click);
            // 
            // buttonRefresh
            // 
            resources.ApplyResources(this.buttonRefresh, "buttonRefresh");
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.UseVisualStyleBackColor = true;
            this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
            // 
            // tableLayoutPanel4
            // 
            resources.ApplyResources(this.tableLayoutPanel4, "tableLayoutPanel4");
            this.tableLayoutPanel4.Controls.Add(this.buttonBreak, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.buttonCancel, 1, 0);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            // 
            // buttonBreak
            // 
            this.buttonBreak.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            resources.ApplyResources(this.buttonBreak, "buttonBreak");
            this.buttonBreak.Name = "buttonBreak";
            this.buttonBreak.UseVisualStyleBackColor = false;
            this.buttonBreak.Click += new System.EventHandler(this.buttonBreak_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonNetwork
            // 
            resources.ApplyResources(this.buttonNetwork, "buttonNetwork");
            this.buttonNetwork.Name = "buttonNetwork";
            this.buttonNetwork.UseVisualStyleBackColor = true;
            this.buttonNetwork.Click += new System.EventHandler(this.buttonNetwork_Click);
            // 
            // timerShowBreakButton
            // 
            this.timerShowBreakButton.Interval = 1000;
            this.timerShowBreakButton.Tick += new System.EventHandler(this.timerShowBreakButton_Tick);
            // 
            // timerDelayedRefresh
            // 
            this.timerDelayedRefresh.Tick += new System.EventHandler(this.timerDelayedRefresh_Tick);
            // 
            // SelectFolderOrFile
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelectFolderOrFile";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private FileSystemTreeView fileSystemTreeView1;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonSelect;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button buttonNewFolder;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button buttonBreak;
        private System.Windows.Forms.Timer timerShowBreakButton;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button buttonRefresh;
        private System.Windows.Forms.Timer timerDelayedRefresh;
        private System.Windows.Forms.Button buttonMapFolder;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.Button buttonNetwork;
    }
}