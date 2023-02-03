using System.Diagnostics;

namespace Core.CodeGen.Git;

public static class GitCommandHelper
{
    public static async Task CommitChangesAsync(string message)
    {
        try
        {
            string fileName = Environment.OSVersion.Platform switch
                              {
                                  PlatformID.Unix => "/bin/bash",
                                  PlatformID.MacOSX => "/bin/sh",
                                  _ => "cmd.exe"
                              };

            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.StandardInput.WriteLineAsync($"git add . && git commit -m \"{message}\"");
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();
            await process.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while sending the command: " + ex.Message);
        }
    }
}