<div align="center">
  <img src="app_icon.png" width="128" height="128" alt="UPDF Logo">
  
  # UPDF FCS (União PDF)
  **A ferramenta definitiva para gerenciamento e edição de PDFs em Windows.**
</div>

---

O **UPDF FCS** é um aplicativo desktop rápido, leve e profissional desenvolvido em C# (.NET 8 e WPF) projetado para resolver todos os problemas do dia a dia com documentos PDF, sem complicações.

## 🚀 Funcionalidades

- **Anotações Visuais (Drag & Drop):** Insira textos personalizados e carimbe imagens (como assinaturas escaneadas e logos) livremente pelas páginas.
- **Assinatura Digital Profissional:** Assine documentos com valor legal utilizando certificados digitais `.pfx` (e-CPF/e-CNPJ), com suporte a estampas visíveis ou invisíveis.
- **Organização Inteligente:** Separe, junte, rotacione, apague ou insira páginas em branco usando um painel visual prático.
- **Extração Avançada:** Exporte páginas específicas como novos PDFs, como arquivos de imagem (JPG/PNG) ou extraia os dados em texto estruturado direto para planilhas do **Excel (.xlsx)**.
- **Auto-Atualizável:** O sistema varre automaticamente o GitHub em busca de novas versões, mantendo seu programa sempre atualizado.

## 📦 Como Instalar

1. Acesse a aba de [Releases](https://github.com/fernandoc-souza/UPDF/releases) do repositório.
2. Baixe o `UPDF_v<versão>.zip` da versão mais recente.
3. Extraia o conteúdo para uma pasta no seu computador.
4. Execute o arquivo `Instalador.bat` (ele pedirá permissão de Administrador para criar os atalhos e registrar o UPDF).
5. Pronto! O atalho **UPDF** estará na sua Área de Trabalho e Menu Iniciar.

O zip contém:

| Arquivo | Para que serve |
|---|---|
| `UPDF.exe` | O programa |
| `Instalador.bat` | Instala, cria atalhos e registra o UPDF para abrir PDFs |
| `Desinstalador.bat` | Remove tudo: arquivos, atalhos e registro |
| `Reparar_AbrirCom.bat` | Conserta o menu "Abrir com" sem reinstalar |
| `app_icon.png` | Ícone usado pelos atalhos |

### O UPDF não aparece em "Abrir com"

Instaladores até a versão 1.3.3 não criavam a chave `Applications\UPDF.exe`, que é
justamente o que faz um programa aparecer na lista de "Abrir com". Em quem já tinha
escolhido o UPDF manualmente uma vez, o Windows guardava essa preferência e o menu
funcionava; numa máquina nova, não aparecia nada.

Numa instalação já existente, rode o **`Reparar_AbrirCom.bat`** como Administrador —
ele só corrige o registro, não reinstala nem mexe no seu leitor de PDF padrão.

Para deixar o UPDF como leitor padrão: botão direito num PDF → **Abrir com** →
**Escolher outro aplicativo** → **UPDF** → **Sempre**. No Windows 10/11 só o próprio
usuário pode definir o padrão; nenhum instalador consegue fazer isso por você.

## 🕒 Carimbo de tempo e LTV (assinatura digital)

A partir da versão 1.3.3 o UPDF sabe aplicar **carimbo de tempo RFC 3161** na assinatura,
gerando um documento **CAdES** com validade de longo prazo. O recurso vem **desligado por
padrão**: sem configurar nada, assinar não faz nenhuma chamada de rede e o resultado é o
mesmo de sempre.

As opções ficam em `%LOCALAPPDATA%\UPDF\config.json`, criado na primeira execução:

```json
{
  "tsaHabilitado": false,
  "tsaUrl": "",
  "tsaUsuario": "",
  "tsaSenha": "",
  "ltvHabilitado": true,
  "tsaTimeoutSegundos": 15
}
```

Para ligar o carimbo, ponha `"tsaHabilitado": true` e preencha `tsaUrl`:

> Só uma TSA **credenciada ICP-Brasil** produz assinatura **AD-RT** com validade legal no
> Brasil — normalmente a sua Autoridade Certificadora fornece o endereço (e, se for o caso,
> usuário e senha). TSAs públicas gratuitas como `http://timestamp.digicert.com` também
> funcionam e provam a data, mas **não são credenciadas ICP-Brasil**.

Se a TSA estiver ligada e não responder, o app assina sem carimbo em vez de falhar, e avisa na tela.

O `ltvHabilitado` embute os dados de revogação (**OCSP/CRL**) do seu certificado e fica ligado.
Quando esses endereços estão fora do ar ou a máquina está offline, a assinatura sai normalmente
sem eles.

> Já tem um `config.json` de uma instalação anterior? O arquivo existente é respeitado — o novo
> padrão só vale para quem ainda não tem um. Para desligar o carimbo num config já criado,
> edite o arquivo e troque `"tsaHabilitado"` para `false`.

## ✍️ Múltiplas assinaturas no mesmo documento

É possível assinar o mesmo PDF várias vezes — PF e PJ, dois engenheiros, etc. — inclusive na
mesma página. Cada assinatura entra como uma revisão incremental, então as anteriores continuam
válidas. Já as operações que reescrevem o arquivo (comprimir, anotar, reorganizar páginas)
invalidam as assinaturas existentes, e o app pede confirmação antes de seguir.

## 💻 Tecnologias Utilizadas

- **C# / .NET 8 (WPF):** Interface moderna e alta performance.
- **iText7:** Motor super robusto de manipulação de PDF, assinaturas e criptografia.
- **WebView2 (Microsoft Edge):** Renderização de documentos nativa em alta definição.
- **ClosedXML:** Geração nativa de planilhas Excel.

## 📄 Licença
Distribuído "as is" (como está) para uso pessoal e profissional. Consulte o código-fonte para mais detalhes sobre as bibliotecas de terceiros utilizadas.
