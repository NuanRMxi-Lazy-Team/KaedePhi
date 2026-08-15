// init 访问器依赖的 IsExternalInit 类型仅在较新的目标框架中提供，
// netstandard2.1 缺少该类型，这里以条件编译补充定义。
#if NETSTANDARD2_1
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
