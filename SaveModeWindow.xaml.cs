using System.Windows;

namespace PdfToolbox
{
    public enum ModoSalvar
    {
        Cancelar,
        Substituir,
        GerarNovo
    }

    // Substitui o MessageBox Sim/Não/Cancelar por botões que dizem o que fazem.
    public partial class SaveModeWindow : Window
    {
        public ModoSalvar Modo { get; private set; } = ModoSalvar.Cancelar;

        public SaveModeWindow()
        {
            InitializeComponent();
        }

        private void BtnSubstituir_Click(object sender, RoutedEventArgs e)
        {
            Modo = ModoSalvar.Substituir;
            DialogResult = true;
            Close();
        }

        private void BtnGerarNovo_Click(object sender, RoutedEventArgs e)
        {
            Modo = ModoSalvar.GerarNovo;
            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Modo = ModoSalvar.Cancelar;
            DialogResult = false;
            Close();
        }
    }
}
