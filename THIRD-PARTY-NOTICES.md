# Licenças de Terceiros e Avisos Legais (Third-Party Notices)

O **BaixALL** utiliza e interage com softwares de terceiros e bibliotecas de código aberto. Esta página lista as atribuições, avisos de direitos autorais e licenças de cada componente.

---

## Índice

1. [yt-dlp](#1-yt-dlp)
2. [FFmpeg e ffprobe](#2-ffmpeg-e-ffprobe)
3. [Deno](#3-deno)
4. [CommunityToolkit.Mvvm](#4-communitytoolkitmvvm)
5. [Microsoft .NET Runtime & Libraries](#5-microsoft-net-runtime--libraries)

---

## 1. yt-dlp

- **Projeto:** [yt-dlp/yt-dlp](https://github.com/yt-dlp/yt-dlp)
- **Função:** Linha de comando para extração de metadados e download de mídias do YouTube.
- **Licença:** The Unlicense (Domínio Público)

```
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

## 2. FFmpeg e ffprobe

- **Projeto:** [FFmpeg](https://ffmpeg.org/) / [yt-dlp/FFmpeg-Builds](https://github.com/yt-dlp/FFmpeg-Builds)
- **Função:** Processamento, multiplexação (merge de fluxos de áudio e vídeo separados), conversão de formatos de áudio (MP3/M4A/Opus) e inspeção técnica de containers de mídia (`ffprobe`).
- **Build Utilizado:** `ffmpeg-master-latest-win64-gpl.zip` (yt-dlp FFmpeg-Builds, compilado com `--enable-gpl`)
- **Licença do Build:** GNU General Public License v3.0 (GPLv3)

> [!NOTE]
> O FFmpeg e o ffprobe são executados como processos autônomos e independentes através da linha de comando, sem vinculação estática ou dinâmica no código-fonte do BaixALL. O BaixALL respeita as diretrizes da GPL para ferramentas externas invocadas via subprocesso.

```
                    GNU GENERAL PUBLIC LICENSE
                       Version 3, 29 June 2007

 Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 Everyone is permitted to copy and distribute verbatim copies
 of this license document, but changing it is not allowed.

                            Preamble

  The GNU General Public License is a free, copyleft license for
software and other kinds of works.

  The licenses for most software and other practical works are designed
to take away your freedom to share and change the works.  By contrast,
the GNU General Public License is intended to guarantee your freedom to
share and change all versions of a program--to make sure it remains free
software for all its users.  We, the Free Software Foundation, use the
GNU General Public License for most of our software; it applies also to
any other work released this way by its authors.  You can apply it to
your programs, too.

  When we speak of free software, we are referring to freedom, not
price.  Our General Public Licenses are designed to make sure that you
have the freedom to distribute copies of free software (and charge for
them if you wish), that you receive source code or can get it if you
want it, that you can change the software or use pieces of it in new
free programs, and that you know you can do these things.

  To protect your rights, we need to prevent others from denying you
these rights or asking you to surrender the rights.  Therefore, you have
certain responsibilities if you distribute copies of the software, or if
you modify it: responsibilities to respect the freedom of others.

  For example, if you distribute copies of such a program, whether
gratis or for a fee, you must pass on to the recipients the same
freedoms that you received.  You must make sure that they, too, receive
or can get the source code.  And you must show them these terms so they
know their rights.

  Developers that use the GNU GPL protect your rights with two steps:
(1) assert copyright on the software, and (2) offer you this License
giving you legal permission to copy, distribute and/or modify it.

  For the developers' and authors' protection, the GPL clearly explains
that there is no warranty for this free software.  For both users' and
authors' sake, the GPL requires that modified versions be marked as
changed, so that their problems will not be attributed erroneously to
authors of previous versions.

  The complete text of the GNU General Public License version 3 can be
obtained at: <https://www.gnu.org/licenses/gpl-3.0.txt>
Código-fonte do FFmpeg: <https://ffmpeg.org/download.html>
Código-fonte dos scripts de build: <https://github.com/yt-dlp/FFmpeg-Builds>
```

---

## 3. Deno

- **Projeto:** [denoland/deno](https://github.com/denoland/deno)
- **Função:** Runtime JavaScript/TypeScript moderno e seguro, utilizado opcionalmente pelo yt-dlp como interpretador JS para resolução de desafios de assinatura e streaming.
- **Licença:** MIT License
- **Copyright:** Copyright (c) 2018-2026 the Deno authors.

```
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

## 4. CommunityToolkit.Mvvm

- **Projeto:** [CommunityToolkit/dotnet](https://github.com/CommunityToolkit/dotnet)
- **Função:** Biblioteca MVVM de alto desempenho para .NET (ObservableObject, RelayCommand, geradores de código fonte).
- **Licença:** MIT License
- **Copyright:** Copyright (c) .NET Foundation and Contributors.

```
MIT License

Copyright (c) .NET Foundation and Contributors.
All rights reserved.

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

## 5. Microsoft .NET Runtime & Libraries

- **Projeto:** [.NET Platform](https://github.com/dotnet)
- **Função:** Plataforma de execução .NET e bibliotecas base da classe (BCL, WPF, System.Text.Json).
- **Licença:** MIT License
- **Copyright:** Copyright (c) .NET Foundation and Contributors.

```
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

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
