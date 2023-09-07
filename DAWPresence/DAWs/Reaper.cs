using System.Diagnostics;

namespace DAWPresence.DAWs;

public class Reaper : Daw
{
	public Reaper()
	{
		ProcessName = "reaper";
		DisplayName = "Reaper";
		ImageKey = "reaper";
		ApplicationId = "1148735771747037184";
		WindowTrim = " - REAPER v";
		TitleOffset = 0;
	}

	public override string GetProjectNameFromProcessWindow()
	{
		Process? process = GetProcess();
		if (process is null) return "";
		string title = process.MainWindowTitle;
		return title.Contains(WindowTrim)
			? title[..^title.IndexOf(WindowTrim, StringComparison.Ordinal)]
			: "";
	}
}
