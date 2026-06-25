using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using iText.Kernel.Pdf;
using iText.Kernel.Geom;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Controls;

namespace PdfToolbox
{
    public partial class VisualAnnotationWindow : Window
    {
        private string _pdfPath;
        private Windows.Data.Pdf.PdfDocument _pdfDoc;
        private int _currentPage = 1;
        private int _pageCount = 1;

        // Propriedades da Anotação
        public bool IsTextAnnotation { get; set; }
        public string TextContent { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public iText.Kernel.Colors.Color TextColor { get; set; }
        public string ImagePath { get; set; }
        public float ImageScale { get; set; }

        public string SavedFilePath { get; private set; }

        public VisualAnnotationWindow(string pdfPath)
        {
            InitializeComponent();
            _pdfPath = pdfPath;
            Loaded += VisualAnnotationWindow_Loaded;
            Closed += VisualAnnotationWindow_Closed;
        }

        private async void VisualAnnotationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var fullPath = System.IO.Path.GetFullPath(_pdfPath);
                
                // Copy to temp to avoid locks
                string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
                File.Copy(fullPath, tempFile, true);

                StorageFile file = await StorageFile.GetFileFromPathAsync(tempFile);
                _pdfDoc = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);
                _pageCount = (int)_pdfDoc.PageCount;
                
                await RenderPage(_currentPage);
                InitializeFloatingElement();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar PDF: " + ex.Message);
                Close();
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Hidden;
            }
        }

        private void VisualAnnotationWindow_Closed(object sender, EventArgs e)
        {
            _pdfDoc = null;
        }

        private async Task RenderPage(int pageNumber)
        {
            TxtPageInfo.Text = $"Página {pageNumber} / {_pageCount}";
            
            try
            {
                using (Windows.Data.Pdf.PdfPage page = _pdfDoc.GetPage((uint)(pageNumber - 1)))
                {
                    using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                    {
                        var options = new PdfPageRenderOptions { DestinationWidth = (uint)(page.Size.Width * 1.5) }; // Alta resolução
                        await page.RenderToStreamAsync(stream, options);

                        using (Stream netStream = stream.AsStream())
                        {
                            netStream.Position = 0;
                            using (MemoryStream ms = new MemoryStream())
                            {
                                await netStream.CopyToAsync(ms);
                                
                                BitmapImage image = new BitmapImage();
                                image.BeginInit();
                                image.CacheOption = BitmapCacheOption.OnLoad;
                                image.StreamSource = new MemoryStream(ms.ToArray());
                                image.EndInit();
                                
                                PdfImage.Source = image;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao renderizar página: " + ex.Message);
            }
        }

        private async void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadingOverlay.Visibility = Visibility.Visible;
                await RenderPage(_currentPage);
                LoadingOverlay.Visibility = Visibility.Hidden;
            }
        }

        private async void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _pageCount)
            {
                _currentPage++;
                LoadingOverlay.Visibility = Visibility.Visible;
                await RenderPage(_currentPage);
                LoadingOverlay.Visibility = Visibility.Hidden;
            }
        }

        private void InitializeFloatingElement()
        {
            if (IsTextAnnotation)
            {
                FloatingText.Visibility = Visibility.Visible;
                FloatingText.Text = TextContent;
                FloatingText.FontFamily = new FontFamily(FontName);
                FloatingText.FontSize = FontSize;
                
                float[] colorVal = TextColor.GetColorValue();
                if (colorVal.Length >= 3)
                {
                    // iText RGB color
                    byte r = (byte)(colorVal[0] * 255);
                    byte g = (byte)(colorVal[1] * 255);
                    byte b = (byte)(colorVal[2] * 255);
                    FloatingText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
                }

                ResizeThumb.Visibility = Visibility.Collapsed; // Sem resize para texto
                DraggableElement.Width = double.NaN; // Auto
                DraggableElement.Height = double.NaN; // Auto
            }
            else
            {
                FloatingImage.Visibility = Visibility.Visible;
                BitmapImage bmp = new BitmapImage(new Uri(ImagePath));
                FloatingImage.Source = bmp;

                // Definir um tamanho padrão inicial razoável para a imagem (max 300px)
                double initWidth = bmp.PixelWidth > 300 ? 300 : bmp.PixelWidth;
                DraggableElement.Width = initWidth;
                DraggableElement.Height = bmp.PixelHeight * (initWidth / bmp.PixelWidth);
                ResizeThumb.Visibility = Visibility.Visible;
            }

            DraggableElement.Visibility = Visibility.Visible;
            // Centralizar inicialmente
            Canvas.SetLeft(DraggableElement, 50);
            Canvas.SetTop(DraggableElement, 50);
        }

        private bool _isDragging = false;
        private System.Windows.Point _clickOffset;

        private void DraggableElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _clickOffset = e.GetPosition(DraggableElement);
            DraggableElement.CaptureMouse();
            e.Handled = true;
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                System.Windows.Point p = e.GetPosition(OverlayCanvas);
                Canvas.SetLeft(DraggableElement, p.X - _clickOffset.X);
                Canvas.SetTop(DraggableElement, p.Y - _clickOffset.Y);
            }
        }

        private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                DraggableElement.ReleaseMouseCapture();
            }
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = DraggableElement.Width + e.HorizontalChange;
            double newHeight = DraggableElement.Height + e.VerticalChange;
            
            // Manter proporção para imagens? Vamos apenas deixar redimensionar livremente ou manter proporção se quisermos.
            // Aqui permitimos livre, mas com tamanho mínimo
            if (newWidth > 20) DraggableElement.Width = newWidth;
            if (newHeight > 20) DraggableElement.Height = newHeight;
        }

        private void BtnConfirmSave_Click(object sender, RoutedEventArgs e)
        {
            double imageActualWidth = PdfImage.ActualWidth;
            double imageActualHeight = PdfImage.ActualHeight;

            // Posição Final do elemento no Canvas
            double elementLeft = Canvas.GetLeft(DraggableElement);
            double elementTop = Canvas.GetTop(DraggableElement);
            
            // Para imagens, usamos Width/Height. Para texto, usamos ActualWidth/ActualHeight
            double elementWidth = DraggableElement.ActualWidth;
            double elementHeight = DraggableElement.ActualHeight;

            // O iText7 usa o canto inferior esquerdo para imagens (SetFixedPosition).
            // Para TextAlignment.CENTER e MIDDLE, ele usa o centro geométrico passado.
            // Vamos unificar passando sempre o CANTO INFERIOR ESQUERDO e no PdfStamperHelper ajustamos.
            
            double bottomY = elementTop + elementHeight; // Y do canto inferior no WPF

            double ratioX = elementLeft / imageActualWidth;
            double ratioY = bottomY / imageActualHeight;
            double ratioWidth = elementWidth / imageActualWidth;
            double ratioHeight = elementHeight / imageActualHeight;

            float pdfW = 0;
            float pdfH = 0;

            try
            {
                using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(_pdfPath))
                using (iText.Kernel.Pdf.PdfDocument itextDoc = new iText.Kernel.Pdf.PdfDocument(reader))
                {
                    iText.Kernel.Geom.Rectangle rect = itextDoc.GetPage(_currentPage).GetPageSizeWithRotation();
                    pdfW = rect.GetWidth();
                    pdfH = rect.GetHeight();
                }

                // Coordenadas PDF
                float targetX = (float)(ratioX * pdfW);
                float targetY = (float)(pdfH - (ratioY * pdfH));
                
                float targetW = (float)(ratioWidth * pdfW);
                float targetH = (float)(ratioHeight * pdfH);

                // Como o TextAlignment está CENTER/MIDDLE, precisamos passar o centro do texto
                float centerX = targetX + (targetW / 2);
                float centerY = targetY + (targetH / 2);

                SaveFileDialog saveDlg = new SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    Title = "Salvar Arquivo Modificado Como...",
                    FileName = System.IO.Path.GetFileNameWithoutExtension(_pdfPath) + "_Editado.pdf"
                };

                if (saveDlg.ShowDialog() == true)
                {
                    LoadingText.Text = "Processando e Salvando...";
                    LoadingOverlay.Visibility = Visibility.Visible;

                    string savePath = saveDlg.FileName;

                    if (IsTextAnnotation)
                    {
                        // Passa centerX, centerY porque PdfStamperHelper está configurado para centralizar
                        PdfStamperHelper.StampText(_pdfPath, savePath, TextContent, _currentPage, centerX, centerY, FontName, FontSize, TextColor);
                    }
                    else
                    {
                        // Passa o canto inferior esquerdo e a largura/altura
                        PdfStamperHelper.StampImage(_pdfPath, savePath, ImagePath, _currentPage, targetX, targetY, targetW, targetH);
                    }

                    SavedFilePath = savePath;
                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao aplicar anotação: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadingOverlay.Visibility = Visibility.Hidden;
            }
        }
    }
}
