using System.Diagnostics;
using KaedePhi.Tool.Common;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KaedePhi.Tool.App.Config;

/// <summary>
/// 应用配置服务：CLI 与 GUI 共用的唯一配置入口，配置文件统一存放于本机应用数据目录。
/// </summary>
public sealed class AppConfigService
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// 进程内共享实例。
    /// </summary>
    public static AppConfigService Instance { get; } = new();

    /// <summary>
    /// 当前配置。
    /// </summary>
    public AppConfig Config { get; private set; }

    /// <summary>
    /// 配置文件完整路径。
    /// </summary>
    public string ConfigPath { get; }

    private AppConfigService()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KaedePhi",
            "config"
        );
        Directory.CreateDirectory(configDir);
        ConfigPath = Path.Combine(configDir, "config.yaml");

        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        Config = Load();
    }

    private AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var yaml = File.ReadAllText(ConfigPath);
                var config = _deserializer.Deserialize<AppConfig>(yaml);
                if (config is null)
                    throw new FormatException("配置文件为空。");
                Validate(config);
                return config;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppConfigService] Failed to load config: {ex.Message}");
            TryBackupInvalidConfig();
        }

        var defaults = new AppConfig();
        Save(defaults);
        return defaults;
    }

    /// <summary>
    /// 将当前配置写入配置文件。
    /// </summary>
    public bool Save()
    {
        return Save(Config);
    }

    /// <summary>
    /// 将配置重置为默认值并写入配置文件。
    /// </summary>
    public bool ResetToDefaults()
    {
        Config = new AppConfig();
        return Save();
    }

    private bool Save(AppConfig config)
    {
        try
        {
            Validate(config);
            var yaml = _serializer.Serialize(config);
            var temporaryPath = ConfigPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, yaml);
                File.Move(temporaryPath, ConfigPath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppConfigService] Failed to save config: {ex.Message}");
            return false;
        }
    }

    private static void Validate(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.LogLevel > 4)
            throw new ArgumentOutOfRangeException(nameof(config.LogLevel));
        if (config.MaxLogFiles is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(config.MaxLogFiles));

        ValidateTool(config.Unbind);
        ValidateTool(config.LayerMerge);
        ValidateTool(config.Cut);
        if (config.Fit is null || config.Render is null || config.Convert is null)
            throw new FormatException("配置文件缺少必要的配置段。");
        ChartProcessingValidator.ValidateTolerance(config.Fit.Tolerance);
        ChartProcessingValidator.ValidatePrecision(config.Render.SamplesPerEvent);
        if (
            config.Render.PixelsPerBeat <= 0
            || config.Render.ChannelWidth <= 0
            || config.Render.BeatSubdivisions <= 0
            || config.Render.RangeSamplesPerEvent <= 0
            || !double.IsFinite(config.Render.RangePaddingRatio)
            || config.Render.RangePaddingRatio < 0
            || !double.IsFinite(config.Render.SegmentGroupTolerance)
            || config.Render.SegmentGroupTolerance < 0
            || !double.IsFinite(config.Render.MinValueRangeHalf)
            || config.Render.MinValueRangeHalf < 0
            || !double.IsFinite(config.Render.MinValueRangeHalfRatio)
            || config.Render.MinValueRangeHalfRatio < 0
        )
            throw new ArgumentOutOfRangeException(nameof(config.Render));
        ValidateConvert(config.Convert);
    }

    private static void ValidateTool(ToolDefaultsConfig? config)
    {
        if (config is null)
            throw new FormatException("配置文件缺少工具配置段。");
        ChartProcessingValidator.ValidatePrecision(config.Precision);
        ChartProcessingValidator.ValidateTolerance(config.Tolerance);
    }

    private static void ValidateConvert(ConvertDefaultsConfig config)
    {
        ChartProcessingValidator.ValidatePrecision(config.PeUnsupportedEasingPrecision);
        ChartProcessingValidator.ValidatePrecision(config.PeMisalignedXyEventPrecision);
        ChartProcessingValidator.ValidatePrecision(config.PeAlphaCutPrecision);
        ChartProcessingValidator.ValidatePrecision(config.PeSpeedCutPrecision);
        ChartProcessingValidator.ValidatePrecision(config.PhigrosEasingPrecision);
        ChartProcessingValidator.ValidatePrecision(config.PhigrosMisalignedXyEventPrecision);
        ChartProcessingValidator.ValidatePrecision(config.PhigrosAlphaCutPrecision);
        ChartProcessingValidator.ValidatePrecision(config.PhigrosSpeedCutPrecision);
        ChartProcessingValidator.ValidatePrecision(config.UnbindPrecision);
        ChartProcessingValidator.ValidatePrecision(config.MultiLayerMergePrecision);
        ChartProcessingValidator.ValidateTolerance(config.PeAlphaCutTolerance);
        ChartProcessingValidator.ValidateTolerance(config.PeSpeedCutTolerance);
        ChartProcessingValidator.ValidateTolerance(config.UnbindTolerance);
        ChartProcessingValidator.ValidateTolerance(config.MultiLayerMergeTolerance);
        ChartProcessingValidator.ValidateTolerance(config.PhigrosAlphaCutTolerance);
        if (
            !double.IsFinite(config.PeTrailingBeatPadding)
            || config.PeTrailingBeatPadding < 0
            || !float.IsFinite(config.PhigrosDefaultBpm)
            || config.PhigrosDefaultBpm <= 0
            || !double.IsFinite(config.PhigrosNegativeAlphaStep)
            || config.PhigrosNegativeAlphaStep <= 0
        )
            throw new ArgumentOutOfRangeException(nameof(config));
    }

    private void TryBackupInvalidConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var backupPath =
                    ConfigPath + ".invalid." + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                File.Move(ConfigPath, backupPath, true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppConfigService] Failed to backup invalid config: {ex.Message}");
        }
    }
}
