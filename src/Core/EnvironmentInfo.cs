using System;
using System.Runtime.InteropServices;

namespace Core;

// Описуємо дані як record (рівність за значенням)
public sealed record EnvironmentReport(
    string AppName,
    string Student,
    string Group,
    string Domain,
    string OsDescription,
    string OsVersion,
    string FrameworkDescription,
    string ProcessArchitecture,
    string DotNetVersion,
    string DetectedRid,
    string ReportedRid,
    string AppDirectory,
    string CurrentDirectory,
    string BuildNote
);

public static class EnvironmentInfo
{
    public static EnvironmentReport Collect()
    {
#if NET10_0_OR_GREATER
        const string note = "Збірка під .NET 10.0+";
#else
        const string note = "Збірка під .NET 8.0";
#endif

        return new EnvironmentReport(
            AppName: "CrossApp - практикум з крос-платформного програмування",
            Student: "Федонюк Артем",
            Group: "ФЕІ-34",
            Domain: "Бібліотека (Book, BookCopy, Reader, Loan)",
            OsDescription: RuntimeInformation.OSDescription,
            OsVersion: Environment.OSVersion.ToString(),
            FrameworkDescription: RuntimeInformation.FrameworkDescription,
            ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
            DotNetVersion: Environment.Version.ToString(),
            DetectedRid: DetectRid(),
            ReportedRid: RuntimeInformation.RuntimeIdentifier ?? "unknown",
            AppDirectory: AppContext.BaseDirectory,
            CurrentDirectory: Environment.CurrentDirectory,
            BuildNote: note
        );
    }

    // Ручне визначення RID для демонстрації його структури
    private static string DetectRid()
    {
        string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "unknown";

        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "unknown"
        };

        return $"{os}-{arch}";
    }
}