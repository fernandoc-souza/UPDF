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
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace PdfToolbox
{
    public class AnnotationElement
    {
        public FrameworkElement Element { get; set; }
        public double CanvasWidth { get; set; }
        public double CanvasHeight { get; set; }
    }

    public partial class FreeEditorWindow : Window
    {
        private string _pdfPath;
        private Windows.Data.Pdf.PdfDocument _pdfDoc;
        private int _currentPage = 1;
        private int _pageCount = 1;
        
        private double _currentZoom = 1.0;
        private bool _isFirstLoad = true;

        private enum ToolMode { Select, Highlight, Text, Eraser }
        private ToolMode _currentMode = ToolMode.Select;

        private Point _startPoint;
        private bool _isDrawing = false;
        
        private FrameworkElement _selectedElement;

        // Armazena as anotações por página para não perdê-las ao trocar de página
        public Dictionary<int, List<AnnotationElement>> PageAnnotations { get; private set; } = new Dictionary<int, List<AnnotationElement>>();

        public string SavedFilePath { get; private set; }

        private bool _isUpdatingToolbar = false;

        public FreeEditorWindow(string pdfPath)
        {
            InitializeComponent();
            _pdfPath = pdfPath;

            // Carrega fontes do sistema
            foreach (var fontFamily in Fonts.SystemFontFamilies)
            {
                CmbFontFamily.Items.Add(new ComboBoxItem { Content = fontFamily.Source, Tag = fontFamily });
            }
            if (CmbFontFamily.Items.Count > 0)
            {
                // Seleciona Arial por padrão, ou a primeira da lista
                var arial = CmbFontFamily.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Arial");
                CmbFontFamily.SelectedItem = arial ?? CmbFontFamily.Items[0];
            }
            CmbFontSize.Text = "24";

            Loaded += FreeEditorWindow_Loaded;
            Closed += FreeEditorWindow_Closed;
        }

        private async void FreeEditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var fullPath = System.IO.Path.GetFullPath(_pdfPath);
                string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
                File.Copy(fullPath, tempFile, true);

                StorageFile file = await StorageFile.GetFileFromPathAsync(tempFile);
                _pdfDoc = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);
                _pageCount = (int)_pdfDoc.PageCount;
                
                await RenderPage(_currentPage);
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

        private void FreeEditorWindow_Closed(object sender, EventArgs e)
        {
            _pdfDoc = null;
        }

        private async Task RenderPage(int pageNumber)
        {
            // Salvar elementos da página atual antes de trocar
            SaveCurrentPageElements();

            TxtPageInfo.Text = $"Página {pageNumber} / {_pageCount}";
            PdfCanvas.Children.Clear();
            PdfCanvas.Children.Add(PdfImage);
            PdfCanvas.Children.Add(DrawingRect);
            
            try
            {
                using (Windows.Data.Pdf.PdfPage page = _pdfDoc.GetPage((uint)(pageNumber - 1)))
                {
                    using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                    {
                        var options = new PdfPageRenderOptions { DestinationWidth = (uint)(page.Size.Width * 1.5) };
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

                                // Restaurar elementos salvos desta página
                                RestoreCurrentPageElements();

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
                MessageBox.Show("Erro ao renderizar página: " + ex.Message);
            }
        }

        private void SaveCurrentPageElements()
        {
            List<AnnotationElement> elements = new List<AnnotationElement>();
            foreach (UIElement child in PdfCanvas.Children)
            {
                if (child != PdfImage && child != DrawingRect && child is FrameworkElement fe)
                {
                    elements.Add(new AnnotationElement { Element = fe, CanvasWidth = PdfCanvas.Width, CanvasHeight = PdfCanvas.Height });
                }
            }
            PageAnnotations[_currentPage] = elements;
        }

        private void RestoreCurrentPageElements()
        {
            if (PageAnnotations.ContainsKey(_currentPage))
            {
                foreach (var ann in PageAnnotations[_currentPage])
                {
                    PdfCanvas.Children.Add(ann.Element);
                }
            }
        }

        // --- BOTOES E NAVEGACAO --- //

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

        private void Tool_Click(object sender, RoutedEventArgs e)
        {
            BtnToolSelect.IsChecked = false;
            BtnToolHighlight.IsChecked = false;
            BtnToolText.IsChecked = false;
            BtnToolEraser.IsChecked = false;
            
            if (sender == BtnToolSelect)
            {
                BtnToolSelect.IsChecked = true;
                _currentMode = ToolMode.Select;
                PdfCanvas.Cursor = Cursors.Arrow;
            }
            else if (sender == BtnToolHighlight)
            {
                BtnToolHighlight.IsChecked = true;
                _currentMode = ToolMode.Highlight;
                PdfCanvas.Cursor = Cursors.Cross;
            }
            else if (sender == BtnToolText)
            {
                BtnToolText.IsChecked = true;
                _currentMode = ToolMode.Text;
                PdfCanvas.Cursor = Cursors.IBeam;
            }
            else if (sender == BtnToolEraser)
            {
                BtnToolEraser.IsChecked = true;
                _currentMode = ToolMode.Eraser;
                PdfCanvas.Cursor = Cursors.Cross;
            }
            
            DeselectCurrent();
        }

        // --- ZOOM LÓGICA --- //

        private void PageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0) BtnZoomIn_Click(sender, new RoutedEventArgs());
                else BtnZoomOut_Click(sender, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            bool isScrollingDown = e.Delta < 0;
            if (isScrollingDown)
            {
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

        // --- CANVAS LÓGICA --- //

        private void PdfCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentMode == ToolMode.Highlight || _currentMode == ToolMode.Eraser)
            {
                _isDrawing = true;
                _startPoint = e.GetPosition(PdfCanvas);
                DrawingRect.Width = 0;
                DrawingRect.Height = 0;
                Canvas.SetLeft(DrawingRect, _startPoint.X);
                Canvas.SetTop(DrawingRect, _startPoint.Y);
                DrawingRect.Visibility = Visibility.Visible;
                PdfCanvas.CaptureMouse();
            }
            else if (_currentMode == ToolMode.Text)
            {
                Point p = e.GetPosition(PdfCanvas);
                CreateTextBox(p.X, p.Y);
                Tool_Click(BtnToolSelect, null); // Volta para modo seleção após criar o texto
            }
            else if (_currentMode == ToolMode.Select)
            {
                if (!(e.OriginalSource is TextBox || e.OriginalSource is Rectangle))
                {
                    DeselectCurrent();
                }
            }
        }

        private void PdfCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing && (_currentMode == ToolMode.Highlight || _currentMode == ToolMode.Eraser))
            {
                Point currentPoint = e.GetPosition(PdfCanvas);
                double x = Math.Min(currentPoint.X, _startPoint.X);
                double y = Math.Min(currentPoint.Y, _startPoint.Y);
                double width = Math.Abs(currentPoint.X - _startPoint.X);
                double height = Math.Abs(currentPoint.Y - _startPoint.Y);

                Canvas.SetLeft(DrawingRect, x);
                Canvas.SetTop(DrawingRect, y);
                DrawingRect.Width = width;
                DrawingRect.Height = height;
            }
        }

        private void PdfCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawing && (_currentMode == ToolMode.Highlight || _currentMode == ToolMode.Eraser))
            {
                _isDrawing = false;
                PdfCanvas.ReleaseMouseCapture();
                DrawingRect.Visibility = Visibility.Collapsed;

                if (DrawingRect.Width > 5 && DrawingRect.Height > 5)
                {
                    bool isEraser = _currentMode == ToolMode.Eraser;
                    CreateHighlightRect(Canvas.GetLeft(DrawingRect), Canvas.GetTop(DrawingRect), DrawingRect.Width, DrawingRect.Height, isEraser);
                }
            }
        }

        // --- CRIAÇÃO DE ELEMENTOS --- //

        private void CreateHighlightRect(double x, double y, double w, double h, bool isEraser = false)
        {
            Grid container = new Grid();
            container.Width = w;
            container.Height = h;
            
            Rectangle rect = new Rectangle();
            
            if (isEraser)
            {
                rect.Fill = Brushes.White;
            }
            else
            {
                ComboBoxItem selectedItem = CmbHighlightColor.SelectedItem as ComboBoxItem;
                string hexColor = selectedItem?.Tag.ToString() ?? "#66FFFF00";
                rect.Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(hexColor);
            }
            rect.Stroke = Brushes.Transparent;
            rect.StrokeThickness = 2;
            container.Children.Add(rect);

            AddHandlesToContainer(container);
            
            // Permitir selecionar clicando no highlight
            rect.MouseLeftButtonDown += (s, e) => {
                if (_currentMode == ToolMode.Select)
                {
                    SelectElement(container);
                    e.Handled = true;
                }
            };
            
            Canvas.SetLeft(container, x);
            Canvas.SetTop(container, y);
            PdfCanvas.Children.Add(container);
            SelectElement(container);
        }

        private void CreateTextBox(double x, double y)
        {
            Grid container = new Grid();
            container.Width = 200;
            container.Height = 60;
            container.Background = Brushes.Transparent; // Para receber clicks

            TextBox tb = new TextBox();
            tb.Text = "Digite seu texto...";
            
            // Puxar formatação da barra
            if (CmbFontFamily.SelectedItem is ComboBoxItem fontItem && fontItem.Tag is FontFamily fontFamily)
            {
                tb.FontFamily = fontFamily;
            }
            if (double.TryParse(CmbFontSize.Text, out double fontSize))
            {
                tb.FontSize = fontSize > 0 ? fontSize : 24;
            }
            else
            {
                tb.FontSize = 24;
            }

            if (CmbTextColor.SelectedItem is ComboBoxItem colorItem && colorItem.Tag != null)
            {
                var color = (Color)ColorConverter.ConvertFromString(colorItem.Tag.ToString());
                byte a = (byte)(SldTextOpacity.Value * 255);
                tb.Foreground = new SolidColorBrush(Color.FromArgb(a, color.R, color.G, color.B));
            }
            else
            {
                byte a = (byte)(SldTextOpacity.Value * 255);
                tb.Foreground = new SolidColorBrush(Color.FromArgb(a, 0, 0, 0));
            }

            tb.Background = Brushes.Transparent;
            tb.BorderBrush = Brushes.Transparent;
            tb.Foreground = Brushes.Black;
            tb.AcceptsReturn = true;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.Padding = new Thickness(5);
            container.Children.Add(tb);

            AddHandlesToContainer(container);

            tb.GotFocus += (s, e) => { 
                if (_currentMode == ToolMode.Select) SelectElement(container); 
            };
            
            Canvas.SetLeft(container, x);
            Canvas.SetTop(container, y);
            PdfCanvas.Children.Add(container);
            
            SelectElement(container);
            tb.Focus();
            tb.SelectAll();
        }

        private void AddHandlesToContainer(Grid container)
        {
            // Drag handle (Círculo azul com ícone)
            Border dragHandle = new Border { 
                Width = 24, Height = 24, 
                Background = Brushes.Blue, 
                CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Left, 
                VerticalAlignment = VerticalAlignment.Top, 
                Margin = new Thickness(-12, -12, 0, 0), 
                Cursor = Cursors.SizeAll, 
                Visibility = Visibility.Collapsed, 
                Tag = "DragHandle" 
            };
            TextBlock dragIcon = new TextBlock {
                Text = "✥",
                Foreground = Brushes.White,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -2, 0, 0)
            };
            dragHandle.Child = dragIcon;
            dragHandle.MouseLeftButtonDown += Handle_MouseLeftButtonDown;
            container.Children.Add(dragHandle);

            // Resize handle (Quadrado normal)
            Border resizeHandle = new Border { Width = 16, Height = 16, Background = Brushes.Blue, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, -8, -8), Cursor = Cursors.SizeNWSE, Visibility = Visibility.Collapsed, Tag = "ResizeHandle" };
            resizeHandle.MouseLeftButtonDown += Handle_MouseLeftButtonDown;
            container.Children.Add(resizeHandle);
        }

        // --- DRAG AND DROP & SELEÇÃO --- //

        private bool _isDraggingElement = false;
        private bool _isResizingElement = false;
        private Point _elementDragStart;
        private double _originalWidth;
        private double _originalHeight;

        private void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentMode == ToolMode.Select && sender is Border handle && handle.Parent is Grid container)
            {
                SelectElement(container);
                
                _elementDragStart = e.GetPosition(PdfCanvas);
                handle.CaptureMouse();
                
                if (handle.Tag.ToString() == "DragHandle")
                {
                    _isDraggingElement = true;
                    _originalWidth = Canvas.GetLeft(container);
                    _originalHeight = Canvas.GetTop(container);
                }
                else if (handle.Tag.ToString() == "ResizeHandle")
                {
                    _isResizingElement = true;
                    _originalWidth = container.ActualWidth;
                    _originalHeight = container.ActualHeight;
                }
                
                handle.MouseMove += Handle_MouseMove;
                handle.MouseLeftButtonUp += Handle_MouseLeftButtonUp;
                e.Handled = true;
            }
        }

        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Border handle && handle.Parent is Grid container)
            {
                Point currentPoint = e.GetPosition(PdfCanvas);
                double diffX = currentPoint.X - _elementDragStart.X;
                double diffY = currentPoint.Y - _elementDragStart.Y;

                if (_isDraggingElement)
                {
                    Canvas.SetLeft(container, _originalWidth + diffX);
                    Canvas.SetTop(container, _originalHeight + diffY);
                }
                else if (_isResizingElement)
                {
                    double newW = _originalWidth + diffX;
                    double newH = _originalHeight + diffY;
                    if (newW > 20) container.Width = newW;
                    if (newH > 20) container.Height = newH;
                }
            }
        }

        private void Handle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border handle)
            {
                _isDraggingElement = false;
                _isResizingElement = false;
                handle.ReleaseMouseCapture();
                handle.MouseMove -= Handle_MouseMove;
                handle.MouseLeftButtonUp -= Handle_MouseLeftButtonUp;
            }
        }

        private void SelectElement(FrameworkElement element)
        {
            if (_selectedElement != element) DeselectCurrent();
            _selectedElement = element;
            BtnDeleteSelected.IsEnabled = true;

            if (_selectedElement is Grid container)
            {
                foreach (UIElement child in container.Children)
                {
                    if (child is Rectangle rect && rect.Fill != null) rect.Stroke = Brushes.Blue;
                    if (child is TextBox tb)
                    {
                        tb.BorderBrush = Brushes.Blue;
                        UpdateToolbarFromTextBox(tb);
                    }
                    if (child is Border handle) handle.Visibility = Visibility.Visible;
                }
            }
        }

        private void DeselectCurrent()
        {
            if (_selectedElement != null)
            {
                if (_selectedElement is Grid container)
                {
                    foreach (UIElement child in container.Children)
                    {
                        if (child is Rectangle rect && rect.Fill != null) rect.Stroke = Brushes.Transparent;
                        if (child is TextBox tb) tb.BorderBrush = Brushes.Transparent;
                        if (child is Border handle) handle.Visibility = Visibility.Collapsed;
                    }
                }
                _selectedElement = null;
                BtnDeleteSelected.IsEnabled = false;
            }
        }

        private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElement != null)
            {
                PdfCanvas.Children.Remove(_selectedElement);
                _selectedElement = null;
                BtnDeleteSelected.IsEnabled = false;
            }
        }

        // --- SALVAR --- //

        private void UpdateToolbarFromTextBox(TextBox tb)
        {
            _isUpdatingToolbar = true;
            
            // Fonte
            var fontItem = CmbFontFamily.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == tb.FontFamily.Source);
            if (fontItem != null) CmbFontFamily.SelectedItem = fontItem;
            
            // Tamanho
            CmbFontSize.Text = tb.FontSize.ToString();
            
            // Cor e Opacidade
            if (tb.Foreground is SolidColorBrush brush)
            {
                string hexColor = string.Format("#FF{0:X2}{1:X2}{2:X2}", brush.Color.R, brush.Color.G, brush.Color.B);
                var colorItem = CmbTextColor.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Tag.ToString() == hexColor);
                if (colorItem != null) CmbTextColor.SelectedItem = colorItem;

                double opacity = brush.Color.A / 255.0;
                SldTextOpacity.Value = opacity;
                if (TxtOpacityValue != null) TxtOpacityValue.Text = $"{(int)(opacity * 100)}%";
            }
            
            _isUpdatingToolbar = false;
        }

        private void TextFormat_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingToolbar) return;

            if (sender == SldTextOpacity && TxtOpacityValue != null)
            {
                TxtOpacityValue.Text = $"{(int)(SldTextOpacity.Value * 100)}%";
            }

            if (_selectedElement is Grid container)
            {
                TextBox tb = container.Children.OfType<TextBox>().FirstOrDefault();
                if (tb != null)
                {
                    if (CmbFontFamily.SelectedItem is ComboBoxItem fontItem && fontItem.Tag is FontFamily fontFamily)
                    {
                        tb.FontFamily = fontFamily;
                    }
                    if (double.TryParse(CmbFontSize.Text, out double fontSize))
                    {
                        if (fontSize > 0) tb.FontSize = fontSize;
                    }

                    if (CmbTextColor.SelectedItem is ComboBoxItem colorItem && colorItem.Tag != null)
                    {
                        var color = (Color)ColorConverter.ConvertFromString(colorItem.Tag.ToString());
                        byte a = (byte)(SldTextOpacity.Value * 255);
                        tb.Foreground = new SolidColorBrush(Color.FromArgb(a, color.R, color.G, color.B));
                    }
                }
            }
        }

        private void CmbFontSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextFormat_Changed(sender, e);
        }

        private void BtnConfirmSave_Click(object sender, RoutedEventArgs e)
        {
            DeselectCurrent(); // Remove bordas de seleção antes de salvar
            SaveCurrentPageElements();

            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Salvar PDF com Anotações...",
                FileName = System.IO.Path.GetFileNameWithoutExtension(_pdfPath) + "_Anotado.pdf"
            };

            if (saveDlg.ShowDialog() == true)
            {
                LoadingText.Text = "Processando e Salvando...";
                LoadingOverlay.Visibility = Visibility.Visible;

                try
                {
                    PdfStamperHelper.StampMultipleAnnotations(_pdfPath, saveDlg.FileName, PageAnnotations);
                    SavedFilePath = saveDlg.FileName;
                    this.DialogResult = true;
                    MessageBox.Show("PDF salvo com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao salvar: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadingOverlay.Visibility = Visibility.Hidden;
                }
            }
        }
    }
}
