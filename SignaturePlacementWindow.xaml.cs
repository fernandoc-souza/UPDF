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

        public SignaturePlacementWindow(string pdfPath)
        {
            InitializeComponent();
            LoadPdfAsync(pdfPath);
        }

        private async void LoadPdfAsync(string pdfPath)
        {
            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(pdfPath);
                var pdfDoc = await PdfDocument.LoadFromFileAsync(file);
                
                if (pdfDoc.PageCount > 0)
                {
                    using (var page = pdfDoc.GetPage(0))
                    {
                        var stream = new InMemoryRandomAccessStream();
                        await page.RenderToStreamAsync(stream);
                        
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream.AsStream();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        
                        PdfImage.Source = bitmap;
                        PdfCanvas.Width = bitmap.PixelWidth;
                        PdfCanvas.Height = bitmap.PixelHeight;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar pré-visualização: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            
            this.DialogResult = true;
            this.Close();
        }
    }
}
