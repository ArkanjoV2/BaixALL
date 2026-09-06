using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public interface IDependencyManager
{
    IReadOnlyList<DependencyItem> GetDependencies();
    Task<bool> CheckDependenciesAsync();
    bool AreAllDependenciesInstalled();
    Task<bool> EnsureAllDependenciesAsync(IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default);
    Task<bool> UpdateDependencyAsync(string toolName, IProgress<(string ToolName, double Progress, string Status)>? progress = null, CancellationToken ct = default);
    string GetYtDlpPath();
    string GetFFmpegPath();
    string GetFFprobePath();
    string GetDenoPath();
}
