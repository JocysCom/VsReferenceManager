using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.IssuesControl;
using System.Windows.Controls;
using System.Linq;
using System.IO;

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
			DetectIssues_DuplicateAssembly();
			//DetectIssues_BackupFolders();
		}

		static string DuplicateAssembly = "Duplicate assembly";

		public static void DetectIssues_DuplicateAssembly()
		{
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
				ii.Name = DuplicateAssembly;
				var ds = $"{item.Count} projects produce same \"{item.AssemblyName}\" assembly:\r\n";
				for (int d = 0; d < projectDupes.Count; d++)
				{
					var pd = projectDupes[d];
					pd.StatusCode = System.Windows.MessageBoxImage.Warning;
					pd.StatusText = DuplicateAssembly;
					ds += $"{pd.ProjectPath}\r\n";
				}
				ii.Description = ds.Trim('\r', '\n', ' ');
				Global.IssueItems.Add(ii);
			}
		}

		public static void DetectIssues_BackupFolders()
		{
			// Get folders where duplicated projects are.
			var dupeProjects = Global.ProjectItems.Items
				.Where(x => x.StatusText == DuplicateAssembly)
				.Select(x => Path.GetDirectoryName(x.ProjectPath))
				.Distinct()
				.OrderBy(x => x)
				.ToList();
			// Get folders where are no duplicates.
			var origProjects = Global.ProjectItems.Items
				.Where(x => x.StatusCode == System.Windows.MessageBoxImage.None)
				.Select(x => Path.GetDirectoryName(x.ProjectPath))
				.Distinct()
				.OrderBy(x => x)
				.ToList();
			// Remove path if it contains unique project.
			// C:\Projects\Project1\Backup
			// C:\Projects\Project1\Backup\test
			//for (int i = 0; i < projects.Length; i++)
			//{
			//	var project = projects[i];
			//	project.ProjectFileInfo = new FileInfo(project.ProjectPath);
			//}
		}

	}
}
