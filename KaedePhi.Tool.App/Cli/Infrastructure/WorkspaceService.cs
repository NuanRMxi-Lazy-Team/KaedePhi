using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.App.Cli.Infrastructure;

/// <summary>
/// 工作区服务，仅负责文件的复制与路径管理，不进行任何谱面序列化/反序列化。
/// </summary>
public sealed class WorkspaceService
{
    private const string ChartFileName = "chart.json";
    private readonly string _rootDir;

    public WorkspaceService()
    {
        _rootDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KaedePhi",
            "workspaces"
        );
        Directory.CreateDirectory(_rootDir);
    }

    /// <summary>
    /// 工作区根目录路径。
    /// </summary>
    public string Root => _rootDir;

    /// <summary>
    /// 列出所有工作区 ID。
    /// </summary>
    /// <returns>工作区 ID 列表</returns>
    public IEnumerable<string> List() =>
        Directory.EnumerateDirectories(_rootDir).Select(d => Path.GetFileName(d));

    /// <summary>
    /// 验证工作区 ID 的合法性，防止路径遍历攻击。
    /// </summary>
    private string ValidateAndResolveId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException(CliLocalizationString.err_workspace_id_null, nameof(id));

        // 只允许字母、数字、下划线、连字符
        if (!id.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            throw new ArgumentException(
                CliLocalizationString.err_workspace_id_invalid_chars,
                nameof(id)
            );

        var dir = Path.GetFullPath(Path.Combine(_rootDir, id));
        if (!dir.StartsWith(Path.GetFullPath(_rootDir), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                CliLocalizationString.err_workspace_id_path_traversal,
                nameof(id)
            );

        return dir;
    }

    /// <summary>
    /// 将外部谱面文件以流的方式复制到工作区目录，不做任何解析。
    /// </summary>
    public async Task LoadAsync(string id, string chartPath, CancellationToken ct = default)
    {
        var dir = ValidateAndResolveId(id);
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, ChartFileName);
        ChartProcessingValidator.ValidateInputFile(chartPath);
        await CopyFileAtomicAsync(chartPath, dest, ct);
    }

    /// <summary>
    /// 检查工作区是否存在。
    /// </summary>
    /// <param name="id">工作区 ID</param>
    /// <returns>存在则返回 true</returns>
    public bool Exists(string id) => Directory.Exists(ValidateAndResolveId(id));

    /// <summary>
    /// 返回工作区谱面文件的路径，若工作区不存在则返回 null。
    /// </summary>
    public string? GetChartPath(string id)
    {
        var dir = ValidateAndResolveId(id);
        var file = Path.Combine(dir, ChartFileName);
        return File.Exists(file) ? file : null;
    }

    /// <summary>
    /// 将工作区谱面文件以流的方式输出到目标路径，不做任何解析。
    /// </summary>
    public async Task SaveAsync(string id, string outputPath, CancellationToken ct = default)
    {
        var file =
            GetChartPath(id)
            ?? throw new InvalidOperationException(
                string.Format(CliLocalizationString.err_workspace_not_found, id)
            );
        var fullOutputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        await CopyFileAtomicAsync(file, fullOutputPath, ct);
    }

    /// <summary>
    /// 清空指定工作区或所有工作区。
    /// </summary>
    /// <param name="id">工作区 ID，为 null 时清空所有</param>
    public void Clear(string? id = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            if (Directory.Exists(_rootDir))
                Directory.Delete(_rootDir, true);
            Directory.CreateDirectory(_rootDir);
            return;
        }

        var dir = ValidateAndResolveId(id);
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
    }

    private static async Task CopyFileAtomicAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken ct
    )
    {
        var temporaryPath = destinationPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (
                var src = new FileStream(
                    sourcePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 65536,
                    useAsync: true
                )
            )
            await using (
                var dst = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 65536,
                    useAsync: true
                )
            )
            {
                await src.CopyToAsync(dst, ct);
                await dst.FlushAsync(ct);
            }

            ct.ThrowIfCancellationRequested();
            File.Move(temporaryPath, destinationPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
