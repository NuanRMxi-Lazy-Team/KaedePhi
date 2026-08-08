using System.Diagnostics;
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
    public AppConfig Config { get; set; }

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
                return _deserializer.Deserialize<AppConfig>(yaml);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppConfigService] Failed to load config: {ex.Message}");
        }

        var defaults = new AppConfig();
        Save(defaults);
        return defaults;
    }

    /// <summary>
    /// 将当前配置写入配置文件。
    /// </summary>
    public void Save()
    {
        Save(Config);
    }

    /// <summary>
    /// 将配置重置为默认值并写入配置文件。
    /// </summary>
    public void ResetToDefaults()
    {
        Config = new AppConfig();
        Save();
    }

    private void Save(AppConfig config)
    {
        try
        {
            var yaml = _serializer.Serialize(config);
            File.WriteAllText(ConfigPath, yaml);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppConfigService] Failed to save config: {ex.Message}");
        }
    }
}