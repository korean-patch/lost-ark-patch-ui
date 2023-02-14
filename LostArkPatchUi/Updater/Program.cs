using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace LostArkKoreanPatch
{
    internal class Program
    {
        // args[0] -> path for the main executable to update.
        // args[1] -> path for the main executable version file.
        // args[2] -> download url.
        // args[3] -> version string.
        static void Main(string[] args)
        {
            // Checking arguments...
            if (args.Length != 4) return;
            if (!File.Exists(args[0])) return;
            if (!Directory.Exists(Path.GetDirectoryName(args[1]))) return;

            // See if main process is still running, and wait for it to close.
            Console.WriteLine("LostArkKoreanPatch가 종료되기를 기다리는 중...");
            Process[] processes = Process.GetProcessesByName("LostArkKoreanPatch");

            while (processes.Length > 0)
            {
                // Try to kill main process.
                foreach (Process p in processes)
                {
                    p.Kill();
                }

                // Wait a bit, then refresh process list.
                Thread.Sleep(1000);
                processes = Process.GetProcessesByName("LostArkKoreanPatch");
            }

            Console.WriteLine();
            Console.WriteLine("LostArkKoreanPatch 업데이트 중...");

            byte[] file = DownloadFile(args[2]);

            if (file == null)
            {
                Console.WriteLine($"다운로드에 실패했습니다: {args[2]}");
                Console.WriteLine("ENTER 키를 눌러 프로그램을 종료해주세요.");
                Console.ReadLine();

                return;
            }

            File.WriteAllBytes(args[0], file);
            File.WriteAllText(args[1], args[3]);

            Process.Start(args[0]);
        }

        static byte[] DownloadFile(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "request");
                client.Timeout = TimeSpan.FromSeconds(30);

                HttpResponseMessage responseMessage = client.GetAsync(url).GetAwaiter().GetResult();

                if (responseMessage == null || responseMessage.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"업데이트에 실패했습니다: {url}");
                    Console.WriteLine("ENTER 키를 눌러 프로그램을 종료해주세요.");
                    Console.ReadLine();

                    return null;
                }

                return responseMessage.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            }
        }
    }
}
