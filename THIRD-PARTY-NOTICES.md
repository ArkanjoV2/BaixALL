# Licenças de Terceiros e Avisos Legais (Third-Party Notices)

O **BaixALL** utiliza, interage e se integra a softwares de terceiros e bibliotecas de código aberto. Esta página documenta detalhadamente as atribuições, avisos de direitos autorais, configurações de build e conformidade com as licenças de cada componente.

---

## Índice

1. [Visão Geral da Arquitetura e Limites de Processo](#1-visão-geral-da-arquitetura-e-limites-de-processo)
2. [yt-dlp](#2-yt-dlp)
3. [FFmpeg e ffprobe (GPLv3)](#3-ffmpeg-e-ffprobe-gplv3)
4. [Deno](#4-deno)
5. [Bibliotecas NuGet (.NET)](#5-bibliotecas-nuget-net)
6. [Microsoft .NET Runtime & Libraries](#6-microsoft-net-runtime--libraries)
7. [Status de Conformidade e Pendências Pré-Publicação](#7-status-de-conformidade-e-pendências-pré-publicação)

---

## 1. Visão Geral da Arquitetura e Limites de Processo

O **BaixALL** foi desenhado com arquitetura de processos desacoplados:
- O executável principal (`BaixALL.exe`) é uma aplicação autônoma em C# e .NET 10.
- As ferramentas externas (`yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe` e `deno.exe`) **não são linkadas estática ou dinamicamente** ao código-fonte ou aos binários do BaixALL.
- A comunicação entre o BaixALL e as ferramentas externas ocorre exclusivamente no nível do sistema operacional através de processos independentes (`System.Diagnostics.Process`), passagem de argumentos via `ProcessStartInfo.ArgumentList` e leitura assíncrona de fluxos de entrada/saída (`stdout` / `stderr`).
- No pacote de instalação padrão do BaixALL (`BaixALL-Setup-1.0.0-rc.1.exe`), os binários de terceiros não são embutidos; eles são baixados pelo usuário através do `DependencyManager` a partir de seus repositórios oficiais. Caso um pacote redistribuível completo futuro venha a incluir os binários pré-baixados, as obrigações da Seção 3 e Seção 6 da GPLv3 são plenamente satisfeitas pelas informações e ofertas de código-fonte aqui documentadas.

---

## 2. yt-dlp

- **Projeto:** [yt-dlp/yt-dlp](https://github.com/yt-dlp/yt-dlp)
- **Função:** Linha de comando para extração de metadados em JSON estruturado e download de fluxos de áudio e vídeo do YouTube.
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
- **Pacote Utilizado:** `ffmpeg-master-latest-win64-gpl.zip` (contendo `ffmpeg.exe` e `ffprobe.exe`)
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

Em cumprimento aos termos da Seção 6 da GPLv3 ("Conveying Non-Source Forms"), informa-se expressamente que:
1. O código-fonte original completo do FFmpeg na versão exata utilizada está disponível em:
   - Repositório oficial: <https://github.com/FFmpeg/FFmpeg>
   - Commit correspondente: `gf93cd72dde` (<https://github.com/FFmpeg/FFmpeg/commit/f93cd72dde>)
   - Portal de download oficial: <https://ffmpeg.org/download.html>
2. O código-fonte completo dos scripts de build, receitas de compilação, patches e automações utilizadas para gerar este binário win64 específico está publicamente disponível em:
   - Repositório de builds: <https://github.com/yt-dlp/FFmpeg-Builds>
   - Scripts de configuração e dependências: <https://github.com/yt-dlp/FFmpeg-Builds/tree/master/scripts.d>
3. Qualquer usuário ou terceiro tem o direito irrestrito de obter, inspecionar, modificar e recompilar o código-fonte do FFmpeg e das ferramentas associadas de acordo com os termos da GPLv3.

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

- **Projeto:** [denoland/deno](https://github.com/denoland/deno)
- **Função:** Runtime JavaScript/TypeScript de alto desempenho e seguro, invocado opcionalmente pelo yt-dlp para resolução de desafios de assinatura em streams do YouTube.
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

## 7. Status de Conformidade e Pendências Pré-Publicação

1. **Separação de Processos:** Plenamente atendida. Não há links estáticos/dinâmicos com o FFmpeg.
2. **Atribuições e Textos de Licença:** Plenamente atendidos e consolidados nesta documentação.
3. **Oferta de Código-Fonte:** Plenamente atendida com a inclusão de links permanentes para os repositórios upstream e commits correspondentes.
4. **Pendências Identificadas:**
   - Caso uma distribuição empacotada "tudo-em-um" (offline bundle contendo os binários pré-baixados de FFmpeg/yt-dlp) seja criada no futuro, este arquivo `THIRD-PARTY-NOTICES.md` deverá acompanhar obrigatoriamente a raiz dessa distribuição.
