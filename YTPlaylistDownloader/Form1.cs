using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace YTPlaylistDownloader
{
    public partial class Form1 : Form
    {
        private readonly List<(string Title, string Id)> _songs = new();
        private CancellationTokenSource? _cts;
        private bool _isDownloading;

        public Form1()
        {
            InitializeComponent();
            txtOutputDir.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        }

        private static string FindExecutable(string name)
        {
            var appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
            var local = Path.Combine(appDir, name + ".exe");
            if (File.Exists(local)) return local;

            foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var full = Path.Combine(dir.Trim(), name + ".exe");
                    if (File.Exists(full)) return full;
                }
                catch { }
            }
            return "";
        }

        private async Task<bool> EnsureDependenciesAsync()
        {
            bool needsYtdlp = string.IsNullOrEmpty(FindExecutable("yt-dlp"));
            bool needsFfmpeg = string.IsNullOrEmpty(FindExecutable("ffmpeg"));

            if (!needsYtdlp && !needsFfmpeg) return true;

            var missing = new List<string>();
            if (needsYtdlp) missing.Add("yt-dlp  (~17 MB)");
            if (needsFfmpeg) missing.Add("ffmpeg  (~75 MB)");

            var result = MessageBox.Show(
                $"The following required tools are missing:\n\n• {string.Join("\n• ", missing)}\n\nDownload them now?",
                "Required Tools Missing",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result != DialogResult.Yes) return false;

            var appDir = Path.GetDirectoryName(Application.ExecutablePath)!;
            SetFetchState(true);
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0;

            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "YTPlaylistDownloader");

                if (needsYtdlp)
                {
                    await DownloadWithProgressAsync(http,
                        "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe",
                        Path.Combine(appDir, "yt-dlp.exe"),
                        "yt-dlp");
                }

                if (needsFfmpeg)
                {
                    lblStatus.Text = "Finding latest ffmpeg release...";
                    progressBar.Style = ProgressBarStyle.Marquee;

                    var json = await http.GetStringAsync("https://api.github.com/repos/GyanD/codexffmpeg/releases/latest");
                    using var doc = JsonDocument.Parse(json);
                    string? zipUrl = null;
                    foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
                    {
                        var name = asset.GetProperty("name").GetString() ?? "";
                        if (name.Contains("essentials_build") && name.EndsWith(".zip"))
                        {
                            zipUrl = asset.GetProperty("browser_download_url").GetString();
                            break;
                        }
                    }

                    if (zipUrl == null)
                    {
                        MessageBox.Show("Could not find a ffmpeg download URL.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    progressBar.Style = ProgressBarStyle.Blocks;
                    var tmpZip = Path.Combine(Path.GetTempPath(), "ffmpeg_essentials.zip");
                    await DownloadWithProgressAsync(http, zipUrl, tmpZip, "ffmpeg");

                    lblStatus.Text = "Extracting ffmpeg...";
                    progressBar.Style = ProgressBarStyle.Marquee;

                    var tmpDir = Path.Combine(Path.GetTempPath(), "ffmpeg_extract");
                    if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true);
                    ZipFile.ExtractToDirectory(tmpZip, tmpDir);

                    var ffmpegExe  = Directory.GetFiles(tmpDir, "ffmpeg.exe",  SearchOption.AllDirectories).FirstOrDefault();
                    var ffprobeExe = Directory.GetFiles(tmpDir, "ffprobe.exe", SearchOption.AllDirectories).FirstOrDefault();
                    if (ffmpegExe  != null) File.Copy(ffmpegExe,  Path.Combine(appDir, "ffmpeg.exe"),  overwrite: true);
                    if (ffprobeExe != null) File.Copy(ffprobeExe, Path.Combine(appDir, "ffprobe.exe"), overwrite: true);

                    File.Delete(tmpZip);
                    Directory.Delete(tmpDir, true);
                    progressBar.Style = ProgressBarStyle.Blocks;
                }

                lblStatus.Text = "All tools are ready.";
                progressBar.Value = 100;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Download failed.";
                return false;
            }
            finally
            {
                progressBar.Style = ProgressBarStyle.Blocks;
                progressBar.Value = 0;
                SetFetchState(false);
            }
        }

        private async Task DownloadWithProgressAsync(HttpClient http, string url, string destination, string label)
        {
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? -1;
            using var src  = await response.Content.ReadAsStreamAsync();
            using var dest = File.Create(destination);

            var buf = new byte[81920];
            long downloaded = 0;
            int read;

            while ((read = await src.ReadAsync(buf)) > 0)
            {
                await dest.WriteAsync(buf.AsMemory(0, read));
                downloaded += read;

                if (total > 0)
                {
                    progressBar.Value = (int)(downloaded * 100 / total);
                    lblStatus.Text = $"Downloading {label}... {downloaded / 1024 / 1024} MB / {total / 1024 / 1024} MB";
                }
                else
                {
                    lblStatus.Text = $"Downloading {label}... {downloaded / 1024 / 1024} MB";
                }
            }
        }

        private async void btnFetch_Click(object sender, EventArgs e)
        {
            var url = txtUrl.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please enter a playlist URL.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!await EnsureDependenciesAsync()) return;

            SetFetchState(true);
            clbSongs.Items.Clear();
            _songs.Clear();
            UpdateSelectedLabel();
            lblStatus.Text = "Fetching playlist info...";
            progressBar.Style = ProgressBarStyle.Marquee;

            _cts = new CancellationTokenSource();
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = FindExecutable("yt-dlp"),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                psi.ArgumentList.Add("--flat-playlist");
                psi.ArgumentList.Add("--print");
                psi.ArgumentList.Add("%(title)s|||%(id)s");
                psi.ArgumentList.Add(url);

                using var process = new Process { StartInfo = psi };
                process.Start();

                var stdoutTask = process.StandardOutput.ReadToEndAsync(_cts.Token);
                var stderrTask = process.StandardError.ReadToEndAsync(_cts.Token);
                await process.WaitForExitAsync(_cts.Token);
                var output = await stdoutTask;
                var stderr  = await stderrTask;

                if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
                {
                    var msg = stderr.Length > 500 ? stderr[..500] + "..." : stderr;
                    MessageBox.Show($"yt-dlp returned an error:\n\n{msg}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "An error occurred.";
                    return;
                }

                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var sep = line.LastIndexOf("|||");
                    if (sep < 0) continue;
                    var title = line[..sep].Trim();
                    var id    = line[(sep + 3)..].Trim();
                    if (string.IsNullOrEmpty(id)) continue;
                    _songs.Add((title, id));
                    clbSongs.Items.Add($"{_songs.Count}. {title}", true);
                }

                lblStatus.Text = _songs.Count > 0
                    ? $"{_songs.Count} songs found."
                    : "No songs found in the playlist.";
                UpdateSelectedLabel();
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Cancelled.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "An error occurred.";
            }
            finally
            {
                progressBar.Style = ProgressBarStyle.Blocks;
                progressBar.Value = 0;
                SetFetchState(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            if (_isDownloading)
            {
                _cts?.Cancel();
                return;
            }

            var outputDir = txtOutputDir.Text.Trim();
            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
            {
                MessageBox.Show("Please select a valid output folder.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedIndices = clbSongs.CheckedIndices.Cast<int>().ToList();
            if (selectedIndices.Count == 0)
            {
                MessageBox.Show("Please select at least one song.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!await EnsureDependenciesAsync()) return;

            SetDownloadState(true);
            progressBar.Maximum = selectedIndices.Count;
            progressBar.Value = 0;
            _cts = new CancellationTokenSource();
            int downloaded = 0, errors = 0;

            try
            {
                for (int i = 0; i < selectedIndices.Count; i++)
                {
                    if (_cts.Token.IsCancellationRequested) break;

                    var (title, id) = _songs[selectedIndices[i]];
                    lblStatus.Text = $"Downloading ({i + 1}/{selectedIndices.Count}): {title}";

                    var psi = new ProcessStartInfo
                    {
                        FileName = FindExecutable("yt-dlp"),
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    psi.ArgumentList.Add("-x");
                    psi.ArgumentList.Add("--audio-format");
                    psi.ArgumentList.Add("mp3");
                    psi.ArgumentList.Add("--audio-quality");
                    psi.ArgumentList.Add("0");
                    psi.ArgumentList.Add("-o");
                    psi.ArgumentList.Add(Path.Combine(outputDir, "%(title)s.%(ext)s"));
                    psi.ArgumentList.Add($"https://www.youtube.com/watch?v={id}");

                    using var process = new Process { StartInfo = psi };
                    process.Start();
                    var stderrTask = process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync(_cts.Token);
                    await stderrTask;

                    if (process.ExitCode == 0) downloaded++;
                    else errors++;

                    progressBar.Value = i + 1;
                }

                if (_cts.Token.IsCancellationRequested)
                    lblStatus.Text = $"Cancelled. {downloaded} song(s) downloaded.";
                else if (errors == 0)
                    lblStatus.Text = $"Done! {downloaded} song(s) downloaded.";
                else
                    lblStatus.Text = $"Done. {downloaded} succeeded, {errors} failed.";
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = $"Cancelled. {downloaded} song(s) downloaded.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "An error occurred.";
            }
            finally
            {
                SetDownloadState(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = txtOutputDir.Text };
            if (dialog.ShowDialog() == DialogResult.OK)
                txtOutputDir.Text = dialog.SelectedPath;
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            clbSongs.ItemCheck -= clbSongs_ItemCheck;
            for (int i = 0; i < clbSongs.Items.Count; i++)
                clbSongs.SetItemChecked(i, true);
            clbSongs.ItemCheck += clbSongs_ItemCheck;
            UpdateSelectedLabel();
        }

        private void btnSelectNone_Click(object sender, EventArgs e)
        {
            clbSongs.ItemCheck -= clbSongs_ItemCheck;
            for (int i = 0; i < clbSongs.Items.Count; i++)
                clbSongs.SetItemChecked(i, false);
            clbSongs.ItemCheck += clbSongs_ItemCheck;
            UpdateSelectedLabel();
        }

        private void clbSongs_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            BeginInvoke(UpdateSelectedLabel);
        }

        private void UpdateSelectedLabel()
        {
            var selected = clbSongs.CheckedItems.Count;
            var total = clbSongs.Items.Count;
            lblSelected.Text = total > 0 ? $"Selected: {selected} / {total} songs" : "";
            if (!_isDownloading)
                btnDownload.Enabled = selected > 0;
        }

        private void SetFetchState(bool fetching)
        {
            btnFetch.Enabled = !fetching;
            txtUrl.Enabled = !fetching;
            btnDownload.Enabled = false;
        }

        private void SetDownloadState(bool downloading)
        {
            _isDownloading = downloading;
            btnDownload.Text = downloading ? "Cancel" : "Download Selected";
            btnFetch.Enabled = !downloading;
            txtUrl.Enabled = !downloading;
            txtOutputDir.Enabled = !downloading;
            btnBrowse.Enabled = !downloading;
            btnSelectAll.Enabled = !downloading;
            btnSelectNone.Enabled = !downloading;
            clbSongs.Enabled = !downloading;
        }
    }
}
