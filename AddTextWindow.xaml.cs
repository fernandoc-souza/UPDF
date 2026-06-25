using System;
using System.Windows;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;

namespace PdfToolbox
{
    public partial class AddTextWindow : Window
    {
        public string TextContent { get; private set; }
        public string FontName { get; private set; }
        public float FontSize { get; private set; }
        public Color TextColor { get; private set; }

        public AddTextWindow()
        {
            InitializeComponent();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextContent = TxtContent.Text;
                if (string.IsNullOrWhiteSpace(TextContent))
                {
                    MessageBox.Show("O texto não pode estar vazio.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                FontSize = float.Parse(TxtSize.Text);

                switch (CmbFont.SelectedIndex)
                {
                    case 0: FontName = StandardFonts.HELVETICA; break;
                    case 1: FontName = StandardFonts.TIMES_ROMAN; break;
                    case 2: FontName = StandardFonts.COURIER; break;
                    default: FontName = StandardFonts.HELVETICA; break;
                }

                TextColor = ParseColor(TxtColor.Text);

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Valores inválidos. Verifique os números digitados.\nErro: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Color ParseColor(string colorStr)
        {
            colorStr = colorStr.Trim().ToLower();
            if (colorStr.StartsWith("#") && colorStr.Length == 7)
            {
                int r = Convert.ToInt32(colorStr.Substring(1, 2), 16);
                int g = Convert.ToInt32(colorStr.Substring(3, 2), 16);
                int b = Convert.ToInt32(colorStr.Substring(5, 2), 16);
                return new DeviceRgb(r, g, b);
            }

            switch (colorStr)
            {
                case "red": return ColorConstants.RED;
                case "blue": return ColorConstants.BLUE;
                case "green": return ColorConstants.GREEN;
                case "white": return ColorConstants.WHITE;
                case "gray": return ColorConstants.GRAY;
                case "yellow": return ColorConstants.YELLOW;
                default: return ColorConstants.BLACK;
            }
        }
    }
}
