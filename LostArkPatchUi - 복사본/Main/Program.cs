using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace LostArkKoreanPatch
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LostArkKoreanPatch());
        }
    }
}
