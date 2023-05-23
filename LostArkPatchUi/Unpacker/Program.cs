using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace Unpacker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            
            string distribPath = Path.Combine(curDir, "distrib");
            string distribDir = Path.Combine(curDir, "distribOutput");
            if (Directory.Exists(distribDir)) Directory.Delete(distribDir, true);
            Directory.CreateDirectory(distribDir);

            if (File.Exists(Path.Combine(curDir, "data2.lpk"))) File.Delete(Path.Combine(curDir, "data2.lpk"));
            if (File.Exists(Path.Combine(curDir, "font.lpk"))) File.Delete(Path.Combine(curDir, "font.lpk"));

            if (!File.Exists(distribPath))
            {
                Console.WriteLine("distrib 파일을 발견하지 못했습니다.");
                Console.WriteLine("프로그램을 종료합니다.");

                return;
            }

            ZipFile.ExtractToDirectory(distribPath, distribDir);

            using (FileStream inStream = new FileStream(Path.Combine(distribDir, "data2lpk"), FileMode.Open))
            using (FileStream outStream = new FileStream(Path.Combine(curDir, "data2.lpk"), FileMode.Create))
            using (GZipStream gzStream = new GZipStream(inStream, CompressionMode.Decompress))
            {
                gzStream.CopyTo(outStream);
            }

            using (FileStream inStream = new FileStream(Path.Combine(distribDir, "fontlpk"), FileMode.Open))
            using (FileStream outStream = new FileStream(Path.Combine(curDir, "font.lpk"), FileMode.Create))
            using (GZipStream gzStream = new GZipStream(inStream, CompressionMode.Decompress))
            {
                gzStream.CopyTo(outStream);
            }

            Directory.Delete(distribDir, true);
        }
    }
}
