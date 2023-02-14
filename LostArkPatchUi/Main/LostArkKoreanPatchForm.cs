using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace LostArkKoreanPatch
{
    public partial class LostArkKoreanPatchForm : Form
    {
        /* ***** WHEN UPDATING, DON'T FORGET TO UPDATE THIS IN ASSEMBLY INFO AND VER FILE!!! ***** */
        private string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        private string mainPath = Application.ExecutablePath;
        private string mainFileName = "LostArkKoreanPatch.exe";
        private string mainVersionFileName = "LostArkKoreanPatch.ver";

        private string[] gameProcessNames = new string[]
        {
            "lostark"
        };

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
            "EFGame/leveldata2.lpk"
        };

        private string githubReleaseApiUrl = "https://api.github.com/repos/korean-patch/lost-ark-patch-ui/releases/latest";
        private string distribUrl = "https://korean-patch.github.io/lost-ark-korean-patch/distrib";

        private string[] distribFiles = new string[]
        {
            "LOSTARK.ver",
            "font.lpk",
            "orig/font.lpk",
            "data2.lpk",
            "orig/data2.lpk"
        };

        private string distribDir = Path.Combine(Application.CommonAppDataPath, "distrib");

        private string targetDir = string.Empty;
        private string targetVersion = string.Empty;

        public LostArkKoreanPatchForm()
        {
            InitializeComponent();

            // Adjust the background to apply gradient effect.
            AdjustBackground();

            // Run the initial checker to verify and set up the environment.
            initialChecker.RunWorkerAsync();
        }

        private void AdjustBackground()
        {
            // Get the background image as Bitmap first.
            Bitmap origImage = (Bitmap)BackgroundImage;

            // Create a new image that will be used as a new background.
            // This should have the same width as the form, and the same width:height ratio.
            Bitmap newImage = new Bitmap(ClientSize.Width, ClientSize.Width * origImage.Height / origImage.Width);
            newImage.SetResolution(origImage.HorizontalResolution, origImage.VerticalResolution);

            // Starting drawing in the new image...
            using (Graphics g = Graphics.FromImage(newImage))
            {
                // First draw a linear gradient with the current form's back color, going transparent at bottom.
                Rectangle rect = new Rectangle(0, 0, newImage.Width, newImage.Height);

                // Now let's blend the original image...
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    g.DrawImage(origImage, rect, 0, 0, origImage.Width, origImage.Height, GraphicsUnit.Pixel, wrapMode);
                }

                LinearGradientBrush brush = new LinearGradientBrush(rect, Color.Transparent, BackColor, 90f);
                g.FillRectangle(brush, rect);
            }

            // Set the new image as background.
            BackgroundImage = newImage;
        }

        // Do some initial setup work before patch can be applied.
        private void initialChecker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Clean up the temp folder.
            if (Directory.Exists(distribDir)) Directory.Delete(distribDir, true);
            Directory.CreateDirectory(distribDir);

            // Wait until the UI is ready.
            while (!statusLabel.IsHandleCreated) { }

            Invoke(new Action(() =>
            {
                statusLabel.Text = "환경 체크 중...";
            }));

            // Check if the patch program is already running, and terminate if it is.
            if (Process.GetProcessesByName(mainFileName).Length > 1)
            {
                MessageBox.Show(
                    "로스트 아크 한글 패치 프로그램이 이미 실행중이에요.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.None,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);

                Invoke(new Action(() =>
                {
                    Close();
                }));

                return;
            }

            // Check if lost ark game process is running.
            if (Process.GetProcesses().Any(p => gameProcessNames.Contains(p.ProcessName.ToLower())))
            {
                MessageBox.Show(
                    "로스트 아크가 이미 실행중이에요." + Environment.NewLine + Environment.NewLine +
                    "로스트 아크를 종료한 후 한글 패치 프로그램을 다시 실행해주세요.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.None,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);

                Invoke(new Action(() =>
                {
                    Close();
                }));

                return;
            }

            // Check main executable's version.
            Invoke(new Action(() =>
            {
                statusLabel.Text = "프로그램 버전 확인 중...";
            }));

            try
            {
                // Get the JSON information about the latest release.
                byte[] latestRelease = DownloadFile(githubReleaseApiUrl);
                if (latestRelease == null) throw new Exception();
                JObject releaseObject = JObject.Parse(Encoding.UTF8.GetString(latestRelease));

                // Get the asset information for the executables.
                JObject[] assetsArray = ((JArray)releaseObject.GetValue("assets")).Select(asset => (JObject)asset).ToArray();

                // Get the version asset from array.
                JObject mainVersionAsset = FindAssetByName(assetsArray, mainVersionFileName);

                // Download the version info for the latest release.
                string latestVersion = Encoding.UTF8.GetString(DownloadAsset(mainVersionAsset));
                
                // This executable is not latest...
                if (!version.Equals(latestVersion))
                {
                    MessageBox.Show(
                        "패치 프로그램 업데이트가 필요합니다." + Environment.NewLine + Environment.NewLine +
                        "최신 패치 프로그램을 다운로드받은 후 다시 실행해주세요.",
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.None,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.DefaultDesktopOnly);

                    Invoke(new Action(() =>
                    {
                        Close();
                    }));

                    return;
                }
            }
            catch
            {
                MessageBox.Show(
                    "버전을 확인하는데 실패했어요." + Environment.NewLine + Environment.NewLine +
                    "문제가 지속될 경우 디스코드를 통해 문의해주세요.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.None,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);

                Invoke(new Action(() =>
                {
                    Close();
                }));

                return;
            }

            Invoke(new Action(() =>
            {
                statusLabel.Text = "로스트 아크 클라이언트를 찾는 중...";
            }));

            // Check Windows registry uninstall list to find the lost ark installation.
            string[] uninstallKeyNames = new string[]
            {
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
                "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
            };

            string[] uninstallSteamKeyNames = new string[]
            {
                $"{uninstallKeyNames[0]}\\Steam App 1599340",
                $"{uninstallKeyNames[1]}\\Steam App 1599340"
            };

            // Check steam registry...
            foreach (string uninstallSteamKeyName in uninstallSteamKeyNames)
            {
                using (RegistryKey uninstallKey = Registry.LocalMachine.OpenSubKey(uninstallSteamKeyName))
                {
                    if (uninstallKey == null) continue;

                    object installLocation = uninstallKey.GetValue("InstallLocation");
                    if (installLocation == null) continue;

                    targetDir = CheckTargetDir(Path.GetFullPath(installLocation.ToString()));
                    break;
                }
            }

            // If the installation location is found, ask user to confirm.
            bool isTargetDirVerified = false;

            if (!string.IsNullOrEmpty(targetDir))
            {
                isTargetDirVerified = MessageBox.Show(
                    "다음 위치에서 로스트 아크 클라이언트가 발견되었어요." + Environment.NewLine + Environment.NewLine +
                    targetDir + Environment.NewLine + Environment.NewLine +
                    "이 클라이언트에 한글 패치를 설치할까요?",
                    Text,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.None,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly) == DialogResult.Yes;
            }

            // If the target directory is not verified, let user choose.
            if (!isTargetDirVerified)
            {
                Invoke(new Action(() =>
                {
                    MessageBox.Show(
                        "로스트 아크 클라이언트가 설치된 장소에서 LOSTARK.exe 파일을 찾아 선택해주세요." + Environment.NewLine + Environment.NewLine +
                        "(보통 Binaries/Win64 폴더 내부에 있어요.)",
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.None,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.DefaultDesktopOnly);

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
                        targetDir = CheckTargetDir(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(dialog.FileName), "..", "..")));

                        if (string.IsNullOrEmpty(targetDir))
                        {
                            MessageBox.Show(
                                "선택하신 경로가 올바르지 않아요.",
                                Text,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.None,
                                MessageBoxDefaultButton.Button1,
                                MessageBoxOptions.DefaultDesktopOnly);
                            Close();
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            "선택하신 경로가 올바르지 않아요.",
                            Text,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.None,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.DefaultDesktopOnly);
                        Close();
                    }
                }));
            }

            // If target directory is still invalid, quit.
            if (string.IsNullOrEmpty(targetDir))
            {
                Invoke(new Action(() =>
                {
                    Close();
                }));

                return;
            }

            // Check the target client version.
            Invoke(new Action(() =>
            {
                statusLabel.Text = "클라이언트 버전 체크 중...";
            }));

            targetVersion = File.ReadAllText(Path.Combine(targetDir, "Binaries", "misc", distribFiles[0]));

            // Retrieve the korean patch based on the client version.
            Invoke(new Action(() =>
            {
                statusLabel.Text = $"서버에서 한글 패치 가져오는 중... 버전 {targetVersion}";
            }));

            // Download patch files for the detected client version.
            foreach (string distribFile in distribFiles)
            {
                string url = $"{distribUrl}/{targetVersion}/{distribFile}";
                byte[] file = DownloadFile(url);

                if (file == null)
                {
                    MessageBox.Show(
                        "다음 파일을 다운로드하는데 실패했어요." + Environment.NewLine + Environment.NewLine +
                        url + Environment.NewLine + Environment.NewLine +
                        "문제가 지속되면 디스코드를 통해 문의해주세요.",
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.None,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.DefaultDesktopOnly);

                    Invoke(new Action(() =>
                    {
                        Close();
                    }));

                    return;
                }
                else
                {
                    string filePath = Path.Combine(distribDir, distribFile);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    File.WriteAllBytes(filePath, file);
                }
            }

            // All done!
            Invoke(new Action(() =>
            {
                statusLabel.Text = $"버전 {targetVersion}";
                installButton.Enabled = true;
                removeButton.Enabled = true;
            }));
        }

        // Downloads a file from given url and return it as a byte array.
        private byte[] DownloadFile(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                // Github request header.
                client.DefaultRequestHeaders.Add("User-Agent", "request");
                client.Timeout = TimeSpan.FromSeconds(30);

                HttpResponseMessage responseMessage = client.GetAsync(url).GetAwaiter().GetResult();

                // Do a quick status check and silently return null if something failed.
                if (responseMessage == null || responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    return null;
                }

                return responseMessage.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            }
        }

        // Find the asset from assets using asset name.
        private JObject FindAssetByName(JObject[] assetsArray, string assetName)
        {
            return assetsArray.First(asset => asset.GetValue("name").ToString() == assetName);
        }

        private string GetDownloadUrlFromAsset(JObject asset)
        {
            return asset.GetValue("browser_download_url").ToString();
        }

        // Download the asset.
        private byte[] DownloadAsset(JObject asset)
        {
            // Download the latest executable as byte array.
            byte[] executable = DownloadFile(GetDownloadUrlFromAsset(asset));
            if (executable == null) throw new Exception();

            return executable;
        }

        // Validates target directory.
        private string CheckTargetDir(string targetDir)
        {
            if (!Directory.Exists(targetDir)) return null;
            if (!requiredFiles.All(requiredFile => File.Exists(Path.Combine(targetDir, requiredFile)))) return null;

            return targetDir;
        }

        private void installButton_Click(object sender, EventArgs e)
        {
            installButton.Enabled = false;
            removeButton.Enabled = false;

            string[] patchFiles = new string[]
            {
                "font.lpk",
                "data2.lpk"
            };

            try
            {
                foreach (string patchFile in patchFiles)
                {
                    File.Copy(Path.Combine(distribDir, patchFile), Path.Combine(targetDir, "EFGame", patchFile), true);
                }
            }
            catch
            {
                MessageBox.Show(
                    "한글 패치 설치 중 오류가 발생했어요." + Environment.NewLine + Environment.NewLine +
                    "문제가 지속되면 디스코드를 통해 문의해주세요.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.None,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);
                
                Close();

                return;
            }

            MessageBox.Show(
                "한글 패치가 성공적으로 설치되었어요.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.None,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.DefaultDesktopOnly);

            Close();
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            installButton.Enabled = false;
            removeButton.Enabled = false;

            string[] patchFiles = new string[]
            {
                "font.lpk",
                "data2.lpk"
            };

            try
            {
                foreach (string patchFile in patchFiles)
                {
                    File.Copy(Path.Combine(distribDir, "orig", patchFile), Path.Combine(targetDir, "EFGame", patchFile), true);
                }
            }
            catch
            {
                MessageBox.Show(
                    "한글 패치 삭제 중 오류가 발생했어요." + Environment.NewLine + Environment.NewLine +
                    "문제가 지속되면 디스코드를 통해 문의해주세요.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.None,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);

                Close();

                return;
            }

            MessageBox.Show(
                "한글 패치가 성공적으로 삭제되었어요.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.None,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.DefaultDesktopOnly);

            Close();
        }
    }
}
