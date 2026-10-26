<img src="assets/logo.png" width="88" alt="Logo do Nocturnal">

# Nocturnal

Uma alternativa open source ao Internet Download Manager (IDM) para Windows, com tema escuro e sem período de avaliação. Baixa arquivos, organiza a fila e permite baixar vídeos em MP4 ou extrair áudio em MP3.

![Nocturnal](docs/desktop.png)

## Como usar

Clique em **Adicionar**, cole o endereço e escolha entre arquivo original, vídeo MP4 ou áudio MP3. Para mídia, selecione a resolução ou a qualidade do áudio e confirme onde salvar.

Os novos downloads são separados em pastas por tipo: Música, Vídeos, Documentos, Compactados, Programas e Outros. Você pode mudar o destino na hora de salvar.

A janela de progresso abre ao iniciar o download. Nela, dá para pausar, continuar, limitar a velocidade e ver os detalhes das conexões. Em **Opções**, você escolhe quantos downloads podem rodar ao mesmo tempo. A retomada depende do suporte do servidor.

## Instalação

No Windows 10 ou 11, com .NET Framework 4.8 instalado, abra o PowerShell na pasta do projeto e execute:

```powershell
.\scripts\build.ps1
.\scripts\install.ps1
```

O primeiro comando compila o aplicativo. O segundo instala o Nocturnal, cria os atalhos e configura a integração com a extensão. Feche o aplicativo antes de atualizar.

### Vídeos e MP3

Para usar essas funções, coloque os executáveis abaixo em uma pasta chamada `tools`, ao lado de `Nocturnal.exe`:

- `yt-dlp.exe` — [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- `ffmpeg.exe` e `ffprobe.exe` — [FFmpeg Builds](https://github.com/yt-dlp/FFmpeg-Builds)
- `deno.exe` — [Deno](https://github.com/denoland/deno)

Essas ferramentas são baixadas separadamente. Para baixar arquivos comuns por HTTP/HTTPS, elas não são necessárias.

## Extensão para Firefox

A extensão adiciona **Baixar com Nocturnal** ao menu do botão direito de links, vídeos e áudios. Depois de escolher essa opção, clique em **Abrir no Nocturnal** e confirme a abertura no Firefox.

Para testar a extensão:

1. Abra `about:debugging` no Firefox e entre em **Este Firefox**.
2. Clique em **Carregar extensão temporária**.
3. Escolha o arquivo `extension-firefox/manifest.json`.

Essa instalação dura até o navegador ser reiniciado. A instalação permanente exige uma versão assinada pela Mozilla.

## Como foi feito

O aplicativo foi escrito em **C# com Windows Forms**. O motor de arquivos usa HTTP/HTTPS e divide o download em até oito conexões quando o servidor permite. A fila é salva localmente.

Vídeos e áudio usam **yt-dlp e FFmpeg**, com **Deno** para os sites que precisam de execução de JavaScript. As resoluções disponíveis dependem da origem; alguns sites e links que exigem login podem não funcionar.

A extensão foi feita em **JavaScript, HTML e CSS** e conversa com o aplicativo pelo protocolo `nocturnal://`.

## Licença

O código está sob a [licença MIT](LICENSE). A foto usada na marca não está incluída nessa licença. As ferramentas externas seguem suas [próprias licenças](THIRD-PARTY.md).
