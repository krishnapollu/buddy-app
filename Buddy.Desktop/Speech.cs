using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Buddy.Desktop;

public sealed record VoiceOption(string Name, string Label)
{
    public override string ToString() => Label;
}

public static class Speech
{
    public static List<VoiceOption> AvailableVoices()
    {
        var voices = new List<VoiceOption> { new("Default", "System default") };
        try
        {
            ProcessStartInfo info;
            if (OperatingSystem.IsMacOS())
            {
                info = new ProcessStartInfo("/usr/bin/say") { RedirectStandardOutput = true, UseShellExecute = false };
                info.ArgumentList.Add("-v"); info.ArgumentList.Add("?");
            }
            else if (OperatingSystem.IsWindows())
            {
                var script = "Add-Type -AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).GetInstalledVoices() | ForEach-Object { $_.VoiceInfo.Name + '|' + $_.VoiceInfo.Culture.Name }";
                info = new ProcessStartInfo("powershell.exe") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                info.ArgumentList.Add("-NoProfile"); info.ArgumentList.Add("-NonInteractive"); info.ArgumentList.Add("-Command"); info.ArgumentList.Add(script);
            }
            else return voices;
            using var process = Process.Start(info);
            if (process is null) return voices;
            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(5000)) { process.Kill(); return voices; }
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (OperatingSystem.IsMacOS())
                {
                    var match = Regex.Match(line, @"^\s*(.+?)\s{2,}([a-z]{2}_[A-Z]{2})\s+#");
                    if (match.Success) voices.Add(new(match.Groups[1].Value.Trim(), $"{match.Groups[1].Value.Trim()} · {match.Groups[2].Value}"));
                }
                else
                {
                    var parts = line.Trim().Split('|', 2);
                    if (parts.Length == 2) voices.Add(new(parts[0], $"{parts[0]} · {parts[1]}"));
                }
            }
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
        return voices;
    }

    public static void Say(string message, string voice)
    {
        try
        {
            if (OperatingSystem.IsMacOS())
            {
                var info = new ProcessStartInfo("/usr/bin/say") { UseShellExecute = false, CreateNoWindow = true };
                if (voice != "Default") { info.ArgumentList.Add("-v"); info.ArgumentList.Add(voice); }
                info.ArgumentList.Add(message);
                Process.Start(info)?.Dispose();
            }
            else if (OperatingSystem.IsWindows())
            {
                var script = "Add-Type -AssemblyName System.Speech; $s = New-Object System.Speech.Synthesis.SpeechSynthesizer; $v = [Console]::In.ReadLine(); if ($v -ne 'Default') { $s.SelectVoice($v) }; $s.Speak([Console]::In.ReadToEnd())";
                var info = new ProcessStartInfo("powershell.exe") { RedirectStandardInput = true, UseShellExecute = false, CreateNoWindow = true };
                info.ArgumentList.Add("-NoProfile"); info.ArgumentList.Add("-NonInteractive"); info.ArgumentList.Add("-Command"); info.ArgumentList.Add(script);
                using var process = Process.Start(info);
                if (process is not null) { process.StandardInput.WriteLine(voice); process.StandardInput.Write(message); process.StandardInput.Close(); }
            }
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
    }
}
