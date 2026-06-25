using System;
using System.Windows;
using Microsoft.Win32;

namespace PdfToolbox
{
    public partial class AddImageWindow : Window
    {
        public string ImagePath { get; private set; }

        public AddImageWindow()
        {
            InitializeComponent();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Imagens|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Todos os Arquivos|*.*";
            if (dlg.ShowDialog() == true)
            {
                TxtImagePath.Text = dlg.FileName;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ImagePath = TxtImagePath.Text;
                if (string.IsNullOrWhiteSpace(ImagePath) || !System.IO.File.Exists(ImagePath))
                {
                    MessageBox.Show("Por favor, selecione uma imagem válida.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Valores inválidos. Verifique os números digitados.\nErro: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
