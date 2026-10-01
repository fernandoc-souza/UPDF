using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PdfToolbox
{
    // Configuração persistida em %LOCALAPPDATA%\UPDF\config.json.
    // O arquivo é criado com os valores padrão na primeira execução e pode ser
    // editado à mão pelo usuário (o app relê a cada assinatura).
    public class UpdfConfig
    {
        // Carimbo de tempo RFC 3161. DESLIGADO por padrão: assinar não faz nenhuma
        // chamada de rede e o resultado é o mesmo CMS de sempre.
        //
        // Para ligar, ponha "tsaHabilitado": true e preencha TsaUrl com a TSA da sua
        // Autoridade Certificadora (e usuário/senha, se ela exigir). Só uma TSA
        // credenciada ICP-Brasil produz assinatura AD-RT com validade legal aqui.
        // TSAs públicas gratuitas (ex.: http://timestamp.digicert.com) também funcionam
        // e provam a data, mas NÃO são credenciadas ICP-Brasil.
        [JsonPropertyName("tsaHabilitado")]
        public bool TsaHabilitado { get; set; } = false;

        [JsonPropertyName("tsaUrl")]
        public string TsaUrl { get; set; } = "";

        [JsonPropertyName("tsaUsuario")]
        public string TsaUsuario { get; set; } = "";

        [JsonPropertyName("tsaSenha")]
        public string TsaSenha { get; set; } = "";

        // Embute OCSP/CRL na assinatura (LTV): permite validar o certificado no futuro,
        // mesmo depois que ele expirar. Exige acesso à internet no momento de assinar.
        [JsonPropertyName("ltvHabilitado")]
        public bool LtvHabilitado { get; set; } = true;

        // Segundos de espera pela TSA antes de desistir e assinar sem carimbo.
        [JsonPropertyName("tsaTimeoutSegundos")]
        public int TsaTimeoutSegundos { get; set; } = 15;

        public static string CaminhoArquivo => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UPDF", "config.json");

        private static readonly JsonSerializerOptions Opcoes = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        // Nunca lança: qualquer falha de leitura cai nos valores padrão.
        public static UpdfConfig Carregar()
        {
            try
            {
                string caminho = CaminhoArquivo;
                if (File.Exists(caminho))
                {
                    UpdfConfig? lido = JsonSerializer.Deserialize<UpdfConfig>(File.ReadAllText(caminho), Opcoes);
                    if (lido != null) return lido;
                }
                else
                {
                    UpdfConfig padrao = new UpdfConfig();
                    padrao.Salvar();
                    return padrao;
                }
            }
            catch
            {
                // config.json corrompido ou sem permissão: segue com o padrão.
            }
            return new UpdfConfig();
        }

        public void Salvar()
        {
            try
            {
                string caminho = CaminhoArquivo;
                string? pasta = Path.GetDirectoryName(caminho);
                if (!string.IsNullOrEmpty(pasta)) Directory.CreateDirectory(pasta);
                File.WriteAllText(caminho, JsonSerializer.Serialize(this, Opcoes));
            }
            catch
            {
                // Sem permissão de escrita: não é motivo pra quebrar a assinatura.
            }
        }
    }
}
