using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;

namespace ProgramPackager
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string programDir = Path.Combine(curDir, "program");

            string patchPath = Path.Combine(programDir, "LostArkKoreanPatch.exe");
            string patchGzPath = Path.Combine(programDir, "LostArkKoreanPatchexe");

            string patcherPath = Path.Combine(programDir, "LostArkKoreanPatcher.exe");
            string patcherGzPath = Path.Combine(programDir, "LostArkKoreanPatcherexe");

            string updaterPath = Path.Combine(programDir, "LostArkKoreanUpdater.exe");
            string updaterGzPath = Path.Combine(programDir, "LostArkKoreanUpdaterexe");

            string outputDir = Path.Combine(curDir, "output");

            string programOutputDir = Path.Combine(outputDir, "programOutput");
            string programOutputPath = Path.Combine(outputDir, "program");

            string[] s = new string[]
            {
                patchPath, patchGzPath,
                patcherPath, patcherGzPath,
                updaterPath, updaterGzPath
            };

            for (int i = 0; i < s.Length; i += 2)
            {
                using (FileStream inStream = new FileStream(s[i], FileMode.Open))
                using (FileStream outStream = new FileStream(s[i + 1], FileMode.Create))
                using (GZipStream gzStream = new GZipStream(outStream, CompressionLevel.Optimal))
                {
                    inStream.CopyTo(gzStream);
                }
            }

            Directory.CreateDirectory(outputDir);
            Directory.CreateDirectory(programOutputDir);

            string[] filesToMove = new string[]
            {
                patchGzPath, patcherGzPath, updaterGzPath
            };

            for (int i = 0; i < filesToMove.Length; i++)
            {
                File.Move(filesToMove[i], Path.Combine(programOutputDir, Path.GetFileName(filesToMove[i])));
            }

            ZipFile.CreateFromDirectory(programOutputDir, programOutputPath);

            using (SHA1CryptoServiceProvider cryptoProvider = new SHA1CryptoServiceProvider())
            {
                File.WriteAllText(
                    $"{programOutputPath}.sha1",
                    BitConverter.ToString(cryptoProvider.ComputeHash(File.ReadAllBytes(programOutputPath))).Replace("-", ""));
            }
        }
    }
}
