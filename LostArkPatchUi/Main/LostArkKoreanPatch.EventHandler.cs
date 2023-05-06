using System.ComponentModel;
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Win32;
using System.Collections.Generic;

namespace LostArkKoreanPatch.Main
{
    public partial class LostArkKoreanPatch : Form
    {
        // Shared event handler for background workers to show progress using progress bar.
        private void progressChanged(object sender, ProgressChangedEventArgs e)
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

                // Check if any worker processes are already running and kill them if they are.
                foreach (Process p in Process.GetProcesses().Where(p => new string[] { patcherFileName, updaterFileName }.Contains(p.ProcessName)))
                {
                    p.Kill();
                    p.WaitForExit();
                }

                // Populate necessary paths.
                mainPath = Application.ExecutablePath;
                mainTempPath = Path.GetFullPath(Path.Combine(Application.CommonAppDataPath, $"{mainFileName}.exe"));
                patcherPath = Path.GetFullPath(Path.Combine(Application.CommonAppDataPath, $"{patcherFileName}.exe"));
                updaterPath = Path.GetFullPath(Path.Combine(Application.CommonAppDataPath, $"{updaterFileName}.exe"));

                programPath = Path.GetFullPath(Path.Combine(Application.CommonAppDataPath, "program"));
                programDir = Path.GetFullPath(Path.Combine(Application.CommonAppDataPath, "programOutput"));

                distribPath = Path.GetFullPath(Path.Combine(Application.CommonAppDataPath, "distrib"));
                distribDir = Path.GetFullPath(Path.Combine(Application.CommonAppDataPath, "distribOutput"));

                origPath = Path.GetFullPath(Path.Combine(Application.CommonAppDataPath, "orig"));
                origDir = Path.GetFullPath(Path.Combine(Application.CommonAppDataPath, "origOutput"));

                // Clean up some stuff.
                if (File.Exists(mainTempPath)) File.Delete(mainTempPath);
                if (Directory.Exists(programPath)) Directory.Delete(programPath, true);
                if (Directory.Exists(distribPath)) Directory.Delete(distribPath, true);
                if (Directory.Exists(origPath)) Directory.Delete(origPath, true);

                // Grab the necessary worker processes from the server.
                UpdateStatusLabel("필요한 프로그램 가져오는 중...");

                try
                {
                    // Check and download program.
                    string programUrl = Encoding.ASCII.GetString(DownloadFile($"{serverUrl}/program", "program", initialChecker));
                    bool downloadRequired = CheckIfDownloadRequired($"{programUrl}.sha1", "program.sha1", programPath, initialChecker);

                    if (downloadRequired)
                    {
                        DownloadAndSaveFile(programUrl, programPath, "program", initialChecker);

                        if (Directory.Exists(programDir)) Directory.Delete(programDir, true);
                        Directory.CreateDirectory(programDir);
                        ExtractToFolder(programPath, programDir);

                        UncompressFile(Path.Combine(programDir, $"{mainFileName}exe"), Path.Combine(programDir, $"{mainFileName}.exe"));
                        UncompressFile(Path.Combine(programDir, $"{patcherFileName}exe"), Path.Combine(programDir, $"{patcherFileName}.exe"));
                        UncompressFile(Path.Combine(programDir, $"{updaterFileName}exe"), Path.Combine(programDir, $"{updaterFileName}.exe"));

                        if (File.Exists(mainTempPath)) File.Delete(mainTempPath);
                        File.Move(Path.Combine(programDir, $"{mainFileName}.exe"), mainTempPath);
                        if (File.Exists(patcherPath)) File.Delete(patcherPath);
                        File.Move(Path.Combine(programDir, $"{patcherFileName}.exe"), patcherPath);
                        if (File.Exists(updaterPath)) File.Delete(updaterPath);
                        File.Move(Path.Combine(programDir, $"{updaterFileName}.exe"), updaterPath);
                    }
                }
                catch (Exception exception)
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "필요한 프로그램들을 가져오는데 실패했어요.",
                        "문제가 지속될 경우 디스코드를 통해 문의해주세요.",
                        "에러 내용:",
                        exception.ToString());
                    CloseForm();
                    return;
                }

                // Check main executable's version and update if necessary.
                UpdateStatusLabel("프로그램 버전 확인 중...");

                try
                {
                    // If main executable is in the temp path, it means it needs to be updated.
                    if (File.Exists(mainTempPath))
                    {
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
                }
                catch (Exception exception)
                {
                    ShowMessageBox(
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        "버전을 확인하는데 실패했어요.",
                        "문제가 지속될 경우 디스코드를 통해 문의해주세요.",
                        "에러 내용:",
                        exception.ToString());
                    CloseForm();
                    return;
                }

                // Try to detect lost ark client path.
                UpdateStatusLabel("로스트 아크 클라이언트를 찾는 중...");

                try
                {
                    // Check Windows registry uninstall list to find the lost ark installation.
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
                            using (RegistryKey uninstallKey = localMachine.OpenSubKey(uninstallSteamKeyName))
                            {
                                if (uninstallKey == null) continue;

                                object installLocation = uninstallKey.GetValue("InstallLocation");
                                if (installLocation == null) continue;

                                targetDir = CheckTargetDir(Path.GetFullPath(installLocation.ToString()));
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    // Any exception happened during detection, just set directory to not found so user can select it.
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

                            if (string.IsNullOrEmpty(targetDir))
                            {
                                ShowMessageBox(
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error,
                                    "선택하신 경로가 올바르지 않아요.");
                                CloseForm();
                                return;
                            }
                        }
                        else
                        {
                            ShowMessageBox(
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error,
                                "선택하신 경로가 올바르지 않아요.");
                            CloseForm();
                            return;
                        }
                    }));
                }

                // If target directory is still invalid, quit.
                if (string.IsNullOrEmpty(targetDir))
                {
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
                    string serverVersion = Encoding.ASCII.GetString(DownloadFile($"{serverUrl}/{versionFileName}", versionFileName, initialChecker));

                    if (!serverVersion.Equals(targetVersion))
                    {
                        ShowMessageBox(
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            "현재 설치된 게임 클라이언트의 버전이 서버의 버전과 달라요!",
                            $"클라이언트 버전: {targetVersion}, 서버 버전: {serverVersion}",
                            "문제가 지속되면 디스코드를 통해 문의해주세요.");
                        CloseForm();
                        return;
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

                // Check all done!
                UpdateStatusLabel($"버전 {targetVersion}");

                Invoke(new Action(() =>
                {
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

        private void installButton_Click(object sender, EventArgs e)
        {
            // Block further inputs.
            installButton.Enabled = false;
            removeButton.Enabled = false;

            // Start the background worker to install the korean patch...
            installWorker.RunWorkerAsync();
        }

        // Install the korean patch.
        private void installWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Check cached patch files and download from server only if SHA1 checksum is different.
                UpdateStatusLabel($"한글 패치 업데이트 확인 중... {targetVersion}");

                string distribUrl = Encoding.ASCII.GetString(DownloadFile($"{serverUrl}/distrib", "distrib", installWorker));
                bool downloadRequired = CheckIfDownloadRequired($"{distribUrl}.sha1", "distrib.sha1", distribPath, installWorker);

                if (downloadRequired)
                {
                    DownloadAndSaveFile(distribUrl, distribPath, "distrib", installWorker);

                    if (Directory.Exists(distribDir)) Directory.Delete(distribDir, true);
                    Directory.CreateDirectory(distribDir);
                    ExtractToFolder(distribPath, distribDir);

                    UncompressFile(Path.Combine(distribDir, "data2lpk"), Path.Combine(distribDir, "data2.lpk"));
                    UncompressFile(Path.Combine(distribDir, "fontlpk"), Path.Combine(distribDir, "font.lpk"));
                }

                UpdateStatusLabel($"한글 패치 설치 중... {targetVersion}");

                Invoke(new Action(() =>
                {
                    progressBar.Value = 0;
                }));

                // Run the child worker process with administrator access to copy the patch files.
                Process p = Process.Start(new ProcessStartInfo(patcherPath, $"0 \"{targetDir}\" \"{distribDir}\"")
                {
                    UseShellExecute = true,
                    Verb = "runas"
                });

                if (p != null)
                {
                    p.WaitForExit();
                }

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

        private void removeButton_Click(object sender, EventArgs e)
        {
            // Block further inputs.
            installButton.Enabled = false;
            removeButton.Enabled = false;

            // Start the background worker to remove the korean patch...
            removeWorker.RunWorkerAsync();
        }

        private void removeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Check cached patch files and download from server only if SHA1 checksum is different.
                UpdateStatusLabel($"한글 패치 업데이트 확인 중... {targetVersion}");

                string origUrl = Encoding.ASCII.GetString(DownloadFile($"{serverUrl}/orig", "orig", removeWorker));
                bool downloadRequired = CheckIfDownloadRequired($"{origUrl}.sha1", "orig.sha1", origPath, removeWorker);

                if (downloadRequired)
                {
                    DownloadAndSaveFile(origUrl, origPath, "orig", removeWorker);

                    if (Directory.Exists(origDir)) Directory.Delete(origDir, true);
                    Directory.CreateDirectory(origDir);
                    ExtractToFolder(origPath, origDir);

                    UncompressFile(Path.Combine(origDir, "data2lpk"), Path.Combine(origDir, "data2.lpk"));
                    UncompressFile(Path.Combine(origDir, "fontlpk"), Path.Combine(origDir, "font.lpk"));
                }

                UpdateStatusLabel($"한글 패치 삭제 중... {targetVersion}");

                Invoke(new Action(() =>
                {
                    progressBar.Value = 0;
                }));

                // Run the child worker process with administrator access to replace the patch files with original unpatched files.
                Process p = Process.Start(new ProcessStartInfo(patcherPath, $"1 \"{targetDir}\" \"{origDir}\"")
                {
                    UseShellExecute = true,
                    Verb = "runas"
                });

                if (p != null)
                {
                    p.WaitForExit();
                }

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
    }
}
