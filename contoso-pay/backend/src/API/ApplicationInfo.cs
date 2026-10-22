using System.Diagnostics;
using System.Reflection;

namespace API;

public static class ApplicationInfo
{
    public const string BaseUrl = "contoso-pay";
    public const string Name = "Contoso Pay";

    public static string GetApplicationVersion()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        string version = $"{fileVersionInfo.FileMajorPart}.{fileVersionInfo.FileMinorPart}.{fileVersionInfo.FileBuildPart}";
        return version;
    }

    public static string GetNETVersion()
    {
        return Environment.Version.ToString();
    }
}