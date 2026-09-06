using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaixALL.App.Infrastructure;

public record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool IsSuccess => ExitCode == 0;
}

public static class ProcessRunner
{
    /// <summary>
    /// Executa um processo externo de forma segura utilizando ArgumentList (evita command injection).
    /// Suporta cancelamento limpo com finalização de toda a árvore de processos filhos (evita processos órfãos).
    /// </summary>
    public static async Task<ProcessResult> RunAsync(
        string executablePath,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        IDictionary<string, string>? environmentVariables = null,
        Action<string>? onOutputLine = null,
        Action<string>? onErrorLine = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException($"O executável não foi encontrado no caminho especificado: {executablePath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        if (!string.IsNullOrEmpty(workingDirectory) && Directory.Exists(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        if (environmentVariables != null)
        {
            foreach (var kvp in environmentVariables)
            {
                startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
            }
        }

        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var outputTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null)
            {
                outputTcs.TrySetResult(true);
            }
            else
            {
                lock (outputBuilder)
                {
                    outputBuilder.AppendLine(e.Data);
                }
                onOutputLine?.Invoke(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null)
            {
                errorTcs.TrySetResult(true);
            }
            else
            {
                lock (errorBuilder)
                {
                    errorBuilder.AppendLine(e.Data);
                }
                onErrorLine?.Invoke(e.Data);
            }
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Falha ao iniciar o processo: {executablePath}");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Registra o token de cancelamento para matar a árvore inteira de processos
            await using (cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignora falhas ao tentar matar processo que já encerrou
                }
            }))
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                await Task.WhenAll(outputTcs.Task, errorTcs.Task).ConfigureAwait(false);
            }

            return new ProcessResult(
                process.ExitCode,
                outputBuilder.ToString(),
                errorBuilder.ToString());
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch { }

            throw;
        }
    }
}
