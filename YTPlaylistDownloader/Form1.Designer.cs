namespace YTPlaylistDownloader
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblUrl = new Label();
            txtUrl = new TextBox();
            btnFetch = new Button();
            lblOutputDir = new Label();
            txtOutputDir = new TextBox();
            btnBrowse = new Button();
            clbSongs = new CheckedListBox();
            btnSelectAll = new Button();
            btnSelectNone = new Button();
            lblSelected = new Label();
            progressBar = new ProgressBar();
            lblStatus = new Label();
            btnDownload = new Button();
            SuspendLayout();

            lblUrl.AutoSize = true;
            lblUrl.Location = new Point(12, 17);
            lblUrl.Text = "Playlist URL:";

            txtUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtUrl.Location = new Point(115, 14);
            txtUrl.Size = new Size(554, 23);

            btnFetch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnFetch.Location = new Point(675, 13);
            btnFetch.Size = new Size(113, 25);
            btnFetch.Text = "Fetch Songs";
            btnFetch.Click += btnFetch_Click;

            lblOutputDir.AutoSize = true;
            lblOutputDir.Location = new Point(12, 51);
            lblOutputDir.Text = "Output Folder:";

            txtOutputDir.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtOutputDir.Location = new Point(115, 48);
            txtOutputDir.Size = new Size(454, 23);

            btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowse.Location = new Point(577, 47);
            btnBrowse.Size = new Size(80, 25);
            btnBrowse.Text = "Browse";
            btnBrowse.Click += btnBrowse_Click;

            clbSongs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            clbSongs.CheckOnClick = true;
            clbSongs.Location = new Point(12, 83);
            clbSongs.Size = new Size(776, 312);
            clbSongs.ItemCheck += clbSongs_ItemCheck;

            btnSelectAll.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSelectAll.Location = new Point(12, 405);
            btnSelectAll.Size = new Size(105, 27);
            btnSelectAll.Text = "Select All";
            btnSelectAll.Click += btnSelectAll_Click;

            btnSelectNone.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSelectNone.Location = new Point(124, 405);
            btnSelectNone.Size = new Size(120, 27);
            btnSelectNone.Text = "Deselect All";
            btnSelectNone.Click += btnSelectNone_Click;

            lblSelected.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblSelected.Location = new Point(255, 410);
            lblSelected.Size = new Size(533, 18);
            lblSelected.TextAlign = ContentAlignment.MiddleRight;

            progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(12, 440);
            progressBar.Size = new Size(776, 23);

            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.Location = new Point(12, 471);
            lblStatus.Size = new Size(645, 20);
            lblStatus.Text = "Ready";

            btnDownload.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDownload.Enabled = false;
            btnDownload.Location = new Point(664, 466);
            btnDownload.Size = new Size(124, 28);
            btnDownload.Text = "Download Selected";
            btnDownload.Click += btnDownload_Click;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 506);
            MinimumSize = new Size(700, 524);
            Controls.AddRange(new Control[] {
                lblUrl, txtUrl, btnFetch,
                lblOutputDir, txtOutputDir, btnBrowse,
                clbSongs,
                btnSelectAll, btnSelectNone, lblSelected,
                progressBar, lblStatus, btnDownload
            });
            Text = "YouTube Playlist Downloader";

            ResumeLayout(false);
            PerformLayout();
        }

        private Label lblUrl;
        private TextBox txtUrl;
        private Button btnFetch;
        private Label lblOutputDir;
        private TextBox txtOutputDir;
        private Button btnBrowse;
        private CheckedListBox clbSongs;
        private Button btnSelectAll;
        private Button btnSelectNone;
        private Label lblSelected;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Button btnDownload;
    }
}
