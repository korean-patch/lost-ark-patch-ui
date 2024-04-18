namespace LostArkKoreanPatch.Main
{
    partial class LostArkKoreanPatch
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LostArkKoreanPatch));
            this.initialChecker = new System.ComponentModel.BackgroundWorker();
            this.installWorker = new System.ComponentModel.BackgroundWorker();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.downloadLabel = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();
            this.removeButton = new System.Windows.Forms.Button();
            this.installButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // initialChecker
            // 
            this.initialChecker.WorkerReportsProgress = true;
            this.initialChecker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.initialChecker_DoWork);
            this.initialChecker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.initialChecker_ProgressChanged);
            // 
            // installWorker
            // 
            this.installWorker.WorkerReportsProgress = true;
            this.installWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.installWorker_DoWork);
            this.installWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.initialChecker_ProgressChanged);
            // 
            // progressBar
            // 
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progressBar.Location = new System.Drawing.Point(0, 401);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(384, 10);
            this.progressBar.TabIndex = 0;
            // 
            // downloadLabel
            // 
            this.downloadLabel.AutoSize = true;
            this.downloadLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.downloadLabel.Location = new System.Drawing.Point(0, 376);
            this.downloadLabel.Name = "downloadLabel";
            this.downloadLabel.Padding = new System.Windows.Forms.Padding(10, 0, 0, 10);
            this.downloadLabel.Size = new System.Drawing.Size(98, 25);
            this.downloadLabel.TabIndex = 0;
            this.downloadLabel.Text = "downloadLabel";
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusLabel.Location = new System.Drawing.Point(0, 331);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Padding = new System.Windows.Forms.Padding(10, 20, 0, 10);
            this.statusLabel.Size = new System.Drawing.Size(76, 45);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "statusLabel";
            // 
            // removeButton
            // 
            this.removeButton.AutoSize = true;
            this.removeButton.BackColor = System.Drawing.Color.Transparent;
            this.removeButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.removeButton.Enabled = false;
            this.removeButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.removeButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.removeButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.removeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.removeButton.Location = new System.Drawing.Point(0, 284);
            this.removeButton.Margin = new System.Windows.Forms.Padding(10);
            this.removeButton.Name = "removeButton";
            this.removeButton.Padding = new System.Windows.Forms.Padding(10);
            this.removeButton.Size = new System.Drawing.Size(384, 47);
            this.removeButton.TabIndex = 0;
            this.removeButton.TabStop = false;
            this.removeButton.Text = "한글 패치 삭제";
            this.removeButton.UseVisualStyleBackColor = false;
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
            // 
            // installButton
            // 
            this.installButton.AutoSize = true;
            this.installButton.BackColor = System.Drawing.Color.Transparent;
            this.installButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.installButton.Enabled = false;
            this.installButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.installButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.installButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.installButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.installButton.Location = new System.Drawing.Point(0, 237);
            this.installButton.Margin = new System.Windows.Forms.Padding(10);
            this.installButton.Name = "installButton";
            this.installButton.Padding = new System.Windows.Forms.Padding(10);
            this.installButton.Size = new System.Drawing.Size(384, 47);
            this.installButton.TabIndex = 0;
            this.installButton.TabStop = false;
            this.installButton.Text = "한글 패치 설치";
            this.installButton.UseVisualStyleBackColor = false;
            this.installButton.Click += new System.EventHandler(this.installButton_Click);
            // 
            // LostArkKoreanPatch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(384, 411);
            this.Controls.Add(this.installButton);
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.downloadLabel);
            this.Controls.Add(this.progressBar);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LostArkKoreanPatch";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "로스트 아크 한글 패치";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.ComponentModel.BackgroundWorker initialChecker;
        private System.ComponentModel.BackgroundWorker installWorker;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label downloadLabel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.Button installButton;
    }
}