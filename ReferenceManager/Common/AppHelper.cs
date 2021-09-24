using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.IssuesControl;
using System.Windows.Controls;
using System.Linq;

namespace JocysCom.VS.ReferenceManager
{
	public static class AppHelper
	{

		public static void SetText(Label label, string name, int count, int updatable = 0)
		{
			var text = $"{count} {name}" + (count == 1 ? "" : "s");
			if (updatable > 0)
				text += $", {updatable} Updatable";
			ControlsHelper.SetText(label, text);
		}

		public static void DetectIssues()
		{
			Global.IssueItems.Items.Clear();
			var projects = Global.ProjectItems.Items.ToArray();
			// Group by assembly name.
			var results = projects.GroupBy(
				p => p.ProjectAssemblyName,
				(key, g) => new { AssemblyName = key, Count = g.Count() })
				// Filter by duplicates.
				.Where(x => x.Count > 1).ToList();
			for (int i = 0; i < results.Count; i++)
			{
				var item = results[i];
				var projectDupes = projects
					.Where(x => string.Equals(x.ProjectAssemblyName, item.AssemblyName, System.StringComparison.InvariantCultureIgnoreCase))
					.ToList();
				var ii = new IssueItem();
				ii.Severity = IssueSeverity.Important;
				ii.Name = "Duplicate assembly";
				var ds = $"{item.Count} projects produce same \"{item.AssemblyName}\" assembly:\r\n";
				for (int d = 0; d < projectDupes.Count; d++)
				{
					var pd = projectDupes[d];
					ds += $"{pd.ProjectPath}\r\n";
				}
				ii.Description = ds.Trim('\r', '\n', ' ');
				Global.IssueItems.Add(ii);
			}
		}

	}
}
