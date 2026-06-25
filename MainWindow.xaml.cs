using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Wpf;
using AutoUpdaterDotNET;
using System.Net.Http;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PdfToolbox
{
    public partial class MainWindow : Window
    {
        private Microsoft.Web.WebView2.Core.CoreWebView2Environment _env;
        private const string CurrentVersion = "v1.0.0";

        private string _caminhoPdfAtual
        {
            get
            {
                if (TabPdfs.SelectedItem is TabItem selectedTab && selectedTab.Tag is string caminho)
                {
                    return caminho;
                }
                return string.Empty;
            }
            set
            {
                if (TabPdfs.SelectedItem is TabItem selectedTab)
                {
                    selectedTab.Tag = value;
                    selectedTab.ToolTip = value;
                    if (selectedTab.Content is WebView2 webView)
                    {
                        webView.Source = new Uri(value);
                    }
                    if (selectedTab.Header is StackPanel sp && sp.Children.Count > 0 && sp.Children[0] is TextBlock tb)
                    {
                        tb.Text = Path.GetFileName(value);
                    }
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
            
            // Inicia a verificação de atualizações no GitHub
            AutoUpdater.Start("https://raw.githubusercontent.com/fernandoc-souza/UPDF/main/update.xml");
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InicializarAmbienteWebViewAsync();
            _ = CheckForUpdatesAsync();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "UPDF-Updater");
                    string url = "https://api.github.com/repos/fernandoc-souza/UPDF/releases/latest";
                    
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            if (doc.RootElement.TryGetProperty("tag_name", out JsonElement tagElement))
                            {
                                string latestVersion = tagElement.GetString();
                                if (!string.IsNullOrEmpty(latestVersion) && latestVersion != CurrentVersion)
                                {
                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                        MessageBoxResult result = MessageBox.Show(
                                            $"Uma nova versão do UPDF ({latestVersion}) está disponível no GitHub!\nSua versão atual é {CurrentVersion}.\n\nDeseja abrir a página para baixar a atualização agora?",
                                            "Nova Atualização Disponível!",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Information);

                                        if (result == MessageBoxResult.Yes)
                                        {
                                            string releaseUrl = "https://github.com/fernandoc-souza/UPDF/releases/latest";
                                            Process.Start(new ProcessStartInfo(releaseUrl) { UseShellExecute = true });
                                        }
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
            Environment.Exit(0);
        }

        private async void InicializarAmbienteWebViewAsync()
        {
            try
            {
                var userDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UPDF");
                _env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, userDataFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao inicializar motor PDF: " + ex.Message);
            }
        }

        public async void AbrirDocumento(string caminho)
        {
            if (string.IsNullOrWhiteSpace(caminho)) return;
            caminho = caminho.Trim('\"', '\'');

            if (!File.Exists(caminho)) return;

            PnlPlaceholder.Visibility = Visibility.Hidden;
            TabPdfs.Visibility = Visibility.Visible;

            var tabItem = new TabItem();
            tabItem.Tag = caminho;
            tabItem.ToolTip = caminho;

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var titleText = new TextBlock { Text = Path.GetFileName(caminho), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            var closeButton = new Button { Content = "X", Padding = new Thickness(5, 0, 5, 0), Background = System.Windows.Media.Brushes.Transparent, BorderThickness = new Thickness(0) };
            
            closeButton.Click += (s, e) =>
            {
                if (tabItem.Content is WebView2 wv)
                {
                    wv.Dispose();
                }
                TabPdfs.Items.Remove(tabItem);
                if (TabPdfs.Items.Count == 0)
                {
                    PnlPlaceholder.Visibility = Visibility.Visible;
                    TabPdfs.Visibility = Visibility.Hidden;
                    AtualizarEstadoBotoes(false);
                    TxtStatus.Text = "Pronto";
                    TxtSidePanel.Text = "Nenhum documento aberto. Clique em 'Abrir' para carregar um PDF.";
                    TxtSidePanel.Foreground = System.Windows.Media.Brushes.Gray;
                }
            };

            headerPanel.Children.Add(titleText);
            headerPanel.Children.Add(closeButton);
            tabItem.Header = headerPanel;

            var webView = new WebView2 { Margin = new Thickness(0) };
            tabItem.Content = webView;
            TabPdfs.Items.Add(tabItem);
            TabPdfs.SelectedItem = tabItem;
            
            if (_env != null)
            {
                await webView.EnsureCoreWebView2Async(_env);
            }
            else
            {
                await webView.EnsureCoreWebView2Async();
            }
            
            webView.Source = new Uri(caminho);
            
            AtualizarEstadoBotoes(true);
        }

        private void TabPdfs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != TabPdfs) return;

            if (TabPdfs.SelectedItem is TabItem selectedTab && selectedTab.Tag is string caminho)
            {
                TxtStatus.Text = $"Arquivo aberto: {caminho}";
                TxtSidePanel.Text = "Documento carregado. Você já pode utilizar as ferramentas superiores para Assinar, Comprimir ou Exportar este arquivo.";
                TxtSidePanel.Foreground = System.Windows.Media.Brushes.Black;
                AtualizarEstadoBotoes(true);
            }
            else if (TabPdfs.Items.Count == 0)
            {
                AtualizarEstadoBotoes(false);
            }
        }

        private void AtualizarEstadoBotoes(bool habilitar)
        {
            BtnSignPdf.IsEnabled = habilitar;
            BtnCompressPdf.IsEnabled = habilitar;
            BtnExportWord.IsEnabled = habilitar;
            BtnExportExcel.IsEnabled = habilitar;
            BtnOrganizePages.IsEnabled = habilitar;
            BtnAddText.IsEnabled = habilitar;
            BtnAddImage.IsEnabled = habilitar;

            BtnSideSignPdf.IsEnabled = habilitar;
            BtnSideCompressPdf.IsEnabled = habilitar;
            BtnSideExportWord.IsEnabled = habilitar;
            BtnSideExportExcel.IsEnabled = habilitar;
            BtnSideOrganizePages.IsEnabled = habilitar;
            BtnSideAddText.IsEnabled = habilitar;
            BtnSideAddImage.IsEnabled = habilitar;
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            string aboutText = "União PDF FCS (UPDF)\n" +
                               "Versão 1.0\n\n" +
                               "Um sistema avançado para visualização, assinatura, compressão e organização de documentos PDF.\n\n" +
                               "Criador: Fernando CS\n\n" +
                               "Se o UPDF foi útil pra você faça uma doação pelo pix: 27999021489.";
                               
            MessageBox.Show(aboutText, "Sobre o UPDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnOpenPdf_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Arquivos PDF (*.pdf)|*.pdf",
                Title = "Selecione um ou mais arquivos PDF",
                Multiselect = true
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    AbrirDocumento(file);
                }
            }
        }

        private void BtnSignPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_caminhoPdfAtual))
                {
                    MessageBox.Show("Por favor, abra um documento PDF primeiro.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 0. Escolher local visual
                SignaturePlacementWindow placementWin = new SignaturePlacementWindow(_caminhoPdfAtual);
                placementWin.Owner = this;
                if (placementWin.ShowDialog() != true) return; // cancelou

                Rect sigRect = placementWin.SelectedRect;
                int sigPage = placementWin.PageNumber;

                // 1. Abrir a loja de certificados
                System.Security.Cryptography.X509Certificates.X509Store store = new System.Security.Cryptography.X509Certificates.X509Store(System.Security.Cryptography.X509Certificates.StoreName.My, System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser);
                store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadOnly);

                // 2. Selecionar certificado
                System.Security.Cryptography.X509Certificates.X509Certificate2Collection sel = System.Security.Cryptography.X509Certificates.X509Certificate2UI.SelectFromCollection(
                    store.Certificates,
                    "Assinatura Digital",
                    "Selecione o certificado para assinar o documento",
                    System.Security.Cryptography.X509Certificates.X509SelectionFlag.SingleSelection);

                if (sel.Count == 0) return; // Usuário cancelou

                System.Security.Cryptography.X509Certificates.X509Certificate2 cert = sel[0];

                // 3. Escolher o arquivo de destino
                string dirOriginal = System.IO.Path.GetDirectoryName(_caminhoPdfAtual);
                string nomeOriginal = System.IO.Path.GetFileNameWithoutExtension(_caminhoPdfAtual);
                string novoNome = $"{nomeOriginal}_assinado.pdf";

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    InitialDirectory = dirOriginal,
                    FileName = novoNome,
                    Filter = "Arquivos PDF (*.pdf)|*.pdf",
                    Title = "Salvar PDF Assinado"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string dest = saveFileDialog.FileName;

                    // 4. Converter certificado para iText7 BC
                    Org.BouncyCastle.X509.X509CertificateParser parser = new Org.BouncyCastle.X509.X509CertificateParser();
                    Org.BouncyCastle.X509.X509Certificate bcCert = parser.ReadCertificate(cert.RawData);
                    iText.Commons.Bouncycastle.Cert.IX509Certificate[] chain = new iText.Commons.Bouncycastle.Cert.IX509Certificate[1];
                    chain[0] = new iText.Bouncycastle.X509.X509CertificateBC(bcCert);

                    // 5. Assinar
                    using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(_caminhoPdfAtual))
                    using (System.IO.FileStream fs = new System.IO.FileStream(dest, System.IO.FileMode.Create))
                    {
                        iText.Signatures.PdfSigner signer = new iText.Signatures.PdfSigner(reader, fs, new iText.Kernel.Pdf.StampingProperties());
                        
                        iText.Kernel.Pdf.PdfDocument pdfDoc = signer.GetDocument();
                        iText.Kernel.Geom.Rectangle pageSize = pdfDoc.GetPage(sigPage).GetPageSize();
                        
                        float pdfWidth = pageSize.GetWidth();
                        float pdfHeight = pageSize.GetHeight();
                        
                        float scaleX = pdfWidth / (float)placementWin.CanvasWidth;
                        float scaleY = pdfHeight / (float)placementWin.CanvasHeight;
                        
                        float rectX = (float)sigRect.X * scaleX;
                        float rectY = (float)sigRect.Y * scaleY;
                        float rectW = (float)sigRect.Width * scaleX;
                        float rectH = (float)sigRect.Height * scaleY;

                        signer.SetPageRect(new iText.Kernel.Geom.Rectangle(rectX, rectY, rectW, rectH));
                        signer.SetPageNumber(sigPage);
                        iText.Signatures.PdfSignatureAppearance appearance = signer.GetSignatureAppearance();
                        
                        appearance.SetReason("Assinatura Digital");
                        appearance.SetRenderingMode(iText.Signatures.PdfSignatureAppearance.RenderingMode.NAME_AND_DESCRIPTION);
                        
                        string nomeSignatario = cert.GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType.SimpleName, false);
                        string dataAssinatura = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss zzz");
                        appearance.SetLayer2Text($"Assinado de forma digital por {nomeSignatario}\nDados: {dataAssinatura}");

                        iText.Signatures.IExternalSignature pks = new CustomX509Certificate2Signature(cert, "SHA-256");

                        signer.SignDetached(pks, chain, null, null, null, 0, iText.Signatures.PdfSigner.CryptoStandard.CMS);
                    }

                    MessageBox.Show("Documento assinado com sucesso!\n\nSalvo em: " + dest, "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Opcional: carregar o novo arquivo
                    _caminhoPdfAtual = dest;
                    TxtStatus.Text = $"Arquivo aberto: {_caminhoPdfAtual}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao assinar: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void BtnCompressPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_caminhoPdfAtual))
                {
                    MessageBox.Show("Por favor, abra um documento PDF primeiro.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string dirOriginal = System.IO.Path.GetDirectoryName(_caminhoPdfAtual);
                string nomeOriginal = System.IO.Path.GetFileNameWithoutExtension(_caminhoPdfAtual);
                string novoNome = $"{nomeOriginal}_comprimido.pdf";

                CompressionLevelWindow levelWin = new CompressionLevelWindow();
                levelWin.Owner = this;
                if (levelWin.ShowDialog() != true) return;
                
                int selectedLevel = levelWin.SelectedLevel;

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    InitialDirectory = dirOriginal,
                    FileName = novoNome,
                    Filter = "Arquivos PDF (*.pdf)|*.pdf",
                    Title = "Salvar PDF Comprimido"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string dest = saveFileDialog.FileName;
                    
                    System.IO.FileInfo fileOriginal = new System.IO.FileInfo(_caminhoPdfAtual);
                    long tamanhoOriginal = fileOriginal.Length;

                    iText.Kernel.Pdf.WriterProperties wp = new iText.Kernel.Pdf.WriterProperties()
                        .SetCompressionLevel(iText.Kernel.Pdf.CompressionConstants.BEST_COMPRESSION)
                        .SetFullCompressionMode(true);

                    using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(_caminhoPdfAtual))
                    using (iText.Kernel.Pdf.PdfWriter writer = new iText.Kernel.Pdf.PdfWriter(dest, wp))
                    using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader, writer))
                    {
                        if (selectedLevel > 1)
                        {
                            CompressImagesInPdf(pdfDoc, selectedLevel);
                        }
                        pdfDoc.Close();
                    }

                    System.IO.FileInfo fileNovo = new System.IO.FileInfo(dest);
                    long tamanhoNovo = fileNovo.Length;

                    double economia = (tamanhoOriginal - tamanhoNovo) / 1024.0 / 1024.0;
                    double porcentagem = ((double)(tamanhoOriginal - tamanhoNovo) / (double)tamanhoOriginal) * 100.0;

                    if (economia > 0)
                    {
                        MessageBox.Show($"Documento comprimido com sucesso!\n\nSalvo em: {dest}\n\nRedução de tamanho: {economia:F2} MB ({porcentagem:F1}%)", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"O PDF já estava otimizado. Nenhuma compressão adicional foi possível.\n\nArquivo salvo em: {dest}", "Compressão Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    // Carrega o novo arquivo comprimido
                    _caminhoPdfAtual = dest;
                    TxtStatus.Text = $"Arquivo aberto: {_caminhoPdfAtual}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao comprimir: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportWord_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_caminhoPdfAtual))
            {
                MessageBox.Show("Por favor, abra um PDF primeiro.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string dirOriginal = System.IO.Path.GetDirectoryName(_caminhoPdfAtual);
                string nomeOriginal = System.IO.Path.GetFileNameWithoutExtension(_caminhoPdfAtual);

                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    InitialDirectory = dirOriginal,
                    FileName = $"{nomeOriginal}_exportado.docx",
                    Filter = "Documento Word (*.docx)|*.docx",
                    Title = "Salvar Exportação do Word"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    WordExporter.ExportToWord(_caminhoPdfAtual, saveFileDialog.FileName);
                    MessageBox.Show("Exportação para Word concluída com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar para Word:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_caminhoPdfAtual))
            {
                MessageBox.Show("Por favor, abra um PDF primeiro.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string dirOriginal = System.IO.Path.GetDirectoryName(_caminhoPdfAtual);
                string nomeOriginal = System.IO.Path.GetFileNameWithoutExtension(_caminhoPdfAtual);

                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    InitialDirectory = dirOriginal,
                    FileName = $"{nomeOriginal}_tabelas.xlsx",
                    Filter = "Planilha Excel (*.xlsx)|*.xlsx",
                    Title = "Salvar Exportação do Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExcelExporter.ExportToExcel(_caminhoPdfAtual, saveFileDialog.FileName);
                    MessageBox.Show("Exportação para Excel concluída com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar para Excel:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOrganizePages_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_caminhoPdfAtual))
            {
                MessageBox.Show("Por favor, selecione um PDF primeiro.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OrganizePagesWindow organizeWindow = new OrganizePagesWindow(_caminhoPdfAtual)
            {
                Owner = this
            };

            if (organizeWindow.ShowDialog() == true)
            {
                TxtStatus.Text = "Páginas reorganizadas com sucesso.";
            }
        }

        private void BtnAddText_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_caminhoPdfAtual)) return;

            AddTextWindow textWindow = new AddTextWindow { Owner = this };
            if (textWindow.ShowDialog() == true)
            {
                VisualAnnotationWindow visualWindow = new VisualAnnotationWindow(_caminhoPdfAtual)
                {
                    Owner = this,
                    IsTextAnnotation = true,
                    TextContent = textWindow.TextContent,
                    FontName = textWindow.FontName,
                    FontSize = textWindow.FontSize,
                    TextColor = textWindow.TextColor
                };

                if (visualWindow.ShowDialog() == true && !string.IsNullOrEmpty(visualWindow.SavedFilePath))
                {
                    _caminhoPdfAtual = visualWindow.SavedFilePath;
                    AbrirDocumento(_caminhoPdfAtual);
                    TxtStatus.Text = "Texto adicionado e arquivo salvo com sucesso.";
                }
            }
        }

        private void BtnAddImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_caminhoPdfAtual)) return;

            AddImageWindow imgWindow = new AddImageWindow { Owner = this };
            if (imgWindow.ShowDialog() == true)
            {
                VisualAnnotationWindow visualWindow = new VisualAnnotationWindow(_caminhoPdfAtual)
                {
                    Owner = this,
                    IsTextAnnotation = false,
                    ImagePath = imgWindow.ImagePath
                };

                if (visualWindow.ShowDialog() == true && !string.IsNullOrEmpty(visualWindow.SavedFilePath))
                {
                    _caminhoPdfAtual = visualWindow.SavedFilePath;
                    AbrirDocumento(_caminhoPdfAtual);
                    TxtStatus.Text = "Imagem adicionada e arquivo salvo com sucesso.";
                }
            }
        }

        private string GetUniqueFilePath(string originalPath, string suffix)
        {
            string folder = System.IO.Path.GetDirectoryName(originalPath) ?? string.Empty;
            string fileName = System.IO.Path.GetFileNameWithoutExtension(originalPath);
            string newName = $"{fileName}{suffix}.pdf";
            string newPath = System.IO.Path.Combine(folder, newName);
            int counter = 1;
            while (System.IO.File.Exists(newPath))
            {
                newName = $"{fileName}{suffix} ({counter}).pdf";
                newPath = System.IO.Path.Combine(folder, newName);
                counter++;
            }
            return newPath;
        }

        private void CompressImagesInPdf(iText.Kernel.Pdf.PdfDocument pdfDoc, int level)
        {
            for (int i = 1; i <= pdfDoc.GetNumberOfPdfObjects(); i++)
            {
                iText.Kernel.Pdf.PdfObject obj = pdfDoc.GetPdfObject(i);
                if (obj != null && obj.IsStream())
                {
                    iText.Kernel.Pdf.PdfStream stream = (iText.Kernel.Pdf.PdfStream)obj;
                    if (iText.Kernel.Pdf.PdfName.Image.Equals(stream.GetAsName(iText.Kernel.Pdf.PdfName.Subtype)))
                    {
                        try
                        {
                            iText.Kernel.Pdf.Xobject.PdfImageXObject image = new iText.Kernel.Pdf.Xobject.PdfImageXObject(stream);
                            byte[] imgBytes = image.GetImageBytes();
                            
                            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(imgBytes))
                            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(ms))
                            {
                                int newWidth = bmp.Width;
                                int newHeight = bmp.Height;
                                
                                // Nível 3 reduz a resolução pela metade
                                if (level == 3 && (newWidth > 1000 || newHeight > 1000))
                                {
                                    newWidth /= 2;
                                    newHeight /= 2;
                                }

                                using (System.Drawing.Bitmap resized = new System.Drawing.Bitmap(bmp, new System.Drawing.Size(newWidth, newHeight)))
                                using (System.IO.MemoryStream outMs = new System.IO.MemoryStream())
                                {
                                    System.Drawing.Imaging.ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
                                    System.Drawing.Imaging.EncoderParameters encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
                                    
                                    // Nível 2 = 75%, Nível 3 = 30%
                                    long quality = level == 2 ? 75L : 30L;
                                    encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                                    
                                    resized.Save(outMs, jpegCodec, encoderParams);
                                    byte[] newBytes = outMs.ToArray();

                                    stream.SetData(newBytes);
                                    stream.Put(iText.Kernel.Pdf.PdfName.Filter, iText.Kernel.Pdf.PdfName.DCTDecode);
                                    stream.Remove(iText.Kernel.Pdf.PdfName.DecodeParms);
                                    stream.Put(iText.Kernel.Pdf.PdfName.Width, new iText.Kernel.Pdf.PdfNumber(newWidth));
                                    stream.Put(iText.Kernel.Pdf.PdfName.Height, new iText.Kernel.Pdf.PdfNumber(newHeight));
                                    stream.Put(iText.Kernel.Pdf.PdfName.ColorSpace, iText.Kernel.Pdf.PdfName.DeviceRGB);
                                    stream.Put(iText.Kernel.Pdf.PdfName.BitsPerComponent, new iText.Kernel.Pdf.PdfNumber(8));
                                    stream.Remove(iText.Kernel.Pdf.PdfName.SMask);
                                }
                            }
                        }
                        catch
                        {
                            // Ignora imagens que não podem ser lidas ou convertidas (ex: CMYK, Indexed)
                        }
                    }
                }
            }
        }

        private System.Drawing.Imaging.ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            System.Drawing.Imaging.ImageCodecInfo[] encoders = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            foreach (var encoder in encoders)
            {
                if (encoder.MimeType == mimeType)
                    return encoder;
            }
            return null;
        }
    }
}