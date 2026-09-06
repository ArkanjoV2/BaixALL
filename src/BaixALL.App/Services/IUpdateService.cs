using System.Threading;
using System.Threading.Tasks;

namespace BaixALL.App.Services;

public record UpdateCheckResult(
    string ToolName,
    string CurrentVersion,
    string LatestVersion,
    bool HasUpdate,
    string ReleaseUrl);

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckYtDlpUpdateAsync(CancellationToken ct = default);
    Task<UpdateCheckResult> CheckDenoUpdateAsync(CancellationToken ct = default);
    Task<bool> UpdateToolAsync(string toolName, CancellationToken ct = default);
}
