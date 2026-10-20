using System.Diagnostics;
using System.Reflection;

namespace API;

public static class ApiInfo
{
    public const string BaseUrl = "contoso-pay";
    public const string Title = "Contoso Pay";

    public static string GetVersion()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        string version = $"{fileVersionInfo.FileMajorPart}.{fileVersionInfo.FileMinorPart}.{fileVersionInfo.FileBuildPart}";
        return version;
    }
}