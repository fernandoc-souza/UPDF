using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;

namespace PdfToolbox
{
    public partial class App : Application
    {
        private const string MutexName = "UPDF_SingleInstance_Mutex_v1";
        private const string PipeName = "UPDF_SingleInstance_Pipe_v1";

        private Mutex _mutex;
        private MainWindow _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _mutex = new Mutex(true, MutexName, out bool isNewInstance);

            if (!isNewInstance)
            {
                // Já existe uma instância aberta: envia os arquivos para ela e encerra esta.
                EnviarParaInstanciaExistente(e.Args);
                Shutdown();
                return;
            }

            // Primeira instância: cria a janela e inicia o servidor de pipe.
            _mainWindow = new MainWindow();
            _mainWindow.Show();

            IniciarServidorPipe();

            AbrirArquivos(e.Args);
        }

        private void AbrirArquivos(string[] args)
        {
            foreach (var arg in args)
            {
                string path = arg.Trim('\"', '\'');
                if (File.Exists(path) && Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    _mainWindow.AbrirDocumento(path);
                }
            }
        }

        private void EnviarParaInstanciaExistente(string[] args)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(3000);
                    using (var writer = new StreamWriter(client) { AutoFlush = true })
                    {
                        foreach (var arg in args)
                        {
                            writer.WriteLine(arg.Trim('\"', '\''));
                        }
                    }
                }
            }
            catch
            {
                // Se não conseguir contatar a instância existente, ignora silenciosamente.
            }
        }

        private void IniciarServidorPipe()
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        using (var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1,
                            PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                        {
                            server.WaitForConnection();
                            using (var reader = new StreamReader(server))
                            {
                                string linha;
                                while ((linha = reader.ReadLine()) != null)
                                {
                                    string path = linha.Trim('\"', '\'');
                                    if (File.Exists(path) && Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            if (_mainWindow.WindowState == WindowState.Minimized)
                                                _mainWindow.WindowState = WindowState.Maximized;
                                            _mainWindow.Activate();
                                            _mainWindow.AbrirDocumento(path);
                                        });
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignora falhas pontuais e segue aguardando novas conexões.
                    }
                }
            })
            {
                IsBackground = true
            };
            thread.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { _mutex?.ReleaseMutex(); } catch { }
            try { _mutex?.Dispose(); } catch { }
            base.OnExit(e);
        }
    }
}
