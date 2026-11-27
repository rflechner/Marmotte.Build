using System.Runtime.CompilerServices;

namespace Marmotte.Build;

public static class Runtime
{
    public static string GetScriptDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

}