using System.Runtime.InteropServices;

namespace KaedePhi.Tool.App;

internal sealed partial class LinuxTerminalDetector : ITerminalDetector
{
    public bool IsInteractiveTerminal()
    {
        return isatty(1) == 1;
    }

    [LibraryImport("libc", SetLastError = true)]
    private static partial int isatty(int fd);
}
