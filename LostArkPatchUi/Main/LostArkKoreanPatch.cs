using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LostArkKoreanPatch.Main
{
    public partial class LostArkKoreanPatch : Form
    {
        #region Variables

        // Github release server URL that hosts distributed patch files.
        private const string serverUrl = "https://github.com/korean-patch/lost-ark-korean-patch/releases/download/release";

        // Path for the main patch program.
        private string mainPath = string.Empty;
        private const string mainFileName = "LostArkKoreanPatch";
        private string mainTempPath = string.Empty;

        // Path for the updater program.
        private string updaterPath = string.Empty;
        private const string updaterFileName = "LostArkKoreanPatchUpdater";

        // Process names to check for before doing the patch.
        private string[] gameProcessNames = new string[]
        {
            "lostark"
        };

        // List of known files that will be used to verify installation path.
        private string[] requiredFiles = new string[]
        {
            "Binaries/Win64/LOSTARK.exe",
            "EFGame/config.lpk",
            "EFGame/data1.lpk",
            "EFGame/data2.lpk",
            "EFGame/data3.lpk",
            "EFGame/data4.lpk",
            "EFGame/font.lpk",
            "EFGame/leveldata1.lpk",
            "EFGame/leveldata2.lpk",
        };

        // List of file names that need to be manipulated.
        private string[] patchFiles = new string[]
        {
            "data2.lpk",
            "font.lpk"
        };

        // Name of the version file that denotes target game client version for the patch.
        private const string versionFileName = "LOSTARK.ver";

        // Name of the logo movie file.
        private const string logoFileName = "E2GFL7M0PSEWD6V1B8YTEO.ipk";

        // Target client directory.
        private string targetDir = string.Empty;

        // Target client version.
        private string targetVersion = string.Empty;

        // Patch version.
        private string serverVersion = string.Empty;

        #endregion

        public LostArkKoreanPatch()
        {
            InitializeComponent();

            // Adjust the background to apply gradient effect.
            AdjustBackground();

            // Empty the labels.
            statusLabel.Text = "";
            downloadLabel.Text = "";

            // Run the initial checker to verify and set up environment.
            initialChecker.RunWorkerAsync();
        }

        #region Functions

        // Grab the background from the form and apply gradient effect.
        private void AdjustBackground()
        {
            // Get the background image as Bitmap first.
            Bitmap origImage = (Bitmap)BackgroundImage;

            // Create a new image that will be used as a new background.
            // This should have the same width as the form, and the same width-height ratio.
            Bitmap newImage = new Bitmap(ClientSize.Width, ClientSize.Width * origImage.Height / origImage.Width);
            newImage.SetResolution(origImage.HorizontalResolution, origImage.VerticalResolution);

            // Starting drawing in the new image...
            using (Graphics g = Graphics.FromImage(newImage))
            {
                // Prepare a rectangle to copy over the original image.
                Rectangle rect = new Rectangle(0, 0, newImage.Width, newImage.Height);

                // Set Graphics parameters...
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // Copy over the original image.
                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    g.DrawImage(origImage, rect, 0, 0, origImage.Width, origImage.Height, GraphicsUnit.Pixel, wrapMode);
                }

                // Prepare a linear gradient brush that is transparent on top and form back color at the bottom.
                LinearGradientBrush brush = new LinearGradientBrush(rect, Color.Transparent, BackColor, 90f);

                // Draw on top of the original image.
                g.FillRectangle(brush, rect);
            }

            // Set the new image as background.
            BackgroundImage = newImage;
        }

        // Display message box from UI thread.
        private DialogResult ShowMessageBox(MessageBoxButtons buttons, MessageBoxIcon icon, params string[] lines)
        {
            // Compile a single string from given text lines.
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                sb.Append(lines[i]);

                // Append 2 new lines in between...
                if (i != lines.Length - 1)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(Environment.NewLine);
                }
            }

            // Display message box on UI thread with given parameters.
            return (DialogResult)Invoke(new Func<DialogResult>(() =>
            {
                return MessageBox.Show(sb.ToString(), Text, buttons, icon);
            }));
        }

        // Update status label text always from UI thread.
        private void UpdateStatusLabel(string text, bool isRed = false)
        {
            Invoke(new Action(() =>
            {
                statusLabel.Text = text;
                statusLabel.ForeColor = isRed ? Color.Red : Color.White;
            }));
        }

        // Update download label text always from UI thread.
        private void UpdateDownloadLabel(string text)
        {
            Invoke(new Action(() =>
            {
                downloadLabel.Text = text;
            }));
        }

        // Close the form always from UI thread.
        private void CloseForm()
        {
            Invoke(new Action(() =>
            {
                Close();
            }));
        }

        // Validates the target client directory by checking if required files are present.
        // Return the directory back if valid, else return null.
        private string CheckTargetDir(string targetDir)
        {
            if (!Directory.Exists(targetDir)) return null;
            if (!requiredFiles.All(requiredFile => File.Exists(Path.Combine(targetDir, requiredFile)))) return null;

            return targetDir;
        }

        // Downloads a file from given url while reporting progress on given background worker, then return the response as byte array.
        // If filePath is given, it will write to FileStream instead of memory.
        private byte[] DownloadFile(string url, string fileName, BackgroundWorker worker, string filePath = null)
        {
            using (HttpClient client = new HttpClient())
            {
                // Default user agent and timeout values.
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
                client.Timeout = TimeSpan.FromMinutes(5);

                // Indicate what file we're downloading...
                UpdateDownloadLabel($"다운로드중: {fileName}");

                // Download the header first to look at the content length.
                using (HttpResponseMessage responseMessage = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
                {
                    if (responseMessage.Content.Headers.ContentLength != null)
                    {
                        long contentLength = (long)responseMessage.Content.Headers.ContentLength;

                        // Create memory stream or file stream based on param and feed it with the client stream.
                        using (Stream s = string.IsNullOrEmpty(filePath) ? (Stream)new MemoryStream() : new FileStream(filePath, FileMode.Create))
                        using (Stream inStream = responseMessage.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                        {
                            // Create a progress reporter that reports the progress to the designated background worker.
                            Progress<int> p = new Progress<int>(new Action<int>((value) =>
                            {
                                worker.ReportProgress(value);
                            }));

                            // Buffer size is 1/10 of the total content.
                            // Grab data from http client stream and copy to destination stream.
                            inStream.CopyToAsync(s, (int)(contentLength / 10), contentLength, p).GetAwaiter().GetResult();

                            // Empty the progress bar after download is complete.
                            worker.ReportProgress(0);

                            // Reset the label.
                            UpdateDownloadLabel("");

                            // If no file path was specified, return as byte array. Else just return.
                            if (string.IsNullOrEmpty(filePath))
                            {
                                return ((MemoryStream)s).ToArray();
                            }
                            else
                            {
                                return new byte[0];
                            }
                        }
                    }
                }
            }

            // If anything happened and didn't reach successful download, throw an exception.
            throw new Exception($"다음 파일을 다운로드하는 중 오류가 발생하였습니다. {url}");
        }

        // Clear all cached files.
        private void ClearCache()
        {
            foreach (string s in Directory.GetFiles(Application.CommonAppDataPath))
            {
                File.Delete(s);
            }
        }

        // Get the SHA1 checksum from a file.
        private string ComputeSHA1(string filePath)
        {
            using (SHA1CryptoServiceProvider cryptoProvider = new SHA1CryptoServiceProvider())
            {
                return BitConverter.ToString(cryptoProvider.ComputeHash(File.ReadAllBytes(filePath))).Replace("-", "");
            }
        }

        // Check SHA1 checksum between given file and server record and return true if they match.
        private bool CheckSHA1(string filePath, string url, string fileName, BackgroundWorker worker)
        {
            return ComputeSHA1(filePath) == Encoding.ASCII.GetString(DownloadFile(url, fileName, worker));
        }

        #endregion

        #region Event Handlers

        // Show progress using progress bar.
        private void initialChecker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        // Background worker that does initial checks.
        private void initialChecker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Wait until the UI is ready.
                while (!statusLabel.IsHandleCreated) { }

                UpdateStatusLabel("환경 체크 중...");

                // Check if the patch program is already running, and terminate if it is.
                if (Process.GetProcessesByName(mainFileName).Length > 1)
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "로스트 아크 한글 패치 프로그램이 이미 실행중이에요.");
                    CloseForm();
                    return;
                }

                // Check if lost ark game process is running.
                if (Process.GetProcesses().Any(p => gameProcessNames.Contains(p.ProcessName.ToLower())))
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "로스트 아크가 이미 실행중이에요.",
                        "로스트 아크를 종료한 후 한글 패치 프로그램을 다시 실행해주세요.");
                    CloseForm();
                    return;
                }

                // Populate necessary paths.
                mainPath = Application.ExecutablePath;
                mainTempPath = Path.Combine(Application.CommonAppDataPath, $"{mainFileName}.exe");
                updaterPath = Path.Combine(Application.CommonAppDataPath, $"{updaterFileName}.exe");

                // Clean up some stuff.
                ClearCache();

                // Check main executable's version and update if necessary.
                UpdateStatusLabel("프로그램 버전 확인 중...");

                try
                {
#if DEBUG
#else
                    // Check the current executable's checksum with the server.
                    if (!CheckSHA1(mainPath, $"{serverUrl}/{mainFileName}.exe.sha1", $"{mainFileName}.exe.sha1", initialChecker))
                    {
                        // If doesn't match, need to download the new binary and updater, then trigger an update.
                        DownloadFile($"{serverUrl}/{mainFileName}.exe", $"{mainFileName}.exe", initialChecker, mainTempPath);
                        DownloadFile($"{serverUrl}/{updaterFileName}.exe", $"{updaterFileName}.exe", initialChecker, updaterPath);

                        // Run updater worker process to update the main executable.
                        ShowMessageBox(
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information,
                            "업데이트가 필요해 프로그램을 종료할 거예요.",
                            "업데이트가 완료되면 자동으로 재실행할게요.");
                        Process.Start(new ProcessStartInfo(updaterPath, $"\"{mainPath}\" \"{mainTempPath}\""));
                        CloseForm();
                        return;
                    }
#endif
                }
                catch (Exception exception)
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "버전을 확인하는데 실패했어요.",
                        "문제가 지속될 경우 디스코드를 통해 문의해주세요.",
                        "에러 내용: ",
                        exception.ToString());
                    CloseForm();
                    return;
                }

                // Try to detect lost ark client path.
                UpdateStatusLabel("로스트 아크 클라이언트를 찾는 중...");

                try
                {
                    // Check windows registry uninstall list to find the lost ark installation.
                    string[] uninstallSteamKeyNames = new string[]
                    {
                        $"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 1599340",
                        $"SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 1599340"
                    };

                    // Check steam registry...
                    using (RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32))
                    {
                        foreach (string uninstallSteamKeyName in uninstallSteamKeyNames)
                        {
                            if (!string.IsNullOrEmpty(targetDir)) break;

                            using (RegistryKey uninstallKey = localMachine.OpenSubKey(uninstallSteamKeyName))
                            {
                                if (uninstallKey == null) continue;

                                object installLocation = uninstallKey.GetValue("InstallLocation");
                                if (installLocation == null) continue;

                                targetDir = CheckTargetDir(Path.GetFullPath(installLocation.ToString()));
                            }
                        }
                    }
                }
                catch
                {
                    // Any exception happened during dtection, just set directory to not found so user can select it.
                    targetDir = string.Empty;
                }

                // If the installation location is found, ask user to confirm.
                bool isTargetDirVerified = false;

                if (!string.IsNullOrEmpty(targetDir))
                {
                    isTargetDirVerified = ShowMessageBox(
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information,
                        "다음 위치에서 로스트 아크 클라이언트가 발견되었어요.",
                        targetDir,
                        "이 클라이언트에 한글 패치를 설치할까요?") == DialogResult.Yes;
                }

                // If the target directory is not verified, let user choose.
                if (!isTargetDirVerified)
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information,
                        "로스트 아크 클라이언트가 설치된 장소에서 LOSTARK.exe 파일을 찾아 선택해주세요.",
                        "(보통 Binaries/Win64 폴더 내부에 있어요.)");

                    // Start the dialog from UI thread.
                    Invoke(new Action(() =>
                    {
                        OpenFileDialog dialog = new OpenFileDialog()
                        {
                            CheckFileExists = true,
                            CheckPathExists = true,
                            DefaultExt = "exe",
                            Filter = "LOST ARK|LOSTARK.exe",
                            Multiselect = false,
                            Title = "LOSTARK.exe 파일을 선택해주세요..."
                        };

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            // Check if the selected directory is valid.
                            targetDir = CheckTargetDir(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(dialog.FileName), "..", "..")));
                        }
                    }));
                }

                // Last check for the targetDir.
                if (string.IsNullOrEmpty(targetDir))
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "선택된 경로가 올바르지 않아요.");
                    CloseForm();
                    return;
                }

                // Check the target client version.
                UpdateStatusLabel("클라이언트 버전 체크 중...");

                // Read the version from target client.
                targetVersion = File.ReadAllText(Path.Combine(targetDir, "Binaries", "misc", versionFileName));

                // Check if the server's client version is the same.
                UpdateStatusLabel($"서버에서 한글 패치 가져오는 중... 버전 {targetVersion}");

                try
                {
                    byte[] payload = DownloadFile($"{serverUrl}/{versionFileName}", versionFileName, initialChecker);
                    serverVersion = Encoding.ASCII.GetString(payload);

                    if (!serverVersion.Equals(targetVersion))
                    {
                        ShowMessageBox(
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            "현재 설치된 게임 클라이언트의 버전이 서버의 버전과 달라요!",
                            $"클라이언트 버전: {targetVersion}, 서버 버전: {serverVersion}",
                            "",
                            "(버전이 다를 경우 한글 패치가 정상적으로 작동하지 않을 수도 있으니 유의해주세요.)",
                            "문제가 지속되면 디스코드를 통해 문의해주세요.");
                    }
                }
                catch (Exception exception)
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "한글 패치 서버 버전을 확인하는데 실패했어요.",
                        "문제가 지속되면 디스코드를 통해 문의해주세요.",
                        "에러 내용:",
                        exception.ToString());
                    CloseForm();
                    return;
                }

                string logoDir = Path.Combine(targetDir, "EFGame", "Movies");
                string logoFilePath = Path.Combine(logoDir, logoFileName);
                bool skipLogoEnabled = Directory.Exists(logoDir) && (File.Exists(logoFilePath) || File.Exists(logoFilePath + ".bk"));
                string skipLogoButtonText = skipLogoEnabled ? ("로고 스킵 모드 " + (File.Exists(logoFilePath) ? "설치" : "제거")) : "-";

                // Check all done!
                UpdateStatusLabel($"버전 {targetVersion}, 패치 버전 {serverVersion}", !serverVersion.Equals(targetVersion));

                Invoke(new Action(() =>
                {
                    skipLogoButton.Text = skipLogoButtonText;
                    skipLogoButton.Enabled = skipLogoEnabled;
                    installButton.Enabled = true;
                    removeButton.Enabled = true;
                    progressBar.Value = 0;
                    downloadLabel.Text = "";
                }));
            }
            catch (Exception exception)
            {
                ShowMessageBox(
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    "처리되지 않은 예외가 발생했어요.",
                    "에러 내용:",
                    exception.ToString());
                CloseForm();
                return;
            }
        }

        private void skipLogoButton_Click(object sender, EventArgs e)
        {
            // Block further inputs.
            skipLogoButton.Enabled = false;
            installButton.Enabled = false;
            removeButton.Enabled = false;

            string logoDir = Path.Combine(targetDir, "EFGame", "Movies");
            string logoFilePath = Path.Combine(logoDir, logoFileName);

            switch (skipLogoButton.Text)
            {
                case "로고 스킵 모드 설치":
                    if (Directory.Exists(logoDir) && File.Exists(logoFilePath))
                    {
                        if (File.Exists(logoFilePath + ".bk")) File.Delete(logoFilePath + ".bk");
                        File.Move(logoFilePath, logoFilePath + ".bk");
                    }
                    break;
                case "로고 스킵 모드 제거":
                    if (Directory.Exists(logoDir) && File.Exists(logoFilePath + ".bk"))
                    {
                        if (File.Exists(logoFilePath)) File.Delete(logoFilePath);
                        File.Move(logoFilePath + ".bk", logoFilePath);
                    }
                    break;
            }

            ShowMessageBox(
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                "작업이 성공적으로 완료되었어요!",
                "문제 발생 시 스팀 게임 파일 무결성 검사를 사용해주세요.");
            CloseForm();
            return;
        }

        private void installButton_Click(object sender, EventArgs e)
        {
            // Block further inputs.
            skipLogoButton.Enabled = false;
            installButton.Enabled = false;
            removeButton.Enabled = false;

            // Start the background worker to install the korean patch.
            installWorker.RunWorkerAsync();
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            // Block further inputs.
            skipLogoButton.Enabled = false;
            installButton.Enabled = false;
            removeButton.Enabled = false;

            // Start the background worker to remove the korean patch.
            removeWorker.RunWorkerAsync();
        }

        private void DownloadWork(string[] patchFiles, bool isRemove = false)
        {
            try
            {
                // Clear cache.
                ClearCache();

                // Download all files.
                UpdateStatusLabel($"파일 다운로드 중... {serverVersion}");

                foreach (string patchFile in patchFiles)
                {
                    DownloadFile($"{serverUrl}/{(isRemove ? "orig." : "")}{patchFile}", patchFile, installWorker, Path.Combine(Application.CommonAppDataPath, patchFile));
                }

                UpdateStatusLabel($"파일 설치 중... {serverVersion}");
                Invoke(new Action(() =>
                {
                    progressBar.Value = 0;
                }));

                foreach (string patchFile in patchFiles)
                {
                    File.Copy(Path.Combine(Application.CommonAppDataPath, patchFile), Path.Combine(targetDir, "EFGame", patchFile), true);
                }

                ShowMessageBox(
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    $"{(isRemove ? "제거" : "설치")}가 성공적으로 완료되었어요!");
                CloseForm();
                return;
            }
            catch (Exception exception)
            {
                ShowMessageBox(
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    "처리되지 않은 예외가 발생했어요.",
                    "에러 내용:",
                    exception.ToString());
                CloseForm();
                return;
            }
        }

        private void installWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DownloadWork(patchFiles);
        }

        private void removeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DownloadWork(patchFiles, true);
        }

        #endregion
    }

    // Extending HTTP client stream to report download progress while copying.
    public static class StreamExtensions
    {
        // Extending CopyToAsync to accept interface that reports an integer progress.
        public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, long totalLength, IProgress<int> progress)
        {
            // Check parameters.
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!source.CanRead) throw new ArgumentException("Has to be readable.", nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (!destination.CanWrite) throw new ArgumentException("Has to be writable.", nameof(destination));
            if (bufferSize < 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

            // Make a buffer with given buffer size.
            byte[] buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            int progressReport = 0;

            // Fill buffer.
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0)
            {
                // Write buffer to destination.
                await destination.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);

                // Up the total counter.
                totalBytesRead += bytesRead;
                int newProgressReport = (int)(totalBytesRead * 100 / totalLength);

                // Only report if progress became higher.
                if (newProgressReport > progressReport)
                {
                    // Report the progress.
                    progressReport = newProgressReport;
                    progress.Report(progressReport);
                }
            }
        }
    }
}
