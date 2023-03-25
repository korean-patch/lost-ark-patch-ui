using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace ChecksumGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            GenerateChecksums(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        static void GenerateChecksums(string targetDir)
        {
            string[] files = Directory.GetFiles(targetDir);

            foreach (string file in files)
            {
                using (SHA1CryptoServiceProvider cryptoProvider = new SHA1CryptoServiceProvider())
                {
                    File.WriteAllText(
                        $"{Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file))}.sha1",
                        BitConverter.ToString(cryptoProvider.ComputeHash(File.ReadAllBytes(file))).Replace("-", ""));
                }
            }

            string[] dirs = Directory.GetDirectories(targetDir);

            foreach (string dir in dirs)
            {
                GenerateChecksums(dir);
            }
        }
    }
}
