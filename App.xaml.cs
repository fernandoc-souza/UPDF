using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PdfToolbox
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            if (e.Args.Length > 0)
            {
                foreach (var arg in e.Args)
                {
                    string path = arg.Trim('\"', '\'');
                    if (File.Exists(path) && Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        mainWindow.AbrirDocumento(path);
                    }
                }
            }
        }
    }
}
