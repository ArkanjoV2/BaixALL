# Licenças de Terceiros e Avisos Legais (Third-Party Notices)

O **BaixALL** utiliza, interage e se integra a softwares de terceiros e bibliotecas de código aberto. Esta página documenta detalhadamente as atribuições, avisos de direitos autorais, configurações de compilação e conformidade legal com as licenças de cada componente.

---

## Índice

1. [Visão Geral da Arquitetura e Limites de Processo](#1-visão-geral-da-arquitetura-e-limites-de-processo)
2. [yt-dlp](#2-yt-dlp)
3. [FFmpeg e ffprobe (GPLv3)](#3-ffmpeg-e-ffprobe-gplv3)
4. [Deno](#4-deno)
5. [Bibliotecas NuGet (.NET)](#5-bibliotecas-nuget-net)
6. [Microsoft .NET Runtime & Libraries](#6-microsoft-net-runtime--libraries)
7. [Status de Conformidade, SmartScreen e Obrigações Legais](#7-status-de-conformidade-smartscreen-e-obrigações-legais)

---

## 1. Visão Geral da Arquitetura e Limites de Processo

O **BaixALL** foi concebido sob uma estrita arquitetura de processos desacoplados:
- O executável principal (`BaixALL.exe`) é uma aplicação autônoma em C# e .NET 10.
- As ferramentas externas (`yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe` e `deno.exe`) **não são linkadas estática ou dinamicamente** ao código-fonte ou aos binários do BaixALL (não há bibliotecas nativas C/C++ compartilhadas, DLLs de terceiros embutidas no processo gerenciado ou P/Invoke direto para bibliotecas das ferramentas).
- Toda interação com ferramentas externas ocorre exclusivamente no nível de sistema operacional através da criação de processos independentes via `System.Diagnostics.Process`, passagem de parâmetros rigorosamente estruturada por `ProcessStartInfo.ArgumentList` e leitura assíncrona dos fluxos de dados padrão (`stdout` / `stderr`).
- Nos artefatos padrão de distribuição do BaixALL (`BaixALL-Setup-1.0.0.exe` e `BaixALL-1.0.0-win-x64.zip`), **os binários de terceiros não vêm pré-empacotados**. O BaixALL realiza o download sob demanda das ferramentas oficiais diretamente a partir de seus repositórios no GitHub no primeiro uso ou quando solicitado pelo usuário.

---

## 2. yt-dlp

- **Projeto Upstream:** [yt-dlp/yt-dlp](https://github.com/yt-dlp/yt-dlp)
- **Função:** Linha de comando para extração de metadados em formato JSON estruturado e download dos fluxos de áudio e vídeo.
- **Licença:** The Unlicense (Dedicação ao Domínio Público)

```text
This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.

In jurisdictions that recognize copyright laws, the author or authors
of this software dedicate any and all copyright interest in the
software to the public domain. We make this dedication for the benefit
of the public at large and to the detriment of our heirs and
successors. We intend this dedication to be an overt act of
relinquishment in perpetuity of all present and future rights to this
software under copyright law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to <http://unlicense.org/>
```

---

## 3. FFmpeg e ffprobe (GPLv3)

- **Projeto Upstream:** [FFmpeg](https://ffmpeg.org/)
- **Fornecedor dos Builds:** [yt-dlp/FFmpeg-Builds](https://github.com/yt-dlp/FFmpeg-Builds)
- **Pacote Referenciado:** `ffmpeg-master-latest-win64-gpl.zip` (contendo `ffmpeg.exe` e `ffprobe.exe`)
- **Versão:** `ffmpeg version N-126435-gf93cd72dde-20260906 Copyright (c) 2000-2026 the FFmpeg developers`
- **Compilador:** GCC 15.2.0 (crosstool-NG 1.28.0.23_185f348) para `x86_64-w64-mingw32`
- **Configuração de Build Oficial:**
  ```text
  configuration: --prefix=/ffbuild/prefix --pkg-config-flags=--static --pkg-config=pkg-config
  --cross-prefix=x86_64-w64-mingw32- --arch=x86_64 --target-os=mingw32 --enable-gpl --enable-version3
  --disable-debug --disable-w32threads --enable-pthreads --enable-iconv --enable-zlib --enable-libxml2
  --enable-libvmaf --enable-fontconfig --enable-libharfbuzz --enable-libfreetype --enable-libfribidi
  --enable-vulkan --enable-libvorbis --enable-gmp --enable-lzma --enable-liblcevc-dec --enable-opencl
  --enable-amf --enable-libaom --enable-libaribb24 --enable-avisynth --enable-chromaprint
  --enable-libdav1d --enable-libdavs2 --enable-libdvdread --enable-libdvdnav --enable-ffnvcodec
  --enable-cuda-llvm --enable-frei0r --enable-libgme --enable-libkvazaar --enable-libaribcaption
  --enable-libass --enable-libbluray --enable-libjxl --enable-libmp3lame --enable-libopus
  --enable-libplacebo --enable-librist --enable-libssh --enable-libtheora --enable-libvpx
  --enable-libwebp --enable-libzmq --enable-lv2 --enable-libvpl --enable-openal --enable-liboapv
  --enable-libopencore-amrnb --enable-libopencore-amrwb --enable-libopenh264 --enable-libopenjpeg
  --enable-libopenmpt --enable-librav1e --enable-librubberband --enable-schannel --enable-sdl2
  --enable-libsnappy --enable-libsoxr --enable-libsrt --enable-libsvtav1 --enable-libtwolame
  --enable-libuavs3d --enable-vaapi --enable-libvidstab --enable-libvvenc --enable-libx264
  --enable-libx265 --enable-libxavs2 --enable-libxvid --enable-libzimg --enable-libzvbi
  ```
- **Licenciamento do Build:** **GNU General Public License versão 3.0 (GPLv3)**, decorrente da inclusão das flags `--enable-gpl` e `--enable-version3`, bem como da integração de bibliotecas sob licença GPL (ex.: `x264`, `x265`, `xvid`, `libvidstab`).

### 📦 Disponibilização de Código-Fonte Correspondente (GPLv3 - Seção 6)

Em cumprimento aos termos da Seção 6 da GPLv3 ("Conveying Non-Source Forms"), declara-se expressamente que:
1. O código-fonte original completo do FFmpeg na versão exata utilizada está disponível em:
   - Repositório oficial: <https://github.com/FFmpeg/FFmpeg>
   - Commit correspondente: `gf93cd72dde` (<https://github.com/FFmpeg/FFmpeg/commit/f93cd72dde>)
   - Portal de download oficial: <https://ffmpeg.org/download.html>
2. O código-fonte completo dos scripts de compilação, receitas de build e patches utilizados para gerar este binário win64 específico está publicamente disponível em:
   - Repositório de builds: <https://github.com/yt-dlp/FFmpeg-Builds>
   - Scripts de configuração e dependências: <https://github.com/yt-dlp/FFmpeg-Builds/tree/master/scripts.d>
3. Qualquer usuário tem o direito irrestrito de obter, inspecionar, modificar e recompilar o código-fonte do FFmpeg e das ferramentas associadas de acordo com os termos da GPLv3.

```text
                    GNU GENERAL PUBLIC LICENSE
                       Version 3, 29 June 2007

 Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 Everyone is permitted to copy and distribute verbatim copies
 of this license document, but changing it is not allowed.

 (Texto integral da licença disponível em: https://www.gnu.org/licenses/gpl-3.0.txt)
```

---

## 4. Deno

- **Projeto Upstream:** [denoland/deno](https://github.com/denoland/deno)
- **Função:** Runtime JavaScript/TypeScript de alto desempenho, invocado pelo yt-dlp para resolução de desafios de assinatura em streams do YouTube.
- **Licença:** MIT License
- **Copyright:** Copyright (c) 2018-2026 the Deno authors.
- **Código-fonte:** <https://github.com/denoland/deno>

```text
MIT License

Copyright (c) 2018-2026 the Deno authors.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 5. Bibliotecas NuGet (.NET)

Todas as dependências NuGet utilizadas no BaixALL são de código aberto sob licenças permissivas:

| Pacote | Versão | Licença | Copyright / Autor | Finalidade |
| :--- | :--- | :--- | :--- | :--- |
| **CommunityToolkit.Mvvm** | 8.4.2 | MIT | .NET Foundation and Contributors | Padrão MVVM, geradores de código, ObservableObject |
| **Microsoft.NET.Test.Sdk** | 17.14.1 | MIT | Microsoft Corporation | Suíte de testes (apenas em desenvolvimento) |
| **xunit** | 2.9.3 | Apache 2.0 / MIT | .NET Foundation / xUnit.net | Framework de testes (apenas em desenvolvimento) |
| **xunit.runner.visualstudio** | 3.1.4 | Apache 2.0 / MIT | .NET Foundation / xUnit.net | Execução de testes no Visual Studio/CLI |
| **coverlet.collector** | 6.0.4 | MIT | Toni Solarin-Sodara | Coleta de cobertura de código (apenas em desenvolvimento) |

---

## 6. Microsoft .NET Runtime & Libraries

- **Projeto:** [.NET Platform](https://github.com/dotnet)
- **Função:** Plataforma de execução .NET 10 LTS e bibliotecas básicas (WPF, System.Text.Json, BCL).
- **Licença:** MIT License
- **Copyright:** Copyright (c) .NET Foundation and Contributors.
- **Código-fonte:** <https://github.com/dotnet/runtime>

---

## 7. Status de Conformidade, SmartScreen e Obrigações Legais

### 7.1 Separação Arquitetural e Limite de Processos
- **Status:** **Plenamente atendido.** O BaixALL não vincula, compila ou linka nenhuma biblioteca externa ao seu processo principal.
- As ferramentas de linha de comando operam como subprocessos independentes invocados por linha de comando.

### 7.2 Esclarecimento Rigoroso sobre Conformidade com a GPLv3
- A inclusão do texto da licença GPLv3 e a indicação de links nesta documentação representam obrigações formais necessárias, **porém não comprovam, por si sós, conformidade total** com as regras de licenciamento.
- **Modelo de Distribuição Padrão (Download sob Demanda):** Nos pacotes oficiais `BaixALL-Setup-1.0.0.exe` e `BaixALL-1.0.0-win-x64.zip`, os binários do FFmpeg **não são redistribuídos**. O usuário os obtém diretamente do repositório upstream oficial ao usar o BaixALL.
- **Regra Bloqueante para Distribuições Offline Futuras:** Caso venha a ser produzido um pacote de distribuição *offline* contendo os binários compilados do FFmpeg pré-embutidos, será **obrigatoriamente bloqueante para publicação** a inclusão direta do código-fonte correspondente ou de uma oferta formal por escrito (Seção 6b da GPLv3), acompanhada dos scripts e instruções exatas de build para reprodução dos binários.

### 7.3 Esclarecimento sobre Windows SmartScreen e Assinatura Digital
- **Avisos de Reputação:** Aplicativos novos distribuídos na internet podem exibir o alerta do Windows Defender SmartScreen (*"O Windows protegeu o seu computador"*).
- **Certificados Digitais (Authenticode):** É incorreto afirmar que certificados comerciais (mesmo do tipo EV ou OV) eliminam imediatamente qualquer aviso do SmartScreen em softwares novos. No ecossistema moderno do Windows, a reputação do SmartScreen é construída de maneira progressiva com base no volume estatístico de downloads benignos e telemetria acumulada ao longo do tempo.
- **Políticas de Integridade:**
  - O BaixALL **nunca desabilita** o Windows Defender, o SmartScreen ou quaisquer mecanismos de segurança do sistema operacional.
  - O projeto **não utiliza certificados fictícios ou autoassinados**, pois estes não possuem cadeia de confiança pública e não possuem validade para distribuição externa.
  - A aquisição de uma assinatura digital Authenticode formal poderá ser tratada futuramente pelo mantenedor caso decida contratar esse serviço.
