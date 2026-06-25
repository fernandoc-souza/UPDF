using System;
using System.Windows;

namespace PdfToolbox
{
    public partial class CompressionLevelWindow : Window
    {
        public int SelectedLevel { get; private set; } = 2; // Default is level 2

        public CompressionLevelWindow()
        {
            InitializeComponent();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (RbLevel1.IsChecked == true) SelectedLevel = 1;
            else if (RbLevel2.IsChecked == true) SelectedLevel = 2;
            else if (RbLevel3.IsChecked == true) SelectedLevel = 3;
            
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
