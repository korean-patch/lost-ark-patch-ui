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
            // Resolve the embedded "newtonsoft.json.dll".
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LostArkKoreanPatch());
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs e)
        {
            string assemblyName = new AssemblyName(e.Name).Name;

            // If looking for .resources.dll, pass.
            if (assemblyName.EndsWith(".resources")) return null;

            // Read from embedded resource file.
            using (Stream stream = Assembly.GetEntryAssembly().GetManifestResourceStream($"{typeof(Program).Namespace}.{assemblyName}.dll"))
            {
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                return Assembly.Load(data);
            }
        }
    }
}
