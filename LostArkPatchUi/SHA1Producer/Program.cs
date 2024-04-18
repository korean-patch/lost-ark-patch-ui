using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace SHA1Producer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string mainPath = Path.Combine(curDir, "LostArkKoreanPatch.exe");

            using (SHA1CryptoServiceProvider cryptoProvider = new SHA1CryptoServiceProvider())
            {
                File.WriteAllText(
                    $"{mainPath}.sha1",
                    BitConverter.ToString(cryptoProvider.ComputeHash(File.ReadAllBytes(mainPath))).Replace("-", ""));
            }
        }
    }
}
