using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Wpf;
using AutoUpdaterDotNET;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace PdfToolbox
{
    public partial class MainWindow : Window
    {
        private Microsoft.Web.WebView2.Core.CoreWebView2Environment _env;

        private static string CurrentVersion
        {
            get
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                return $"{v.Major}.{v.Minor}.{v.Build}";
            }
        }

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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InicializarAmbienteWebViewAsync();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                foreach (var item in TabPdfs.Items)
                {
                    if (item is TabItem tabItem && tabItem.Content is Microsoft.Web.WebView2.Wpf.WebView2 wv)
                    {
                        wv.Dispose();
                    }
                }
            }
            catch { }

            Application.Current.Shutdown();
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

            var webView = new WebView2 { Margin = new Thickness(0) };
            var tabItem = CriarAba(Path.GetFileName(caminho), caminho, webView, webView);
            tabItem.Tag = caminho;

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

            // Cliques em links dentro do PDF abrem em nova aba de navegador (não na aba do PDF)
            webView.CoreWebView2.NewWindowRequested += (s, args) =>
            {
                args.Handled = true;
                string u = args.Uri;
                Dispatcher.InvokeAsync(() => CriarAbaNavegador(u));
            };
            webView.CoreWebView2.NavigationStarting += (s, args) =>
            {
                string u = args.Uri ?? string.Empty;
                if (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    args.Cancel = true; // não navega a aba do PDF
                    Dispatcher.InvokeAsync(() => CriarAbaNavegador(u));
                }
            };

            webView.Source = new Uri(caminho);

            AtualizarEstadoBotoes(true);
        }

        // Cria uma aba com cabeçalho (título + botão fechar) reutilizável para PDF e navegador.
        private TabItem CriarAba(string titulo, string tooltip, UIElement conteudo, WebView2 webViewDispose)
        {
            var tabItem = new TabItem { ToolTip = tooltip };

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var titleText = new TextBlock { Text = titulo, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0), MaxWidth = 150, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = titulo };
            var closeButton = new Button { Content = "X", Padding = new Thickness(5, 0, 5, 0), Background = System.Windows.Media.Brushes.Transparent, BorderThickness = new Thickness(0) };

            closeButton.Click += (s, e) =>
            {
                try { webViewDispose?.Dispose(); } catch { }
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
            tabItem.Content = conteudo;
            return tabItem;
        }

        private Button CriarBotaoNav(string conteudo, string tooltip)
        {
            return new Button
            {
                Content = conteudo,
                ToolTip = tooltip,
                Width = 32,
                Height = 26,
                Margin = new Thickness(2, 4, 2, 4),
                FontSize = 14,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
        }

        // Abre uma página web em nova aba, com barra de navegação (voltar/avançar/recarregar/URL).
        private async void CriarAbaNavegador(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            PnlPlaceholder.Visibility = Visibility.Hidden;
            TabPdfs.Visibility = Visibility.Visible;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var barra = new DockPanel
            {
                LastChildFill = true,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF0, 0xF0, 0xF0))
            };
            Grid.SetRow(barra, 0);

            var btnBack = CriarBotaoNav("◀", "Voltar");
            var btnFwd = CriarBotaoNav("▶", "Avançar");
            var btnReload = CriarBotaoNav("⟳", "Recarregar");
            var btnExt = CriarBotaoNav("\U0001F310", "Abrir no navegador padrão");
            var txtUrl = new TextBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 4, 4, 4),
                Padding = new Thickness(4, 2, 4, 2),
                Text = url
            };

            DockPanel.SetDock(btnBack, Dock.Left);
            DockPanel.SetDock(btnFwd, Dock.Left);
            DockPanel.SetDock(btnReload, Dock.Left);
            DockPanel.SetDock(btnExt, Dock.Right);
            barra.Children.Add(btnBack);
            barra.Children.Add(btnFwd);
            barra.Children.Add(btnReload);
            barra.Children.Add(btnExt);
            barra.Children.Add(txtUrl); // preenche o restante

            var wv = new WebView2();
            Grid.SetRow(wv, 1);
            grid.Children.Add(barra);
            grid.Children.Add(wv);

            string host;
            try { host = new Uri(url).Host; } catch { host = "Navegador"; }

            var tab = CriarAba(host, url, grid, wv);
            tab.Tag = null; // sem caminho PDF: é aba de navegador
            TabPdfs.Items.Add(tab);
            TabPdfs.SelectedItem = tab;

            var titleText = ((StackPanel)tab.Header).Children[0] as TextBlock;

            if (_env != null)
            {
                await wv.EnsureCoreWebView2Async(_env);
            }
            else
            {
                await wv.EnsureCoreWebView2Async();
            }

            btnBack.Click += (s, e) => { if (wv.CoreWebView2.CanGoBack) wv.CoreWebView2.GoBack(); };
            btnFwd.Click += (s, e) => { if (wv.CoreWebView2.CanGoForward) wv.CoreWebView2.GoForward(); };
            btnReload.Click += (s, e) => wv.CoreWebView2.Reload();
            btnExt.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(wv.CoreWebView2.Source) { UseShellExecute = true }); } catch { }
            };
            txtUrl.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    string alvo = txtUrl.Text.Trim();
                    if (!alvo.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !alvo.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        alvo = "https://" + alvo;
                    }
                    try { wv.CoreWebView2.Navigate(alvo); } catch { }
                }
            };
            wv.CoreWebView2.SourceChanged += (s, e) =>
            {
                txtUrl.Text = wv.CoreWebView2.Source;
                btnBack.IsEnabled = wv.CoreWebView2.CanGoBack;
                btnFwd.IsEnabled = wv.CoreWebView2.CanGoForward;
            };
            wv.CoreWebView2.HistoryChanged += (s, e) =>
            {
                btnBack.IsEnabled = wv.CoreWebView2.CanGoBack;
                btnFwd.IsEnabled = wv.CoreWebView2.CanGoForward;
            };
            wv.CoreWebView2.DocumentTitleChanged += (s, e) =>
            {
                if (titleText != null)
                {
                    string t = wv.CoreWebView2.DocumentTitle;
                    titleText.Text = string.IsNullOrWhiteSpace(t) ? host : (t.Length > 30 ? t.Substring(0, 30) + "…" : t);
                }
            };
            // Links target=_blank dentro do navegador abrem na própria aba (evita popups)
            wv.CoreWebView2.NewWindowRequested += (s, args) =>
            {
                args.Handled = true;
                try { wv.CoreWebView2.Navigate(args.Uri); } catch { }
            };

            wv.CoreWebView2.Navigate(url);
            btnBack.IsEnabled = false;
            btnFwd.IsEnabled = false;
        }

        // Roda do mouse sobre a faixa de abas rola horizontalmente (abas ficam sempre na mesma linha).
        private void TabHeaderScroll_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer sv)
            {
                sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private void TabPdfs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != TabPdfs) return;

            if (TabPdfs.SelectedItem is TabItem selectedTab)
            {
                if (selectedTab.Tag is string caminho && selectedTab.Content is WebView2)
                {
                    TxtStatus.Text = $"Arquivo aberto: {caminho}";
                    TxtSidePanel.Text = "Documento carregado. Você já pode utilizar as ferramentas superiores para Assinar, Comprimir ou Exportar este arquivo.";
                    TxtSidePanel.Foreground = System.Windows.Media.Brushes.Black;
                    AtualizarEstadoBotoes(true);
                }
                else
                {
                    // Aba de navegador: ferramentas de PDF não se aplicam
                    TxtStatus.Text = "Página web aberta no navegador interno.";
                    AtualizarEstadoBotoes(false);
                }
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
            BtnAddImage.IsEnabled = habilitar;
            BtnFreeEditor.IsEnabled = habilitar;

            BtnSideSignPdf.IsEnabled = habilitar;
            BtnSideCompressPdf.IsEnabled = habilitar;
            BtnSideExportWord.IsEnabled = habilitar;
            BtnSideExportExcel.IsEnabled = habilitar;
            BtnSideOrganizePages.IsEnabled = habilitar;
            BtnSideAddImage.IsEnabled = habilitar;
            BtnSideFreeEditor.IsEnabled = habilitar;
        }

        private bool _sidebarCollapsed = false;
        private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            _sidebarCollapsed = !_sidebarCollapsed;
            if (_sidebarCollapsed)
            {
                SideCol.Width = new GridLength(40);
                SidePanelContent.Visibility = Visibility.Collapsed;
                TxtSideTitle.Visibility = Visibility.Collapsed;
                BtnToggleSidebar.Content = "»"; // »
                BtnToggleSidebar.ToolTip = "Expandir painel";
            }
            else
            {
                SideCol.Width = new GridLength(220);
                SidePanelContent.Visibility = Visibility.Visible;
                TxtSideTitle.Visibility = Visibility.Visible;
                BtnToggleSidebar.Content = "«"; // «
                BtnToggleSidebar.ToolTip = "Recolher painel";
            }
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            string aboutText = "União PDF FCS (UPDF)\n" +
                               $"Versão {CurrentVersion}\n\n" +
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

        private async void BtnSignPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_caminhoPdfAtual))
                {
                    MessageBox.Show("Por favor, abra um documento PDF primeiro.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string origem = _caminhoPdfAtual;

                // 0. Escolher local visual (e se assina todas as páginas ou só a atual)
                SignaturePlacementWindow placementWin = new SignaturePlacementWindow(origem);
                placementWin.Owner = this;
                if (placementWin.ShowDialog() != true) return; // cancelou

                Rect sigRect = placementWin.SelectedRect;
                int sigPage = placementWin.PageNumber;
                bool todasPaginas = placementWin.ApplyToAllPages;

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

                // 3. Perguntar: gerar novo arquivo ou substituir o atual?
                SaveModeWindow modoWin = new SaveModeWindow { Owner = this };
                if (modoWin.ShowDialog() != true || modoWin.Modo == ModoSalvar.Cancelar) return;
                bool substituir = (modoWin.Modo == ModoSalvar.Substituir);

                string dest;
                if (substituir)
                {
                    dest = origem;
                }
                else
                {
                    string dirOriginal = System.IO.Path.GetDirectoryName(origem);
                    string nomeOriginal = System.IO.Path.GetFileNameWithoutExtension(origem);
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        InitialDirectory = dirOriginal,
                        FileName = $"{nomeOriginal}_assinado.pdf",
                        Filter = "Arquivos PDF (*.pdf)|*.pdf",
                        Title = "Salvar PDF Assinado"
                    };
                    if (saveFileDialog.ShowDialog() != true) return;
                    dest = saveFileDialog.FileName;
                }

                // 4. Converter certificado para iText7 BC
                Org.BouncyCastle.X509.X509CertificateParser parser = new Org.BouncyCastle.X509.X509CertificateParser();
                Org.BouncyCastle.X509.X509Certificate bcCert = parser.ReadCertificate(cert.RawData);
                iText.Commons.Bouncycastle.Cert.IX509Certificate[] chain = new iText.Commons.Bouncycastle.Cert.IX509Certificate[1];
                chain[0] = new iText.Bouncycastle.X509.X509CertificateBC(bcCert);

                // 5. Definir páginas a assinar
                var paginas = new System.Collections.Generic.List<int>();
                if (todasPaginas)
                {
                    for (int p = 1; p <= placementWin.PageCount; p++) paginas.Add(p);
                }
                else
                {
                    paginas.Add(sigPage);
                }

                // 6. Assinar (sequencialmente, uma assinatura real por página) num arquivo temporário
                ResultadoAssinatura resultado = await Task.Run(() => AssinarPaginas(
                    origem, paginas, sigRect, placementWin.CanvasWidth, placementWin.CanvasHeight, cert, chain));
                string assinadoTemp = resultado.Caminho;

                // 7. Gravar no destino final
                if (substituir)
                {
                    // Libera o bloqueio do WebView2 sobre o arquivo atual antes de sobrescrever
                    var tab = TabPdfs.SelectedItem as TabItem;
                    var wv = tab?.Content as WebView2;
                    if (wv != null)
                    {
                        wv.Source = new Uri("about:blank");
                        await Task.Delay(400);
                    }
                    System.IO.File.Copy(assinadoTemp, dest, true);
                    if (wv != null) wv.Source = new Uri(dest);
                }
                else
                {
                    System.IO.File.Copy(assinadoTemp, dest, true);
                    _caminhoPdfAtual = dest; // repõe a aba atual apontando pro novo arquivo
                }

                try { System.IO.File.Delete(assinadoTemp); } catch { }

                TxtStatus.Text = $"Arquivo aberto: {dest}";
                string msgPaginas = todasPaginas ? $"em todas as {paginas.Count} páginas" : $"na página {sigPage}";
                string msgCarimbo = resultado.ComCarimbo
                    ? "\n\nCarimbo de tempo aplicado."
                    : (string.IsNullOrEmpty(resultado.AvisoCarimbo) ? "" : $"\n\n{resultado.AvisoCarimbo}");
                MessageBox.Show($"Documento assinado com sucesso ({msgPaginas})!\n\nSalvo em: {dest}{msgCarimbo}", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao assinar: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Resultado de AssinarPaginas: caminho do temporário + se o carimbo de tempo entrou.
        private class ResultadoAssinatura
        {
            public string Caminho = string.Empty;
            public bool ComCarimbo;
            public string AvisoCarimbo = string.Empty;
        }

        // Assina cada página da lista na mesma posição proporcional. Cada página recebe uma assinatura
        // digital real; a partir da 2ª usa modo append para preservar as assinaturas anteriores.
        // Retorna o caminho de um arquivo temporário com o resultado final.
        private ResultadoAssinatura AssinarPaginas(
            string origem,
            System.Collections.Generic.List<int> paginas,
            Rect sigRect,
            double canvasWidth,
            double canvasHeight,
            System.Security.Cryptography.X509Certificates.X509Certificate2 cert,
            iText.Commons.Bouncycastle.Cert.IX509Certificate[] chain)
        {
            string nomeSignatario = cert.GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType.SimpleName, false);
            string dataAssinatura = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss zzz");

            var resultado = new ResultadoAssinatura();
            UpdfConfig config = UpdfConfig.Carregar();

            // Carimbo de tempo (RFC 3161) e dados de revogação (LTV). Ambos dependem de rede:
            // se a TSA não responder, a assinatura sai sem carimbo em vez de falhar.
            iText.Signatures.ITSAClient? tsaClient = null;
            if (config.TsaHabilitado && !string.IsNullOrWhiteSpace(config.TsaUrl))
            {
                try
                {
                    tsaClient = string.IsNullOrEmpty(config.TsaUsuario)
                        ? new iText.Signatures.TSAClientBouncyCastle(config.TsaUrl)
                        : new iText.Signatures.TSAClientBouncyCastle(config.TsaUrl, config.TsaUsuario, config.TsaSenha);
                }
                catch (Exception ex)
                {
                    tsaClient = null;
                    resultado.AvisoCarimbo = "Assinatura gravada SEM carimbo de tempo (TSA inválida: " + ex.Message + ").";
                }
            }

            iText.Signatures.IOcspClient? ocspClient = null;
            System.Collections.Generic.ICollection<iText.Signatures.ICrlClient>? crlClients = null;
            if (config.LtvHabilitado)
            {
                try
                {
                    ocspClient = new iText.Signatures.OcspClientBouncyCastle(null);
                    crlClients = new System.Collections.Generic.List<iText.Signatures.ICrlClient>
                    {
                        new iText.Signatures.CrlClientOnline(chain)
                    };
                }
                catch
                {
                    // Sem OCSP/CRL a assinatura continua válida, só não fica LTV.
                    ocspClient = null;
                    crlClients = null;
                }
            }

            bool origemJaAssinada;
            // Nomes de campo já existentes no arquivo (assinaturas anteriores de outras sessões).
            // Precisa ser único por assinatura, senão o iText lança "Field has been already signed."
            var nomesCamposUsados = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            using (iText.Kernel.Pdf.PdfReader checkReader = new iText.Kernel.Pdf.PdfReader(origem))
            using (iText.Kernel.Pdf.PdfDocument checkDoc = new iText.Kernel.Pdf.PdfDocument(checkReader))
            {
                origemJaAssinada = new iText.Signatures.SignatureUtil(checkDoc).GetSignatureNames().Count > 0;

                var acro = iText.Forms.PdfAcroForm.GetAcroForm(checkDoc, false);
                if (acro != null)
                {
                    foreach (string nomeCampo in acro.GetAllFormFields().Keys) nomesCamposUsados.Add(nomeCampo);
                }
            }

            string atual = origem;   // fonte da vez
            string anterior = null;  // temp intermediário a apagar
            bool precisaRefazerSemCarimbo = false;

            for (int idx = 0; idx < paginas.Count; idx++)
            {
                int pagina = paginas[idx];
                string saida = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
                string nomeCampoDaVez = string.Empty;

                var props = new iText.Kernel.Pdf.StampingProperties();
                // Preserva assinaturas já aplicadas: tanto as adicionadas nas páginas anteriores
                // deste loop (idx > 0) quanto uma assinatura pré-existente no arquivo de origem.
                if (idx > 0 || origemJaAssinada) props.UseAppendMode();

                using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(atual))
                using (System.IO.FileStream fs = new System.IO.FileStream(saida, System.IO.FileMode.Create))
                {
                    iText.Signatures.PdfSigner signer = new iText.Signatures.PdfSigner(reader, fs, props);

                    iText.Kernel.Pdf.PdfDocument pdfDoc = signer.GetDocument();
                    iText.Kernel.Pdf.PdfPage pdfPage = pdfDoc.GetPage(pagina);
                    iText.Kernel.Geom.Rectangle pageSize = pdfPage.GetPageSize();

                    float PW = pageSize.GetWidth();
                    float PH = pageSize.GetHeight();
                    int rot = ((pdfPage.GetRotation() % 360) + 360) % 360;

                    // Dimensões visíveis (renderizadas) em pontos: trocam quando a página é girada 90/270.
                    float VW = (rot == 90 || rot == 270) ? PH : PW;
                    float VH = (rot == 90 || rot == 270) ? PW : PH;

                    // Retângulo desenhado, em pontos, na orientação visível (origem inferior-esquerda).
                    float vx0 = (float)(sigRect.X / canvasWidth) * VW;
                    float vy0 = (float)(sigRect.Y / canvasHeight) * VH;
                    float vx1 = (float)((sigRect.X + sigRect.Width) / canvasWidth) * VW;
                    float vy1 = (float)((sigRect.Y + sigRect.Height) / canvasHeight) * VH;

                    // Mapeia cada canto do espaço visível para o espaço não-rotacionado da página.
                    var p0 = MapearParaPagina(vx0, vy0, rot, PW, PH);
                    var p1 = MapearParaPagina(vx1, vy1, rot, PW, PH);

                    float rectX = Math.Min(p0.Item1, p1.Item1);
                    float rectY = Math.Min(p0.Item2, p1.Item2);
                    float rectW = Math.Abs(p1.Item1 - p0.Item1);
                    float rectH = Math.Abs(p1.Item2 - p0.Item2);

                    signer.SetPageRect(new iText.Kernel.Geom.Rectangle(rectX, rectY, rectW, rectH));
                    signer.SetPageNumber(pagina);
                    // Nome único por página E por assinatura: permite várias assinaturas
                    // (PF + PJ, dois engenheiros, etc.) no mesmo documento e na mesma página.
                    string nomeCampoAssinatura = $"Assinatura_p{pagina}";
                    int sufixo = 2;
                    while (nomesCamposUsados.Contains(nomeCampoAssinatura))
                    {
                        nomeCampoAssinatura = $"Assinatura_p{pagina}_{sufixo}";
                        sufixo++;
                    }
                    nomesCamposUsados.Add(nomeCampoAssinatura);
                    nomeCampoDaVez = nomeCampoAssinatura;
                    signer.SetFieldName(nomeCampoAssinatura);

                    // API atual do iText 8 (PdfSignatureAppearance está obsoleto e sai no iText 9).
                    signer.SetReason("Assinatura Digital");
                    var appearance = new iText.Forms.Form.Element.SignatureFieldAppearance(nomeCampoAssinatura);
                    appearance.SetContent(nomeSignatario,
                        $"Assinado de forma digital por {nomeSignatario}\nDados: {dataAssinatura}");
                    signer.SetSignatureAppearance(appearance);

                    iText.Signatures.IExternalSignature pks = new CustomX509Certificate2Signature(cert, "SHA-256");

                    // Com carimbo de tempo usa CAdES (padrão exigido para AD-RT); sem TSA
                    // mantém CMS, que é o formato que o app já gerava.
                    if (tsaClient != null)
                    {
                        try
                        {
                            signer.SignDetached(pks, chain, crlClients, ocspClient, tsaClient, 0,
                                iText.Signatures.PdfSigner.CryptoStandard.CADES);
                            resultado.ComCarimbo = true;
                        }
                        catch (Exception ex)
                        {
                            // TSA fora do ar / sem internet: refaz a assinatura sem carimbo.
                            // O PdfSigner já foi consumido, então o retry acontece fora deste bloco.
                            tsaClient = null;
                            resultado.ComCarimbo = false;
                            resultado.AvisoCarimbo =
                                "Assinatura gravada SEM carimbo de tempo.\nA TSA não respondeu: " + DetalharErro(ex);
                            precisaRefazerSemCarimbo = true;
                        }
                    }

                    if (!precisaRefazerSemCarimbo && tsaClient == null)
                    {
                        signer.SignDetached(pks, chain, crlClients, ocspClient, null, 0,
                            iText.Signatures.PdfSigner.CryptoStandard.CMS);
                    }
                }

                if (precisaRefazerSemCarimbo)
                {
                    // Descarta o arquivo meio-escrito e repete a MESMA página sem TSA.
                    precisaRefazerSemCarimbo = false;
                    try { System.IO.File.Delete(saida); } catch { }
                    nomesCamposUsados.Remove(nomeCampoDaVez);
                    idx--; // repete a iteração
                    continue;
                }

                if (anterior != null)
                {
                    try { System.IO.File.Delete(anterior); } catch { }
                }
                anterior = saida;
                atual = saida;
            }

            resultado.Caminho = atual;
            return resultado;
        }

        // O iText embrulha falhas de rede num PdfException genérico ("Unknown PdfException.");
        // a causa útil fica na InnerException.
        private static string DetalharErro(Exception ex)
        {
            Exception atual = ex;
            while (atual.InnerException != null) atual = atual.InnerException;
            string msg = atual.Message;
            if (!ReferenceEquals(atual, ex) && !string.IsNullOrWhiteSpace(ex.Message))
            {
                msg = ex.Message.TrimEnd('.') + " - " + msg;
            }
            return msg;
        }

        // Compressão e anotações reescrevem o PDF inteiro, o que invalida qualquer assinatura
        // existente. Avisa e pede confirmação; retorna false se o usuário desistir.
        private bool ConfirmarPerdaDeAssinatura(string caminho, string operacao)
        {
            if (!PossuiAssinaturaDigital(caminho)) return true;

            var confirma = MessageBox.Show(
                "Este PDF contém assinatura digital.\n\n" +
                $"{operacao} reescreve o arquivo inteiro e INVALIDA todas as assinaturas existentes. " +
                "O arquivo gerado deixará de ser um documento assinado.\n\n" +
                "Deseja continuar mesmo assim?",
                "Atenção: assinaturas serão invalidadas",
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

            return confirma == MessageBoxResult.Yes;
        }

        // True se o PDF já contém pelo menos uma assinatura digital aplicada.
        private static bool PossuiAssinaturaDigital(string caminho)
        {
            try
            {
                using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(caminho))
                using (iText.Kernel.Pdf.PdfDocument doc = new iText.Kernel.Pdf.PdfDocument(reader))
                {
                    return new iText.Signatures.SignatureUtil(doc).GetSignatureNames().Count > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        // Converte um ponto (origem inferior-esquerda) do espaço visível/renderizado para o
        // espaço de coordenadas não-rotacionado da página, conforme a rotação /Rotate (horária).
        private static Tuple<float, float> MapearParaPagina(float vx, float vy, int rot, float PW, float PH)
        {
            switch (rot)
            {
                case 90: return Tuple.Create(PW - vy, vx);
                case 180: return Tuple.Create(PW - vx, PH - vy);
                case 270: return Tuple.Create(vy, PH - vx);
                default: return Tuple.Create(vx, vy);
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

                if (!ConfirmarPerdaDeAssinatura(_caminhoPdfAtual, "A compressão")) return;

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

                    // Grava num temporário: recomprimir pode AUMENTAR o arquivo (imagens já
                    // otimizadas re-encodadas em JPEG). Só entrega o resultado se ele for menor,
                    // e escrever fora do destino evita truncar a origem quando dest == origem.
                    string tempComprimido = System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");

                    int imgIgnoradas = 0;
                    try
                    {
                        using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(_caminhoPdfAtual))
                        using (iText.Kernel.Pdf.PdfWriter writer = new iText.Kernel.Pdf.PdfWriter(tempComprimido, wp))
                        using (iText.Kernel.Pdf.PdfDocument pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader, writer))
                        {
                            if (selectedLevel > 1)
                            {
                                imgIgnoradas = CompressImagesInPdf(pdfDoc, selectedLevel);
                            }
                        }

                        long tamanhoComprimido = new System.IO.FileInfo(tempComprimido).Length;
                        bool valeuAPena = tamanhoComprimido < tamanhoOriginal;

                        string fonteFinal = valeuAPena ? tempComprimido : _caminhoPdfAtual;
                        bool mesmoArquivo = string.Equals(
                            System.IO.Path.GetFullPath(fonteFinal),
                            System.IO.Path.GetFullPath(dest),
                            StringComparison.OrdinalIgnoreCase);
                        if (!mesmoArquivo) System.IO.File.Copy(fonteFinal, dest, true);

                        long tamanhoNovo = new System.IO.FileInfo(dest).Length;
                        double economia = (tamanhoOriginal - tamanhoNovo) / 1024.0 / 1024.0;
                        double porcentagem = tamanhoOriginal > 0
                            ? ((double)(tamanhoOriginal - tamanhoNovo) / (double)tamanhoOriginal) * 100.0
                            : 0.0;

                        string aviso = imgIgnoradas > 0
                            ? $"\n\nAtenção: {imgIgnoradas} imagem(ns) não puderam ser recomprimidas (transparência ou formato não suportado) e foram mantidas como estavam."
                            : "";

                        if (valeuAPena)
                        {
                            MessageBox.Show($"Documento comprimido com sucesso!\n\nSalvo em: {dest}\n\nRedução de tamanho: {economia:F2} MB ({porcentagem:F1}%){aviso}", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show($"O PDF já estava otimizado: a compressão deixaria o arquivo maior, então foi gravada uma cópia do original sem alterações.\n\nArquivo salvo em: {dest}{aviso}", "Compressão Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    finally
                    {
                        try { System.IO.File.Delete(tempComprimido); } catch { }
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

            if (!ConfirmarPerdaDeAssinatura(_caminhoPdfAtual, "Reorganizar as páginas")) return;

            OrganizePagesWindow organizeWindow = new OrganizePagesWindow(_caminhoPdfAtual)
            {
                Owner = this
            };

            if (organizeWindow.ShowDialog() == true)
            {
                TxtStatus.Text = "Páginas reorganizadas com sucesso.";
            }
        }

        private void BtnAddImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_caminhoPdfAtual)) return;
            if (!ConfirmarPerdaDeAssinatura(_caminhoPdfAtual, "Adicionar uma imagem")) return;

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
                    // Abre o resultado numa aba nova; não repõe a aba atual (isso duplicava a aba).
                    AbrirDocumento(visualWindow.SavedFilePath);
                    TxtStatus.Text = "Imagem adicionada e arquivo salvo com sucesso.";
                }
            }
        }

        private void BtnFreeEditor_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_caminhoPdfAtual)) return;
            if (!ConfirmarPerdaDeAssinatura(_caminhoPdfAtual, "Aplicar anotações")) return;

            FreeEditorWindow freeEditorWindow = new FreeEditorWindow(_caminhoPdfAtual);
            freeEditorWindow.Owner = this;
            
            // Esconde a MainWindow para focar no editor livre
            this.Hide();

            string? salvoPeloEditor = freeEditorWindow.ShowDialog() == true ? freeEditorWindow.SavedFilePath : null;

            // Mostra a MainWindow de volta ANTES de abrir a aba: AbrirDocumento mexe na UI
            // da janela principal e precisa dela visível para o WebView2 inicializar.
            this.Show();

            if (!string.IsNullOrEmpty(salvoPeloEditor))
            {
                // Abre o resultado numa aba nova; não repõe a aba atual (isso duplicava a aba).
                AbrirDocumento(salvoPeloEditor);
                TxtStatus.Text = "Anotações livres aplicadas e arquivo salvo.";
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

        // Retorna quantas imagens foram ignoradas (não recomprimidas).
        private int CompressImagesInPdf(iText.Kernel.Pdf.PdfDocument pdfDoc, int level)
        {
            int ignoradas = 0;
            for (int i = 1; i <= pdfDoc.GetNumberOfPdfObjects(); i++)
            {
                iText.Kernel.Pdf.PdfObject obj = pdfDoc.GetPdfObject(i);
                if (obj != null && obj.IsStream())
                {
                    iText.Kernel.Pdf.PdfStream stream = (iText.Kernel.Pdf.PdfStream)obj;
                    if (iText.Kernel.Pdf.PdfName.Image.Equals(stream.GetAsName(iText.Kernel.Pdf.PdfName.Subtype)))
                    {
                        // Recodificar como JPEG RGB descartaria máscara de transparência ou stencil; mantém intacta.
                        if (stream.ContainsKey(iText.Kernel.Pdf.PdfName.SMask)
                            || stream.ContainsKey(iText.Kernel.Pdf.PdfName.Mask)
                            || stream.GetAsBool(iText.Kernel.Pdf.PdfName.ImageMask) == true)
                        {
                            ignoradas++;
                            continue;
                        }

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
                                }
                            }
                        }
                        catch
                        {
                            // Imagem em formato que o GDI+ não decodifica (ex: CMYK, JBIG2, CCITT): fica como está.
                            ignoradas++;
                        }
                    }
                }
            }
            return ignoradas;
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