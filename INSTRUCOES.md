# Projeto PDF Toolbox (C# WPF)

Este é o esqueleto gerado para a sua aplicação Windows nativa. Ele utiliza **WPF** com **.NET 8**.

## Como rodar no seu VS Code:

1. Certifique-se de que você tem o **.NET 8 SDK** instalado no seu computador.
2. Extraia o arquivo ZIP que você baixou.
3. No VS Code, vá em **File > Open Folder...** e selecione a pasta `PdfToolbox_CSharp` (não a pasta raiz do zip, mas sim esta pasta específica).
4. O **C# Dev Kit** irá reconhecer o arquivo `.csproj` e carregar a Solution.
5. Abra o Terminal do VS Code (`Ctrl + '`) e rode o comando para baixar as dependências:
   ```bash
   dotnet restore
   ```
6. Para iniciar o programa, rode:
   ```bash
   dotnet run
   ```
   *Ou apenas pressione `F5` se o C# Dev Kit estiver totalmente configurado.*

## Próximos Passos
O arquivo `MainWindow.xaml.cs` possui os botões e dicas de como você deverá implementar a lógica de negócio usando as bibliotecas `iText7` e `OpenXML` que já deixei configuradas no `.csproj`.


## Dicas para depois de gerar o código:
Instale o WebView2: A IA vai pedir para adicionar o pacote WebView2. No terminal do seu VS Code, você precisará rodar:
dotnet add package Microsoft.Web.WebView2
Isso é essencial para o visualizador moderno estilo "Adobe" funcionar dentro do seu programa.
Definir como Padrão do Windows: Depois que seu app estiver rodando sem erros, você pode ir em qualquer arquivo PDF no seu computador, clicar com o botão direito > Abrir com > Escolher outro aplicativo, buscar o .exe que foi gerado na pasta bin/Debug/net8.0-windows do seu projeto, e marcar a caixinha "Sempre usar este aplicativo para abrir arquivos .pdf". (O código gerado no prompt acima cuidará de ler esse arquivo ao iniciar).


## prompt:
Atue como um Engenheiro de Software Sênior especialista em C# e WPF (Windows Presentation Foundation) com .NET 8. 

Meu objetivo é criar um aplicativo desktop nativo para Windows chamado "PDF Toolbox Corporativo". Ele deve ter uma interface moderna e profissional, inspirada no Adobe Reader (com menu superior, barra lateral de ferramentas e uma área central grande para visualização do PDF). 

Por favor, gere os códigos completos (XAML e C#) seguindo estritamente a arquitetura e os requisitos abaixo, garantindo que não haja erros de compilação (CS0103) ou de propriedades XAML inexistentes (MC3072).

### 🛠 Stack Tecnológico e Bibliotecas (Adicionar ao .csproj)
1. **.NET 8 (WPF)** (`<UseWPF>true</UseWPF>`)
2. **Microsoft.Web.WebView2**: Para renderizar o PDF na área central com alta fidelidade (nativo do Edge).
3. **iText7** e **iText7.bouncy-castle-adapter**: Para manipulação de PDF (assinatura e compressão).
4. **DocumentFormat.OpenXml**: Para gerar arquivos do Word (.docx) e Excel (.xlsx).

### 🎨 Arquitetura de Interface (MainWindow.xaml)
A interface deve ser dividida nativamente usando `Grid` e `DockPanel`:
- **Topo (Menu/Ribbon)**: Um menu com "Arquivo" (Abrir, Sair) e botões de atalho rápidos.
- **Centro (Visualizador)**: 
  - Deve conter um `Grid` central.
  - Quando nenhum arquivo estiver aberto, mostrar um `StackPanel` centralizado (nomeado "PnlPlaceholder") com um ícone/texto de "Arraste um PDF ou clique em Abrir".
  - Quando aberto, ocultar o placeholder e exibir um componente `WebView2` (nomeado "PdfWebViewer") ocupando o espaço todo.
- **Barra Lateral Direita ou Esquerda (Ferramentas)**: Um painel com largura fixa contendo botões estilizados para as ferramentas: "Assinar Digitalmente", "Comprimir", "Exportar Word" e "Exportar Excel". (Nomeie os botões adequadamente para o Code-Behind, ex: BtnSignPdf).
- **Rodapé (Status Bar)**: Uma barra simples embaixo mostrando o status atual (nomeada "TxtStatus").
*Nota de XAML:* Certifique-se de usar apenas propriedades válidas do WPF puro (por exemplo, botões padrão não têm a propriedade `CornerRadius` sem um template, então use propriedades seguras como `Margin`, `Background`, `Foreground`).

### ⚙️ Funcionalidades e Regras de Negócio (MainWindow.xaml.cs)
Gere os manipuladores de eventos e a base lógica para:
1. **Abertura de Arquivo (e Associação de Arquivos)**: 
   - Um método para abrir o `OpenFileDialog`.
   - Lógica para carregar o caminho do arquivo selecionado no `PdfWebViewer.Source`.
   - O construtor ou o `App.xaml.cs` deve aceitar argumentos de linha de comando (`string[] args`) para que, se o usuário clicar duas vezes em um PDF no Windows associado a este app, ele já abra o documento direto.
2. **Assinatura Digital**: 
   - Inclua comentários ou a estrutura inicial usando a classe `X509Store` do .NET para listar os certificados do Windows (A1/A3).
   - Mostre como instanciar o `PdfSigner` do iText7.
3. **Compressão**: 
   - Estrutura base usando iText7 para ler o PDF e salvar uma versão reduzida (ex: `PdfWriter` com `CompressionLevel.BEST_COMPRESSION`).
4. **Exportação Word/Excel**: 
   - Lógica para extrair texto (usando `PdfTextExtractor` do iText7) e comentários simulando a criação via `OpenXML`.

### 🚀 O que eu preciso que você me entregue agora:
1. O conteúdo completo e atualizado do arquivo **PdfToolbox.csproj**.
2. O conteúdo de **App.xaml.cs** preparado para receber argumentos de inicialização (duplo clique do Windows).
3. O código completo, bonito e livre de erros do **MainWindow.xaml**.
4. O código correspondente (Code-Behind) do **MainWindow.xaml.cs** com todos os objetos declarados na UI devidamente referenciados e métodos de clique linkados.

Por favor, forneça os códigos em blocos separados para que eu possa apenas copiar e colar e executar `dotnet run` com sucesso.