using System.Runtime.InteropServices;

namespace KaedePhi.Tool.App;

/// <summary>
/// 基于 Win32 API GetConsoleProcessList 的实现。
/// 原理：如果程序是双击启动的，Windows 会为其创建一个全新的控制台，
/// 此时挂载在该控制台上的进程只有程序自身（进程数 == 1）。
/// 如果程序是从已有的 cmd、PowerShell、Git Bash 等终端启动的，
/// 该控制台上会同时挂载着终端本身的进程和子进程（进程数 > 1）。
/// </summary>
public sealed partial class WindowsTerminalDetector : ITerminalDetector
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial int GetConsoleProcessList(
        [In, Out] uint[] processList,
        uint processCount
    );

    public bool IsInteractiveTerminal()
    {
        // 没有控制台（例如作为 Windows 服务或被完全脱离控制台启动）
        if (GetConsoleWindowHandleIsNull())
        {
            return false;
        }

        // 先查询需要的缓冲区大小
        var probeBuffer = new uint[1];
        var count = GetConsoleProcessList(probeBuffer, 1);

        if (count <= 0)
        {
            return false;
        }

        // count 即为挂载在当前控制台上的进程数量
        // == 1 : 只有自身进程，说明控制台是为本程序新建的（双击启动）
        // > 1  : 控制台上还挂载着终端宿主进程（PowerShell/cmd/bash 等）
        return count > 1;
    }

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetConsoleWindow();

    private static bool GetConsoleWindowHandleIsNull()
    {
        return GetConsoleWindow() == IntPtr.Zero;
    }
}
