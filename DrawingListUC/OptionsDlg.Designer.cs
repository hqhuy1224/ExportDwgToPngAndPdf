namespace DrawingListUC
{
    partial class OptionsDlg
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
            if (disposing && (components != null))
            {
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
            label_timeout = new System.Windows.Forms.Label();
            textSeconds = new System.Windows.Forms.TextBox();
            OptionOK = new System.Windows.Forms.Button();
            OptionCancel = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            logPathBrowse = new System.Windows.Forms.Button();
            ProcessLogFilePath = new System.Windows.Forms.TextBox();
            restartAcad = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            searchFolder = new System.Windows.Forms.CheckBox();
            groupBox_image = new System.Windows.Forms.GroupBox();
            radioButton_none = new System.Windows.Forms.RadioButton();
            radioButton_failed = new System.Windows.Forms.RadioButton();
            radioButton_all = new System.Windows.Forms.RadioButton();
            diagnosticMode = new System.Windows.Forms.CheckBox();
            groupBox_exepath = new System.Windows.Forms.GroupBox();
            button_exePath = new System.Windows.Forms.Button();
            textBox_exePath = new System.Windows.Forms.TextBox();
            groupBox_speed = new System.Windows.Forms.GroupBox();
            trackBar_speed = new System.Windows.Forms.TrackBar();
            OpenDWGFile = new System.Windows.Forms.CheckBox();
            UseExeCheckbox = new System.Windows.Forms.CheckBox();
            groupBox1.SuspendLayout();
            groupBox_image.SuspendLayout();
            groupBox_exepath.SuspendLayout();
            groupBox_speed.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBar_speed).BeginInit();
            SuspendLayout();
            // 
            // label_timeout
            // 
            label_timeout.AutoSize = true;
            label_timeout.Location = new System.Drawing.Point(23, 81);
            label_timeout.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label_timeout.Name = "label_timeout";
            label_timeout.Size = new System.Drawing.Size(262, 15);
            label_timeout.TabIndex = 0;
            label_timeout.Text = "Process timeout per drawing in seconds ( >= 10)";
            // 
            // textSeconds
            // 
            textSeconds.Location = new System.Drawing.Point(312, 77);
            textSeconds.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textSeconds.Name = "textSeconds";
            textSeconds.Size = new System.Drawing.Size(56, 23);
            textSeconds.TabIndex = 1;
            textSeconds.Text = "30";
            // 
            // OptionOK
            // 
            OptionOK.Location = new System.Drawing.Point(352, 406);
            OptionOK.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            OptionOK.Name = "OptionOK";
            OptionOK.Size = new System.Drawing.Size(100, 29);
            OptionOK.TabIndex = 10;
            OptionOK.Text = "OK";
            OptionOK.UseVisualStyleBackColor = true;
            OptionOK.Click += OptionOK_Click;
            // 
            // OptionCancel
            // 
            OptionCancel.Location = new System.Drawing.Point(479, 406);
            OptionCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            OptionCancel.Name = "OptionCancel";
            OptionCancel.Size = new System.Drawing.Size(100, 29);
            OptionCancel.TabIndex = 11;
            OptionCancel.Text = "Cancel";
            OptionCancel.UseVisualStyleBackColor = true;
            OptionCancel.Click += OptionCancel_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(logPathBrowse);
            groupBox1.Controls.Add(ProcessLogFilePath);
            groupBox1.Location = new System.Drawing.Point(27, 154);
            groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Size = new System.Drawing.Size(573, 59);
            groupBox1.TabIndex = 4;
            groupBox1.TabStop = false;
            groupBox1.Text = "Process log folder";
            // 
            // logPathBrowse
            // 
            logPathBrowse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            logPathBrowse.Location = new System.Drawing.Point(470, 16);
            logPathBrowse.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            logPathBrowse.Name = "logPathBrowse";
            logPathBrowse.Size = new System.Drawing.Size(78, 29);
            logPathBrowse.TabIndex = 1;
            logPathBrowse.Text = "Browse";
            logPathBrowse.UseVisualStyleBackColor = true;
            logPathBrowse.Click += logPathBrowse_Click;
            // 
            // ProcessLogFilePath
            // 
            ProcessLogFilePath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ProcessLogFilePath.Location = new System.Drawing.Point(27, 20);
            ProcessLogFilePath.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            ProcessLogFilePath.Name = "ProcessLogFilePath";
            ProcessLogFilePath.Size = new System.Drawing.Size(436, 23);
            ProcessLogFilePath.TabIndex = 0;
            // 
            // restartAcad
            // 
            restartAcad.Location = new System.Drawing.Point(198, 107);
            restartAcad.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            restartAcad.Name = "restartAcad";
            restartAcad.Size = new System.Drawing.Size(56, 23);
            restartAcad.TabIndex = 2;
            restartAcad.Text = "30";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(23, 111);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(157, 15);
            label2.TabIndex = 5;
            label2.Text = "Restart AutoCAD after every ";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(262, 111);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(91, 15);
            label1.TabIndex = 7;
            label1.Text = "drawings (>= 1)";
            // 
            // searchFolder
            // 
            searchFolder.AutoSize = true;
            searchFolder.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            searchFolder.Location = new System.Drawing.Point(29, 355);
            searchFolder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            searchFolder.Name = "searchFolder";
            searchFolder.Size = new System.Drawing.Size(233, 19);
            searchFolder.TabIndex = 7;
            searchFolder.Text = "Select DWG/DXF files in sub directories ";
            searchFolder.UseVisualStyleBackColor = true;
            // 
            // groupBox_image
            // 
            groupBox_image.Controls.Add(radioButton_none);
            groupBox_image.Controls.Add(radioButton_failed);
            groupBox_image.Controls.Add(radioButton_all);
            groupBox_image.Location = new System.Drawing.Point(27, 250);
            groupBox_image.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox_image.Name = "groupBox_image";
            groupBox_image.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox_image.Size = new System.Drawing.Size(300, 68);
            groupBox_image.TabIndex = 5;
            groupBox_image.TabStop = false;
            groupBox_image.Text = "Create image before closing the drawing file";
            // 
            // radioButton_none
            // 
            radioButton_none.AutoSize = true;
            radioButton_none.Checked = true;
            radioButton_none.Location = new System.Drawing.Point(205, 29);
            radioButton_none.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            radioButton_none.Name = "radioButton_none";
            radioButton_none.Size = new System.Drawing.Size(54, 19);
            radioButton_none.TabIndex = 2;
            radioButton_none.TabStop = true;
            radioButton_none.Text = "None";
            radioButton_none.UseVisualStyleBackColor = true;
            // 
            // radioButton_failed
            // 
            radioButton_failed.AutoSize = true;
            radioButton_failed.Location = new System.Drawing.Point(90, 30);
            radioButton_failed.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            radioButton_failed.Name = "radioButton_failed";
            radioButton_failed.Size = new System.Drawing.Size(108, 19);
            radioButton_failed.TabIndex = 1;
            radioButton_failed.Text = "Only Failed files";
            radioButton_failed.UseVisualStyleBackColor = true;
            // 
            // radioButton_all
            // 
            radioButton_all.AutoSize = true;
            radioButton_all.Location = new System.Drawing.Point(20, 30);
            radioButton_all.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            radioButton_all.Name = "radioButton_all";
            radioButton_all.Size = new System.Drawing.Size(63, 19);
            radioButton_all.TabIndex = 0;
            radioButton_all.Text = "All files";
            radioButton_all.UseVisualStyleBackColor = true;
            // 
            // diagnosticMode
            // 
            diagnosticMode.AutoSize = true;
            diagnosticMode.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            diagnosticMode.Location = new System.Drawing.Point(337, 355);
            diagnosticMode.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            diagnosticMode.Name = "diagnosticMode";
            diagnosticMode.Size = new System.Drawing.Size(202, 19);
            diagnosticMode.TabIndex = 8;
            diagnosticMode.Text = "Run the tool in diagnostic  mode ";
            diagnosticMode.UseVisualStyleBackColor = true;
            // 
            // groupBox_exepath
            // 
            groupBox_exepath.Controls.Add(button_exePath);
            groupBox_exepath.Controls.Add(textBox_exePath);
            groupBox_exepath.Location = new System.Drawing.Point(29, 10);
            groupBox_exepath.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox_exepath.Name = "groupBox_exepath";
            groupBox_exepath.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox_exepath.Size = new System.Drawing.Size(573, 59);
            groupBox_exepath.TabIndex = 0;
            groupBox_exepath.TabStop = false;
            groupBox_exepath.Text = "AutoCAD application to use";
            // 
            // button_exePath
            // 
            button_exePath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            button_exePath.Location = new System.Drawing.Point(477, 17);
            button_exePath.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            button_exePath.Name = "button_exePath";
            button_exePath.Size = new System.Drawing.Size(74, 29);
            button_exePath.TabIndex = 1;
            button_exePath.Text = "Browse";
            button_exePath.UseVisualStyleBackColor = true;
            button_exePath.Click += button_exePath_Click;
            // 
            // textBox_exePath
            // 
            textBox_exePath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            textBox_exePath.Location = new System.Drawing.Point(27, 21);
            textBox_exePath.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBox_exePath.Name = "textBox_exePath";
            textBox_exePath.Size = new System.Drawing.Size(438, 23);
            textBox_exePath.TabIndex = 0;
            textBox_exePath.Leave += textBox_exePath_Leave;
            // 
            // groupBox_speed
            // 
            groupBox_speed.Controls.Add(trackBar_speed);
            groupBox_speed.Location = new System.Drawing.Point(337, 250);
            groupBox_speed.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox_speed.Name = "groupBox_speed";
            groupBox_speed.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox_speed.Size = new System.Drawing.Size(265, 68);
            groupBox_speed.TabIndex = 6;
            groupBox_speed.TabStop = false;
            groupBox_speed.Text = "Delay during process (Seconds)";
            // 
            // trackBar_speed
            // 
            trackBar_speed.LargeChange = 1;
            trackBar_speed.Location = new System.Drawing.Point(15, 17);
            trackBar_speed.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            trackBar_speed.Name = "trackBar_speed";
            trackBar_speed.Size = new System.Drawing.Size(227, 45);
            trackBar_speed.TabIndex = 0;
            trackBar_speed.Scroll += trackBar_speed_Scroll;
            // 
            // OpenDWGFile
            // 
            OpenDWGFile.AutoSize = true;
            OpenDWGFile.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            OpenDWGFile.Location = new System.Drawing.Point(30, 382);
            OpenDWGFile.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            OpenDWGFile.Name = "OpenDWGFile";
            OpenDWGFile.Size = new System.Drawing.Size(244, 19);
            OpenDWGFile.TabIndex = 9;
            OpenDWGFile.Text = "Run script without opening drawing file   ";
            OpenDWGFile.UseVisualStyleBackColor = true;
            // 
            // UseExeCheckbox
            // 
            UseExeCheckbox.AutoSize = true;
            UseExeCheckbox.Location = new System.Drawing.Point(27, 415);
            UseExeCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            UseExeCheckbox.Name = "UseExeCheckbox";
            UseExeCheckbox.Size = new System.Drawing.Size(303, 19);
            UseExeCheckbox.TabIndex = 12;
            UseExeCheckbox.Text = "Use script as commandline argument for application";
            UseExeCheckbox.UseVisualStyleBackColor = true;
            UseExeCheckbox.Visible = false;
            UseExeCheckbox.CheckedChanged += UseExeCheckbox_CheckedChanged;
            // 
            // OptionsDlg
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(625, 452);
            Controls.Add(UseExeCheckbox);
            Controls.Add(OpenDWGFile);
            Controls.Add(groupBox_speed);
            Controls.Add(groupBox_exepath);
            Controls.Add(diagnosticMode);
            Controls.Add(groupBox_image);
            Controls.Add(searchFolder);
            Controls.Add(label1);
            Controls.Add(restartAcad);
            Controls.Add(label2);
            Controls.Add(groupBox1);
            Controls.Add(OptionCancel);
            Controls.Add(OptionOK);
            Controls.Add(textSeconds);
            Controls.Add(label_timeout);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "OptionsDlg";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Options";
            Load += OptionsDlg_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox_image.ResumeLayout(false);
            groupBox_image.PerformLayout();
            groupBox_exepath.ResumeLayout(false);
            groupBox_exepath.PerformLayout();
            groupBox_speed.ResumeLayout(false);
            groupBox_speed.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackBar_speed).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_timeout;
        private System.Windows.Forms.TextBox textSeconds;
        private System.Windows.Forms.Button OptionOK;
        private System.Windows.Forms.Button OptionCancel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button logPathBrowse;
        private System.Windows.Forms.TextBox ProcessLogFilePath;
        private System.Windows.Forms.TextBox restartAcad;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox searchFolder;
        private System.Windows.Forms.GroupBox groupBox_image;
        private System.Windows.Forms.RadioButton radioButton_failed;
        private System.Windows.Forms.RadioButton radioButton_all;
        private System.Windows.Forms.RadioButton radioButton_none;
        private System.Windows.Forms.CheckBox diagnosticMode;
        private System.Windows.Forms.GroupBox groupBox_exepath;
        private System.Windows.Forms.Button button_exePath;
        private System.Windows.Forms.TextBox textBox_exePath;
        private System.Windows.Forms.GroupBox groupBox_speed;
        private System.Windows.Forms.TrackBar trackBar_speed;
        private System.Windows.Forms.CheckBox OpenDWGFile;
        private System.Windows.Forms.CheckBox UseExeCheckbox;
    }
}