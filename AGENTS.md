# KaedePhi 代码注释规范

## 基本原则

1. **有意义**：注释必须有助于理解代码，不能是无意义的装饰
2. **正确性**：行间注释必须准确描述代码行为，与代码逻辑保持一致
3. **简洁性**：不连续超过两行注释，避免冗余
4. **全中文**：所有注释必须使用中文，不得使用英文
5. **用词统一**：相同概念使用相同术语

## XML 文档注释规范

XML 文档注释必须包含以下内容（`<summary>` 标签内）：

- **传入什么**：方法的参数说明（使用 `<param>` 标签）
- **做了什么**：方法的主要功能描述
- **输出什么**：返回值说明（使用 `<returns>` 标签）

### 适用范围

XML 文档注释仅适用于**库项目**（KaedePhi.Core、KaedePhi.Tool）的公开接口。

**不需要** XML 文档注释的项目：
- CLI/GUI 交互程序（KaedePhi.Tool.App）：终端与桌面应用，无外部调用方
- 测试项目（KaedePhi.Tests）

这些项目的公开接口（ViewModel 属性、Command 属性等）名字本身已自解释，仅需保留有意义的行间注释。

### 禁止内容

- 不得出现调用方无需知道的实现原理
- 不得出现内部算法细节
- 不得出现与调用无关的技术细节

### 示例

```csharp
/// <summary>
/// 将 KPC 事件层映射为 PE 的线事件结构。
/// </summary>
/// <param name="target">目标 PE 判定线</param>
/// <param name="layers">源 KPC 事件层列表</param>
public void ConvertLineEvents(Pe.JudgeLine target, List<KpcEventLayer> layers)
{
    // 实现细节...
}
```

## 行间注释规范

### 允许的注释

- 解释复杂业务逻辑
- 说明非显而易见的决策原因
- 标记待办事项或临时解决方案

### 禁止的注释

- 分隔线注释（如 `// ----`、`// ====`、`// ****`）
- 无意义的装饰性注释
- 重复代码逻辑的注释

### 示例

```csharp
// 正确：解释为什么需要这个检查
if (Math.Abs(previousEndBeat - startBeat) > FloatEpsilon)
{
    // 断开连接：前一段结束拍与当前段开始拍不连续
    target.AlphaFrames.Add(new Pe.Frame());
}

// 错误：重复代码逻辑
// 检查是否断开连接
if (Math.Abs(previousEndBeat - startBeat) > FloatEpsilon)
{
    // 添加帧
    target.AlphaFrames.Add(new Pe.Frame());
}
```

## 代码分区规范

使用 `#region` 和 `#endregion` 进行代码分区，替代分隔线注释。

### 示例

```csharp
#region PE 转换选项

public double PeSpeedConversionRatio { get; set; } = 14d / 9d;
public double PeTrailingBeatPadding { get; set; } = 1d / 64d;

#endregion

#region PhigrosV3 转换选项

public float PhigrosDefaultBpm { get; set; } = 120f;

#endregion
```

## 检查清单

在提交代码前，请检查：

- [ ] 所有注释是否为中文
- [ ] 是否存在连续超过两行的注释
- [ ] 是否存在分隔线注释（应改为 `#region`）
- [ ] 库项目（Core/Tool）的公开接口是否有完整的 XML 文档
- [ ] 行间注释是否准确描述代码行为
- [ ] 用词是否统一
