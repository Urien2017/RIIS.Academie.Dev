using System.Diagnostics;
using System.Text;

namespace RIIS.Academic.Extensions;

public static class DesktopShortcutService
{
    public static void CreateAndOpen(
        string applicationUrl,
        bool createShortcut,
        bool openBrowser)
    {
        if (!OperatingSystem.IsWindows()
            || !Environment.UserInteractive
            || !Uri.TryCreate(applicationUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return;
        }

        try
        {
            if (createShortcut)
            {
                var desktopDirectory = Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory);

                if (!string.IsNullOrWhiteSpace(desktopDirectory))
                {
                    var shortcutPath = Path.Combine(
                        desktopDirectory,
                        "RIIS Academic.url");

                    var shortcutContent = $"[InternetShortcut]{Environment.NewLine}"
                        + $"URL={uri.AbsoluteUri}{Environment.NewLine}";

                    File.WriteAllText(
                        shortcutPath,
                        shortcutContent,
                        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                }
            }

            if (openBrowser)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = uri.AbsoluteUri,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Impossible de creer ou ouvrir le raccourci RIIS Academic : {exception.Message}");
        }
    }
}
