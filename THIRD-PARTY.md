# Componentes externos

O código usa bibliotecas do .NET Framework e APIs do Windows, instaladas pelo usuário como parte do sistema/plataforma.

As funções de vídeo e áudio invocam executáveis externos que não estão incluídos neste repositório nem no pacote básico:

- yt-dlp: https://github.com/yt-dlp/yt-dlp — consulte a licença e os avisos da distribuição escolhida.
- FFmpeg e FFprobe: https://ffmpeg.org/ — licença depende da configuração do build; builds GPL possuem obrigações próprias.
- Deno: https://github.com/denoland/deno — consulte os avisos e a licença upstream.

Baixe os componentes nos projetos oficiais. A licença MIT do Nocturnal não substitui as licenças desses componentes. Redistribuir um pacote com esses executáveis exige conferir as condições dos builds específicos.

O executável do Internet Download Manager, suas DLLs, extensões e ícones não fazem parte do projeto.
