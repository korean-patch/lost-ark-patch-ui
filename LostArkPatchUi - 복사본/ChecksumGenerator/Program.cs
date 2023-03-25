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
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            GenerateChecksums(currentDir);

            string[] dirs = Directory.GetDirectories(currentDir);

            foreach (string dir in dirs)
            {
                GenerateChecksums(dir);
            }
        }

        static void GenerateChecksums(string targetDirectory)
        {
            string[] files = Directory.GetFiles(targetDirectory);

            foreach (string file in files)
            {
                //if (Path.GetFileName(file) == Path.GetFileName(Assembly.GetExecutingAssembly().Location)) continue;

                using (SHA1CryptoServiceProvider cryptoProvider = new SHA1CryptoServiceProvider())
                {
                    File.WriteAllText(
                        $"{Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file))}.sha1",
                        BitConverter.ToString(cryptoProvider.ComputeHash(File.ReadAllBytes(file))).Replace("-", ""));
                }
            }
        }
    }
}
