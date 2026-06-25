using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PdfToolbox
{
    public partial class OrganizePagesWindow : Window
    {
        public ObservableCollection<PageItem> PagesCollection { get; set; } = new ObservableCollection<PageItem>();
        private string _initialPdfPath;
        private Point _dragStartPoint;
        private bool _isDragging = false;

        public OrganizePagesWindow(string initialPdfPath)
        {
            InitializeComponent();
            _initialPdfPath = initialPdfPath;
            PagesListBox.ItemsSource = PagesCollection;
            
            Loaded += OrganizePagesWindow_Loaded;
        }

        private async void OrganizePagesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadPdfPagesAsync(_initialPdfPath);
        }

        private async Task LoadPdfPagesAsync(string pdfPath)
        {
            LoadingText.Visibility = Visibility.Visible;
            try
            {
                var fullPath = System.IO.Path.GetFullPath(pdfPath);

                // Run Windows.Data.Pdf operations on a background thread (MTA)
                var renderedStreams = await Task.Run(async () =>
                {
                    var results = new System.Collections.Generic.List<Tuple<int, byte[]>>();
                    
                    // CRIA UMA CÓPIA TEMPORÁRIA PARA EVITAR BLOQUEIO PELO WEBVIEW2
                    string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
                    System.IO.File.Copy(fullPath, tempFile, true);

                    try
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(tempFile);
                        Windows.Data.Pdf.PdfDocument pdfDoc = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);

                        for (uint i = 0; i < pdfDoc.PageCount; i++)
                        {
                            using (Windows.Data.Pdf.PdfPage page = pdfDoc.GetPage(i))
                            {
                                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                                {
                                    var options = new PdfPageRenderOptions { DestinationWidth = (uint)(page.Size.Width / 3) };
                                    await page.RenderToStreamAsync(stream, options);

                                    using (Stream netStream = stream.AsStream())
                                    {
                                        netStream.Position = 0;
                                        using (MemoryStream ms = new MemoryStream())
                                        {
                                            await netStream.CopyToAsync(ms);
                                            results.Add(new Tuple<int, byte[]>((int)(i + 1), ms.ToArray()));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        // Limpa o arquivo temporário
                        if (System.IO.File.Exists(tempFile))
                        {
                            try { System.IO.File.Delete(tempFile); } catch { }
                        }
                    }
                    return results;
                });

                // Convert byte array to BitmapImage back on the UI thread
                foreach (var tuple in renderedStreams)
                {
                    BitmapImage image = new BitmapImage();
                    using (MemoryStream ms = new MemoryStream(tuple.Item2))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = ms;
                        image.EndInit();
                        image.Freeze();
                    }

                    PagesCollection.Add(new PageItem
                    {
                        SourcePdfPath = pdfPath,
                        OriginalPageNumber = tuple.Item1,
                        Thumbnail = image
                    });
                }

                UpdateDisplayNumbers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar miniaturas:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingText.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateDisplayNumbers()
        {
            for (int i = 0; i < PagesCollection.Count; i++)
            {
                PagesCollection[i].DisplayPageNumber = i + 1;
                // Force update UI
                var item = PagesCollection[i];
                PagesCollection[i] = null;
                PagesCollection[i] = item;
            }
        }

        private async void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Arquivos PDF (*.pdf)|*.pdf",
                Title = "Selecionar PDF para Inserir"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await LoadPdfPagesAsync(openFileDialog.FileName);
            }
        }

        private void BtnMoveLeft_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = PagesListBox.SelectedIndex;
            if (selectedIndex > 0)
            {
                var item = PagesCollection[selectedIndex];
                PagesCollection.RemoveAt(selectedIndex);
                PagesCollection.Insert(selectedIndex - 1, item);
                PagesListBox.SelectedIndex = selectedIndex - 1;
                UpdateDisplayNumbers();
            }
        }

        private void BtnMoveRight_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = PagesListBox.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < PagesCollection.Count - 1)
            {
                var item = PagesCollection[selectedIndex];
                PagesCollection.RemoveAt(selectedIndex);
                PagesCollection.Insert(selectedIndex + 1, item);
                PagesListBox.SelectedIndex = selectedIndex + 1;
                UpdateDisplayNumbers();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = PagesListBox.SelectedItems.Cast<PageItem>().ToList();
            foreach (var item in selectedItems)
            {
                PagesCollection.Remove(item);
            }
            UpdateDisplayNumbers();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (PagesCollection.Count == 0)
            {
                MessageBox.Show("Você não pode salvar um PDF sem páginas.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Documento PDF (*.pdf)|*.pdf",
                Title = "Salvar PDF Organizado",
                FileName = System.IO.Path.GetFileNameWithoutExtension(_initialPdfPath) + "_organizado.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    PageOrganizer.OrganizeAndSave(PagesCollection.ToList(), saveFileDialog.FileName);
                    MessageBox.Show("PDF organizado e salvo com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao salvar o PDF organizado:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Drag and Drop Implementation
        private void PagesListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void PagesListBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
                    if (listBoxItem != null)
                    {
                        PageItem pageItem = (PageItem)listBoxItem.DataContext;
                        _isDragging = true;
                        DragDrop.DoDragDrop(PagesListBox, pageItem, DragDropEffects.Move);
                        _isDragging = false;
                    }
                }
            }
        }

        private void PagesListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(PageItem)))
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void PagesListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(PageItem)))
            {
                PageItem droppedData = (PageItem)e.Data.GetData(typeof(PageItem));
                PageItem targetData = null;

                var listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
                if (listBoxItem != null)
                {
                    targetData = (PageItem)listBoxItem.DataContext;
                }

                if (droppedData != null && targetData != null && !ReferenceEquals(droppedData, targetData))
                {
                    int removeIdx = PagesCollection.IndexOf(droppedData);
                    int targetIdx = PagesCollection.IndexOf(targetData);

                    if (removeIdx != -1 && targetIdx != -1)
                    {
                        PagesCollection.RemoveAt(removeIdx);
                        PagesCollection.Insert(targetIdx, droppedData);
                        PagesListBox.SelectedItem = droppedData;
                        UpdateDisplayNumbers();
                    }
                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }
    }
}
