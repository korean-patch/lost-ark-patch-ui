using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        // S3 server URL that hosts distributed patch files.
        private const string serverUrl = "https://lost-ark-korean-patch.s3.us-west-2.amazonaws.com";

        // Path for the main patch program.
        private string mainPath = string.Empty;
        private const string mainFileName = "LostArkKoreanPatch";
        private string mainTempPath = string.Empty;

        // Path for the patcher program.
        private string patcherPath = string.Empty;
        private const string patcherFileName = "LostArkKoreanPatcher";

        // Path for the updater program.
        private string updaterPath = string.Empty;
        private const string updaterFileName = "LostArkKoreanUpdater";

        // Directory for the distributed patch files.
        private string distribDir = string.Empty;

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

        // Name of the version file that denotes target game client version for the patch.
        private const string versionFileName = "LOSTARK.ver";

        // Map of the list of distributed patch file names, paired with their extensions.
        private Dictionary<string, string> distributedFileNames = new Dictionary<string, string>()
        {
            { "font", "lpk" },
            { "data2", "lpk" }
        };

        // Target client directory.
        private string targetDir = string.Empty;

        // Target client version.
        private string targetVersion = string.Empty;

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
        private void UpdateStatusLabel(string text)
        {
            Invoke(new Action(() =>
            {
                statusLabel.Text = text;
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

        // Get the SHA1 checksum from a file.
        private string ComputeSHA1(string filePath)
        {
            using (SHA1CryptoServiceProvider cryptoProvider = new SHA1CryptoServiceProvider())
            {
                return BitConverter.ToString(cryptoProvider.ComputeHash(File.ReadAllBytes(filePath))).Replace("-", "");
            }
        }

        // Downloads a file from given url while reporting progress on given background worker, then return the response as byte array.
        private byte[] DownloadFile(string url, string fileName, BackgroundWorker worker)
        {
            using (HttpClient client = new HttpClient())
            {
                // Default user agent and timeout values.
                client.DefaultRequestHeaders.Add("User-Agent", "request");
                client.Timeout = TimeSpan.FromMinutes(5);

                // Indicate what file we're downloading...
                UpdateDownloadLabel($"다운로드중: {fileName}");

                // Download the header first to look at the content length.
                using (HttpResponseMessage responseMessage = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
                {
                    if (responseMessage.Content.Headers.ContentLength != null)
                    {
                        long contentLength = (long)responseMessage.Content.Headers.ContentLength;

                        // Create a memory stream and feed it with the client stream.
                        using (MemoryStream ms = new MemoryStream())
                        using (Stream inStream = responseMessage.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                        {
                            // Create a progress reporter that reports the progress to the designated background worker.
                            Progress<int> p = new Progress<int>(new Action<int>((value) =>
                            {
                                worker.ReportProgress(value);
                            }));

                            // Buffer size is 1/10 of the total content.
                            // Grab data from http client stream and copy to memory stream.
                            inStream.CopyToAsync(ms, (int)(contentLength / 10), contentLength, p).GetAwaiter().GetResult();

                            // Empty the progress bar after download is complete.
                            worker.ReportProgress(0);

                            // Reset the label.
                            UpdateDownloadLabel("");

                            // Return the downloaded data as byte array.
                            return ms.ToArray();
                        }
                    }
                }
            }

            // If anything happened and didn't reach successful download, throw an exception.
            throw new Exception($"다음 파일을 다운로드하는 중 오류가 발생하였습니다. {url}");
        }

        // Downloads a file from given url while reporting progress on given background worker, then save it to disk.
        private void DownloadAndSaveFile(string url, string filePath, string fileName, BackgroundWorker worker)
        {
            using (HttpClient client = new HttpClient())
            {
                // Default user agent and timeout values.
                client.DefaultRequestHeaders.Add("User-Agent", "request");
                client.Timeout = TimeSpan.FromMinutes(5);

                // Indicate what file we're downloading...
                UpdateDownloadLabel($"다운로드중: {fileName}");

                // Download the header first to look at the content length.
                using (HttpResponseMessage responseMessage = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
                {
                    if (responseMessage.Content.Headers.ContentLength != null)
                    {
                        long contentLength = (long)responseMessage.Content.Headers.ContentLength;

                        // Create a memory stream and feed it with the client stream.
                        using (FileStream fs = new FileStream(filePath, FileMode.Create))
                        using (Stream inStream = responseMessage.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                        {
                            // Create a progress reporter that reports the progress to the designated background worker.
                            Progress<int> p = new Progress<int>(new Action<int>((value) =>
                            {
                                worker.ReportProgress(value);
                            }));

                            // Buffer size is 1/10 of the total content.
                            // Grab data from http client stream and copy to memory stream.
                            inStream.CopyToAsync(fs, (int)(contentLength / 10), contentLength, p).GetAwaiter().GetResult();

                            // Empty the progress bar after download is complete.
                            worker.ReportProgress(0);

                            // Reset the label.
                            UpdateDownloadLabel("");

                            return;
                        }
                    }
                }
            }

            // If anything happened and didn't reach successful download, throw an exception.
            throw new Exception($"다음 파일을 다운로드하는 중 오류가 발생하였습니다. {url}");
        }

        // Check SHA1 checksum between given file and server record and return true if they match.
        private bool CheckSHA1(string filePath, string url, string fileName, BackgroundWorker worker)
        {
            return ComputeSHA1(filePath) == Encoding.ASCII.GetString(DownloadFile(url, fileName, worker));
        }

        // Check SHA1 with existing file and only download if checksum is different.
        private void CheckAndDownload(string baseUrl, string fileName, string fileExtension, string targetFilePath, BackgroundWorker worker)
        {
            // If target file does not exist, download is always required.
            bool downloadRequired = !File.Exists(targetFilePath);

            // If target file exists, check SHA1 checksum and compare with the server.
            if (!downloadRequired)
            {
                // Compare SHA1 checksum.
                downloadRequired = !CheckSHA1(targetFilePath, $"{baseUrl}/{fileName}.sha1", $"{fileName}.sha1", worker);
            }

            // Do nothing if download is not required.
            if (!downloadRequired) return;

            // Download the file and save it.
            DownloadAndSaveFile($"{baseUrl}/{fileName}.{fileExtension}", targetFilePath, $"{fileName}.{fileExtension}", worker);
        }
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
