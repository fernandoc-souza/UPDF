using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using Windows.Data.Pdf;
using Windows.Storage.Streams;
using System.Threading.Tasks;

namespace PdfToolbox
{
    public partial class SignaturePlacementWindow : Window
    {
        private Point _startPoint;
        private bool _isDrawing = false;
        
        public Rect SelectedRect { get; private set; }
        public int PageNumber { get; private set; } = 1;
        public double CanvasWidth { get; private set; }
        public double CanvasHeight { get; private set; }

        private Windows.Data.Pdf.PdfDocument _pdfDoc;
        private int _currentPage = 1;
        private int _pageCount = 1;
        
        private double _currentZoom = 1.0;
        private bool _isFirstLoad = true;

        public SignaturePlacementWindow(string pdfPath)
        {
            InitializeComponent();
            LoadPdfAsync(pdfPath);
        }

        private async void LoadPdfAsync(string pdfPath)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var fullPath = System.IO.Path.GetFullPath(pdfPath);
                
                // Copy to temp to avoid locks (same logic as VisualAnnotationWindow)
                string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
                File.Copy(fullPath, tempFile, true);

                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(tempFile);
                _pdfDoc = await PdfDocument.LoadFromFileAsync(file);
                
                if (_pdfDoc.PageCount > 0)
                {
                    _pageCount = (int)_pdfDoc.PageCount;
                    await RenderPage(_currentPage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar pré-visualização: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Hidden;
            }
        }

        private async Task RenderPage(int pageNumber)
        {
            TxtPageInfo.Text = $"Página {pageNumber} / {_pageCount}";
            SelectionRect.Visibility = Visibility.Collapsed;
            
            try
            {
                using (var page = _pdfDoc.GetPage((uint)(pageNumber - 1)))
                {
                    using (var stream = new InMemoryRandomAccessStream())
                    {
                        var options = new Windows.Data.Pdf.PdfPageRenderOptions { DestinationWidth = (uint)(page.Size.Width * 1.5) };
                        await page.RenderToStreamAsync(stream, options);
                        
                        using (Stream netStream = stream.AsStream())
                        {
                            netStream.Position = 0;
                            using (MemoryStream ms = new MemoryStream())
                            {
                                await netStream.CopyToAsync(ms);
                                
                                BitmapImage bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.StreamSource = new MemoryStream(ms.ToArray());
                                bitmap.EndInit();
                                
                                PdfImage.Source = bitmap;
                                PdfCanvas.Width = bitmap.PixelWidth;
                                PdfCanvas.Height = bitmap.PixelHeight;

                                // Aguardar a UI renderizar o tamanho do ScrollViewer
                                await Task.Delay(50);
                                if (_isFirstLoad)
                                {
                                    _isFirstLoad = false;
                                    FitToScreen();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao renderizar página: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage = 1;
                LoadingOverlay.Visibility = Visibility.Visible;
                await RenderPage(_currentPage);
                LoadingOverlay.Visibility = Visibility.Hidden;
            }
        }

        private async void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _pageCount)
            {
                _currentPage = _pageCount;
                LoadingOverlay.Visibility = Visibility.Visible;
                await RenderPage(_currentPage);
                LoadingOverlay.Visibility = Visibility.Hidden;
            }
        }

        private void PageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                    BtnZoomIn_Click(sender, new RoutedEventArgs());
                else
                    BtnZoomOut_Click(sender, new RoutedEventArgs());
                
                e.Handled = true;
                return;
            }

            bool isScrollingDown = e.Delta < 0;

            if (isScrollingDown)
            {
                // Se a rolagem chegou ao fim (ou se não tem barra de rolagem por caber na tela)
                if (PageScrollViewer.ScrollableHeight == 0 || PageScrollViewer.VerticalOffset >= PageScrollViewer.ScrollableHeight - 1)
                {
                    if (_currentPage < _pageCount)
                    {
                        BtnNext_Click(sender, new RoutedEventArgs());
                        e.Handled = true;
                    }
                }
            }
            else
            {
                // Se a rolagem chegou ao topo (ou não tem barra)
                if (PageScrollViewer.ScrollableHeight == 0 || PageScrollViewer.VerticalOffset <= 1)
                {
                    if (_currentPage > 1)
                    {
                        BtnPrev_Click(sender, new RoutedEventArgs());
                        e.Handled = true;
                    }
                }
            }
        }

        private void FitToScreen()
        {
            if (PdfCanvas.Width > 0 && PageScrollViewer.ActualWidth > 0)
            {
                double scaleX = (PageScrollViewer.ActualWidth - 40) / PdfCanvas.Width;
                double scaleY = (PageScrollViewer.ActualHeight - 40) / PdfCanvas.Height;
                _currentZoom = Math.Min(scaleX, scaleY);
                if (_currentZoom > 2.0) _currentZoom = 2.0;
                if (_currentZoom < 0.1) _currentZoom = 0.1;
                ApplyZoom();
            }
        }

        private void ApplyZoom()
        {
            PageScaleTransform.ScaleX = _currentZoom;
            PageScaleTransform.ScaleY = _currentZoom;
            TxtZoom.Text = $"{(int)(_currentZoom * 100)}%";
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom += 0.1;
            if (_currentZoom > 5.0) _currentZoom = 5.0;
            ApplyZoom();
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom -= 0.1;
            if (_currentZoom < 0.1) _currentZoom = 0.1;
            ApplyZoom();
        }

        private void PdfCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = true;
            _startPoint = e.GetPosition(PdfCanvas);
            SelectionRect.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRect, _startPoint.X);
            Canvas.SetTop(SelectionRect, _startPoint.Y);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
            PdfCanvas.CaptureMouse();
        }

        private void PdfCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing)
            {
                var pos = e.GetPosition(PdfCanvas);
                
                var x = Math.Min(pos.X, _startPoint.X);
                var y = Math.Min(pos.Y, _startPoint.Y);
                var w = Math.Abs(pos.X - _startPoint.X);
                var h = Math.Abs(pos.Y - _startPoint.Y);
                
                Canvas.SetLeft(SelectionRect, x);
                Canvas.SetTop(SelectionRect, y);
                SelectionRect.Width = w;
                SelectionRect.Height = h;
            }
        }

        private void PdfCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawing)
            {
                _isDrawing = false;
                PdfCanvas.ReleaseMouseCapture();
                
                SelectedRect = new Rect(Canvas.GetLeft(SelectionRect), Canvas.GetTop(SelectionRect), SelectionRect.Width, SelectionRect.Height);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (SelectionRect.Width == 0 || SelectionRect.Height == 0 || SelectionRect.Visibility == Visibility.Collapsed)
            {
                MessageBox.Show("Por favor, desenhe o retângulo da assinatura antes de confirmar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            double pdfHeight = PdfCanvas.Height;
            double invertedY = pdfHeight - SelectedRect.Y - SelectedRect.Height;
            
            SelectedRect = new Rect(SelectedRect.X, invertedY, SelectedRect.Width, SelectedRect.Height);
            
            CanvasWidth = PdfCanvas.Width;
            CanvasHeight = PdfCanvas.Height;
            
            this.PageNumber = _currentPage;
            this.DialogResult = true;
            this.Close();
        }
    }
}
