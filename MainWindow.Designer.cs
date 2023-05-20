namespace Backup
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.buttonIncludeRemove = new System.Windows.Forms.Button();
            this.buttonIncludeAdd = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.buttonExcludeRemove = new System.Windows.Forms.Button();
            this.buttonExcludeAdd = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.buttonFolders = new System.Windows.Forms.Button();
            this.dateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.checkBoxAtTime = new System.Windows.Forms.CheckBox();
            this.buttonCheckBackup = new System.Windows.Forms.Button();
            this.buttonReset = new System.Windows.Forms.Button();
            this.checkBoxRegisterAutostart = new System.Windows.Forms.CheckBox();
            this.checkBoxAutoExecute = new System.Windows.Forms.CheckBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.buttonExecute = new System.Windows.Forms.Button();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.listViewInclude = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.listViewExclude = new System.Windows.Forms.ListView();
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 65F));
            this.tableLayoutPanel1.Controls.Add(this.panel1, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel2, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.panel3, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(632, 449);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.buttonIncludeRemove);
            this.panel1.Controls.Add(this.buttonIncludeAdd);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(567, 80);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(65, 184);
            this.panel1.TabIndex = 2;
            // 
            // buttonIncludeRemove
            // 
            this.buttonIncludeRemove.Location = new System.Drawing.Point(3, 64);
            this.buttonIncludeRemove.Name = "buttonIncludeRemove";
            this.buttonIncludeRemove.Size = new System.Drawing.Size(59, 23);
            this.buttonIncludeRemove.TabIndex = 1;
            this.buttonIncludeRemove.Text = "Remove";
            this.buttonIncludeRemove.UseVisualStyleBackColor = true;
            this.buttonIncludeRemove.Click += new System.EventHandler(this.buttonIncludeRemove_Click);
            // 
            // buttonIncludeAdd
            // 
            this.buttonIncludeAdd.Location = new System.Drawing.Point(3, 23);
            this.buttonIncludeAdd.Name = "buttonIncludeAdd";
            this.buttonIncludeAdd.Size = new System.Drawing.Size(59, 23);
            this.buttonIncludeAdd.TabIndex = 0;
            this.buttonIncludeAdd.Text = "Add";
            this.buttonIncludeAdd.UseVisualStyleBackColor = true;
            this.buttonIncludeAdd.Click += new System.EventHandler(this.buttonIncludeAdd_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.buttonExcludeRemove);
            this.panel2.Controls.Add(this.buttonExcludeAdd);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(567, 264);
            this.panel2.Margin = new System.Windows.Forms.Padding(0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(65, 185);
            this.panel2.TabIndex = 3;
            // 
            // buttonExcludeRemove
            // 
            this.buttonExcludeRemove.Location = new System.Drawing.Point(3, 63);
            this.buttonExcludeRemove.Name = "buttonExcludeRemove";
            this.buttonExcludeRemove.Size = new System.Drawing.Size(59, 23);
            this.buttonExcludeRemove.TabIndex = 3;
            this.buttonExcludeRemove.Text = "Remove";
            this.buttonExcludeRemove.UseVisualStyleBackColor = true;
            this.buttonExcludeRemove.Click += new System.EventHandler(this.buttonExcludeRemove_Click);
            // 
            // buttonExcludeAdd
            // 
            this.buttonExcludeAdd.Location = new System.Drawing.Point(3, 23);
            this.buttonExcludeAdd.Name = "buttonExcludeAdd";
            this.buttonExcludeAdd.Size = new System.Drawing.Size(59, 23);
            this.buttonExcludeAdd.TabIndex = 2;
            this.buttonExcludeAdd.Text = "Add";
            this.buttonExcludeAdd.UseVisualStyleBackColor = true;
            this.buttonExcludeAdd.Click += new System.EventHandler(this.buttonExcludeAdd_Click);
            // 
            // panel3
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.panel3, 2);
            this.panel3.Controls.Add(this.buttonFolders);
            this.panel3.Controls.Add(this.dateTimePicker);
            this.panel3.Controls.Add(this.checkBoxAtTime);
            this.panel3.Controls.Add(this.buttonCheckBackup);
            this.panel3.Controls.Add(this.buttonReset);
            this.panel3.Controls.Add(this.checkBoxRegisterAutostart);
            this.panel3.Controls.Add(this.checkBoxAutoExecute);
            this.panel3.Controls.Add(this.progressBar);
            this.panel3.Controls.Add(this.buttonExecute);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Margin = new System.Windows.Forms.Padding(0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(632, 80);
            this.panel3.TabIndex = 4;
            // 
            // buttonFolders
            // 
            this.buttonFolders.Location = new System.Drawing.Point(554, 12);
            this.buttonFolders.Name = "buttonFolders";
            this.buttonFolders.Size = new System.Drawing.Size(75, 43);
            this.buttonFolders.TabIndex = 8;
            this.buttonFolders.Text = "Backup destination";
            this.toolTip.SetToolTip(this.buttonFolders, "select folders to backup");
            this.buttonFolders.UseVisualStyleBackColor = true;
            this.buttonFolders.Click += new System.EventHandler(this.buttonFolders_Click);
            // 
            // dateTimePicker
            // 
            this.dateTimePicker.CustomFormat = "HH:mm";
            this.dateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePicker.Location = new System.Drawing.Point(163, 41);
            this.dateTimePicker.Name = "dateTimePicker";
            this.dateTimePicker.ShowUpDown = true;
            this.dateTimePicker.Size = new System.Drawing.Size(54, 20);
            this.dateTimePicker.TabIndex = 7;
            this.dateTimePicker.ValueChanged += new System.EventHandler(this.dateTimePicker_ValueChanged);
            // 
            // checkBoxAtTime
            // 
            this.checkBoxAtTime.AutoSize = true;
            this.checkBoxAtTime.Location = new System.Drawing.Point(3, 42);
            this.checkBoxAtTime.Name = "checkBoxAtTime";
            this.checkBoxAtTime.Size = new System.Drawing.Size(162, 17);
            this.checkBoxAtTime.TabIndex = 6;
            this.checkBoxAtTime.Text = "Backup daily (awakes PC) at";
            this.checkBoxAtTime.UseVisualStyleBackColor = true;
            this.checkBoxAtTime.CheckedChanged += new System.EventHandler(this.checkBoxAtTime_CheckedChanged);
            // 
            // buttonCheckBackup
            // 
            this.buttonCheckBackup.Location = new System.Drawing.Point(374, 12);
            this.buttonCheckBackup.Name = "buttonCheckBackup";
            this.buttonCheckBackup.Size = new System.Drawing.Size(68, 43);
            this.buttonCheckBackup.TabIndex = 5;
            this.buttonCheckBackup.Text = "Check only";
            this.toolTip.SetToolTip(this.buttonCheckBackup, "backup test, check only");
            this.buttonCheckBackup.UseVisualStyleBackColor = true;
            this.buttonCheckBackup.Click += new System.EventHandler(this.buttonCheckBackup_Click);
            // 
            // buttonReset
            // 
            this.buttonReset.Location = new System.Drawing.Point(457, 22);
            this.buttonReset.Name = "buttonReset";
            this.buttonReset.Size = new System.Drawing.Size(59, 23);
            this.buttonReset.TabIndex = 4;
            this.buttonReset.Text = "Clear UI";
            this.toolTip.SetToolTip(this.buttonReset, "clear UI");
            this.buttonReset.UseVisualStyleBackColor = true;
            this.buttonReset.Click += new System.EventHandler(this.buttonReset_Click);
            // 
            // checkBoxRegisterAutostart
            // 
            this.checkBoxRegisterAutostart.AutoSize = true;
            this.checkBoxRegisterAutostart.Location = new System.Drawing.Point(3, 3);
            this.checkBoxRegisterAutostart.Name = "checkBoxRegisterAutostart";
            this.checkBoxRegisterAutostart.Size = new System.Drawing.Size(142, 17);
            this.checkBoxRegisterAutostart.TabIndex = 3;
            this.checkBoxRegisterAutostart.Text = "Register app to autostart";
            this.checkBoxRegisterAutostart.UseVisualStyleBackColor = true;
            this.checkBoxRegisterAutostart.CheckedChanged += new System.EventHandler(this.checkBoxRegisterAutostart_CheckedChanged);
            // 
            // checkBoxAutoExecute
            // 
            this.checkBoxAutoExecute.AutoSize = true;
            this.checkBoxAutoExecute.Location = new System.Drawing.Point(3, 22);
            this.checkBoxAutoExecute.Name = "checkBoxAutoExecute";
            this.checkBoxAutoExecute.Size = new System.Drawing.Size(158, 17);
            this.checkBoxAutoExecute.TabIndex = 2;
            this.checkBoxAutoExecute.Text = "Execute at app start && close";
            this.checkBoxAutoExecute.UseVisualStyleBackColor = true;
            this.checkBoxAutoExecute.CheckedChanged += new System.EventHandler(this.checkBoxAutoExecute_CheckedChanged);
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(3, 67);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(626, 10);
            this.progressBar.Step = 1;
            this.progressBar.TabIndex = 1;
            // 
            // buttonExecute
            // 
            this.buttonExecute.Location = new System.Drawing.Point(254, 12);
            this.buttonExecute.Name = "buttonExecute";
            this.buttonExecute.Size = new System.Drawing.Size(107, 43);
            this.buttonExecute.TabIndex = 0;
            this.buttonExecute.Text = "Execute backup";
            this.toolTip.SetToolTip(this.buttonExecute, "execute the backup");
            this.buttonExecute.UseVisualStyleBackColor = true;
            this.buttonExecute.Click += new System.EventHandler(this.buttonExecute_Click);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.listViewInclude, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 80);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(567, 184);
            this.tableLayoutPanel2.TabIndex = 7;
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(129, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Included items for backup";
            // 
            // listViewInclude
            // 
            this.listViewInclude.CheckBoxes = true;
            this.listViewInclude.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listViewInclude.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewInclude.FullRowSelect = true;
            this.listViewInclude.HideSelection = false;
            this.listViewInclude.Location = new System.Drawing.Point(3, 23);
            this.listViewInclude.Name = "listViewInclude";
            this.listViewInclude.Size = new System.Drawing.Size(561, 158);
            this.listViewInclude.TabIndex = 0;
            this.listViewInclude.UseCompatibleStateImageBehavior = false;
            this.listViewInclude.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Path";
            this.columnHeader1.Width = 410;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.listViewExclude, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 264);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(567, 185);
            this.tableLayoutPanel3.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 3);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(561, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Excluded items from backup";
            // 
            // listViewExclude
            // 
            this.listViewExclude.CheckBoxes = true;
            this.listViewExclude.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2});
            this.listViewExclude.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewExclude.FullRowSelect = true;
            this.listViewExclude.HideSelection = false;
            this.listViewExclude.Location = new System.Drawing.Point(3, 23);
            this.listViewExclude.Name = "listViewExclude";
            this.listViewExclude.Size = new System.Drawing.Size(561, 159);
            this.listViewExclude.TabIndex = 1;
            this.listViewExclude.UseCompatibleStateImageBehavior = false;
            this.listViewExclude.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Path";
            this.columnHeader2.Width = 412;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(632, 449);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MinimumSize = new System.Drawing.Size(640, 480);
            this.Name = "MainWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "BackUp Files and Folders";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button buttonIncludeRemove;
        private System.Windows.Forms.Button buttonIncludeAdd;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button buttonExcludeRemove;
        private System.Windows.Forms.Button buttonExcludeAdd;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button buttonExecute;
        private System.Windows.Forms.ListView listViewInclude;
        private System.Windows.Forms.ListView listViewExclude;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.CheckBox checkBoxRegisterAutostart;
        private System.Windows.Forms.CheckBox checkBoxAutoExecute;
        private System.Windows.Forms.Button buttonReset;
        private System.Windows.Forms.Button buttonCheckBackup;
        private System.Windows.Forms.DateTimePicker dateTimePicker;
        private System.Windows.Forms.CheckBox checkBoxAtTime;
        private System.Windows.Forms.Button buttonFolders;
        private System.Windows.Forms.ToolTip toolTip;
    }
}

