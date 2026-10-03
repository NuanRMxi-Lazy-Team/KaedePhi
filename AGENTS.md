# KaedePhi 代码注释规范

## 基本原则

1. **有意义**：注释必须有助于理解代码，不能是无意义的装饰
2. **正确性**：行间注释必须准确描述代码行为，与代码逻辑保持一致
3. **简洁性**：不连续超过两行注释，避免冗余
4. **全中文**：所有注释必须使用中文，不得使用英文
5. **用词统一**：相同概念使用相同术语
6. **禁止破坏性更改**：为所有破坏性更改添加向前兼容，在commit中明确说明破坏更改内容

## XML 文档注释规范

XML 文档注释必须包含以下内容（`<summary>` 标签内）：

- **传入什么**：方法的参数说明（使用 `<param>` 标签）
- **做了什么**：方法的主要功能描述
- **输出什么**：返回值说明（使用 `<returns>` 标签，除非是静态方法）

### 适用范围

XML 文档注释仅适用于**库项目**（KaedePhi.Core、KaedePhi.Tool）的公开接口。

**不需要** XML 文档注释的项目：
- CLI/GUI 交互程序（KaedePhi.Tool.App）：终端与桌面应用，无外部调用方
- 测试项目（KaedePhi.Tests）

这些项目的公开接口（ViewModel 属性、Command 属性等）名字本身已自解释，仅需保留有意义的行间注释。

对于非公开接口，不使用XML文档注释，而是使用行内注释进行逻辑说明。

### 禁止内容

- 不得出现调用方无需知道的实现原理
- 不得出现内部算法细节
- 不得出现与调用无关的技术细节

### 对于所有内容的禁止事项

在某些场景下，用户可能会提供给你一些参考用项目，这些参考项目通常有自己的名称，但是通常情况下，你永远也不应该在调用方可直接看到的内容中直接输出参考项目名称。

错误示范：
```csharp
/// <summary>
/// 检查事件列表是否已按 StartBeat 升序排列，对齐phichain行为
/// </summary>
private static bool IsSortedByStartBeat(List<IrEvents.Event<TPayload>> events)
{
    ... 
}
```

正确示范：
```csharp
/// <summary>
/// 检查事件列表是否已按 StartBeat 升序排列。
/// </summary>
private static bool IsSortedByStartBeat(List<IrEvents.Event<TPayload>> events)
{
    // 此处相关代码来自于项目“phichain”
    // 相关算法参考或代码誊抄已经过对应项目许可证LGPLv3授权，感谢原项目的贡献。
}
```
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

public double PeTrailingBeatPadding { get; set; } = 1d / 64d;

#endregion

#region PhigrosV3 转换选项

public float PhigrosDefaultBpm { get; set; } = 120f;

#endregion
```

## 代码文件规范
枚举、类、结构等内容不得定义在同一个文件中，命名空间一定要与文件所在位置对齐。

正确示范，一个位于KaedePhi.Core/Common/Unit.cs的文件：
```csharp
namespace KaedePhi.Core.Common
{
    /// <summary>
    /// 单位转换工具类
    /// </summary>
    public static class Unit
    {
        // 实现细节...
    }
}
```

错误示范，一个位于KaedePhi.Core/Common/Unit.cs的文件：
```csharp
namespace KaedePhi.Core.Units
{
    /// <summary>
    /// 单位转换工具类
    /// </summary>
    public static class Unit
    {
        // 实现细节...
    }
    
    public enum HowToDo
    {
        Delete,
        Keep,
        ReWrite,
        Ignore
    }
}
```

## 检查清单

在提交代码前，请检查：

- [ ] 所有注释是否为中文
- [ ] 是否存在连续超过两行的注释
- [ ] 是否存在分隔线注释（应改为 `#region`）
- [ ] 库项目（Core/Tool）的公开接口是否有完整的 XML 文档
- [ ] 行间注释是否准确描述代码行为
- [ ] 用词是否统一
- [ ] 是否有破坏性更改未添加向前兼容
- [ ] 是否有破坏性更改未在 commit 中明确说明
- [ ] 单个文件中是否定义了多个枚举、类、结构等内容
- [ ] 文件的命名空间是否与文件所在位置对齐
