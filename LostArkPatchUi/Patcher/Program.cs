using System;
using System.IO;
using System.Threading;

namespace LostArkKoreanPatch.Patcher
{
    // Worker process for LostArkKoreanPatch that do stuff that may require administrator access.
    internal class Program
    {
        private static string targetDir = string.Empty;
        private static string distribDir = string.Empty;

        private static string[] patchFiles = new string[]
        {
            "font.lpk",
            "data2.lpk"
        };
        private static string[] restoreFiles = new string[]
        {
            "font.lpk",
            "data2.lpk"
        };

        // args[0] -> operation mode.
        //         -> 0 = install korean patch.
        //         -> 1 = remove korean patch and restore original.
        // args[1] -> path to the lost ark client.
        // args[2] -> path to the cached korean patch files.
        static void Main(string[] args)
        {
            // Check arguments.
            if (args.Length != 3) return;
            if (!Directory.Exists(args[1])) return;
            if (!Directory.Exists(args[2])) return;

            // Populate paths.
            targetDir = args[1];
            distribDir = args[2];

            switch (args[0])
            {
                case "0":
                    Install();
                    break;
                case "1":
                    Remove();
                    break;
            }

            Console.WriteLine("작업이 성공적으로 완료되었습니다!");
            Console.WriteLine("이 창은 5초 후 자동으로 닫힙니다.");
            Thread.Sleep(5000);
        }

        // This installs font and text patches.
        static void Install()
        {
            foreach (string patchFile in patchFiles)
            {
                File.Copy(Path.Combine(distribDir, patchFile), Path.Combine(targetDir, "EFGame", patchFile), true);
            }
        }

        // This removes the patch and restore the client to original.
        static void Remove()
        {
            foreach (string restoreFile in restoreFiles)
            {
                File.Copy(Path.Combine(distribDir, restoreFile), Path.Combine(targetDir, "EFGame", restoreFile), true);
            }
        }
    }
}
