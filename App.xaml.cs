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
        private const string UniqueEventName = "UPDF_App_SingleInstance_V2";
        private const string PipeName = "UPDF_Pipe_V2";
        private Mutex _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, UniqueEventName, out bool isOwned);

            if (!isOwned)
            {
                // Já existe uma instância. Enviar os arquivos via NamedPipe.
                if (e.Args.Length > 0)
                {
                    try
                    {
                        using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                        {
                            client.Connect(1000);
                            using (var writer = new StreamWriter(client))
                            {
                                foreach (var arg in e.Args)
                                {
                                    writer.WriteLine(arg);
                                }
                                writer.Flush();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Ignorar falha se não conseguir enviar
                    }
                }
                Current.Shutdown();
                return;
            }

            // Primeira instância, prosseguir normalmente.
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

            // Iniciar o servidor de Pipe para escutar outras instâncias
            Task.Run(() => StartPipeServer(mainWindow));
        }

        private void StartPipeServer(MainWindow mainWindow)
        {
            while (true)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                    {
                        server.WaitForConnection();
                        using (var reader = new StreamReader(server))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                string filePath = line.Trim('\"', '\'');
                                if (File.Exists(filePath) && Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Invocar no dispatcher da UI thread
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        mainWindow.AbrirDocumento(filePath);
                                        if (mainWindow.WindowState == WindowState.Minimized)
                                        {
                                            mainWindow.WindowState = WindowState.Normal;
                                        }
                                        mainWindow.Activate();
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Evitar que o loop quebre por causa de exceções
                    Thread.Sleep(100);
                }
            }
        }
    }
}
