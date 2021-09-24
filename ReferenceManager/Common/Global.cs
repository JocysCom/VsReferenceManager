using JocysCom.ClassLibrary.Controls.IssuesControl;
using System.Linq;

namespace JocysCom.VS.ReferenceManager
{
	public static class Global
	{

		public static AppData AppSettings =>
			AppData.Items.FirstOrDefault();

		public static ClassLibrary.Configuration.SettingsData<AppData> AppData =
			new ClassLibrary.Configuration.SettingsData<AppData>(null, false, null, System.Reflection.Assembly.GetExecutingAssembly());

		public static ClassLibrary.Configuration.SettingsData<ReferenceItem> SolutionItems =
			new ClassLibrary.Configuration.SettingsData<ReferenceItem>($"{nameof(SolutionItems)}.xml", false, null, System.Reflection.Assembly.GetExecutingAssembly());

		public static ClassLibrary.Configuration.SettingsData<ReferenceItem> ProjectItems =
			new ClassLibrary.Configuration.SettingsData<ReferenceItem>($"{nameof(ProjectItems)}.xml", false, null, System.Reflection.Assembly.GetExecutingAssembly());

		public static ClassLibrary.Configuration.SettingsData<ReferenceItem> ReferenceItems =
			new ClassLibrary.Configuration.SettingsData<ReferenceItem>($"{nameof(ReferenceItems)}.xml", false, null, System.Reflection.Assembly.GetExecutingAssembly());

		public static ClassLibrary.Configuration.SettingsData<IssueItem> IssueItems =
			new ClassLibrary.Configuration.SettingsData<IssueItem>($"{nameof(IssueItems)}.xml", false, null, System.Reflection.Assembly.GetExecutingAssembly());


		public static void SaveSettings()
		{
			AppData.Save();
			SolutionItems.Save();
			ProjectItems.Save();
			ReferenceItems.Save();
			IssueItems.Save();
		}

		public static void LoadSettings()
		{
			AppData.Load();
			SolutionItems.Load();
			ProjectItems.Load();
			ReferenceItems.Load();
			IssueItems.Load();
		}

		public static void ClearItems()
		{
			SolutionItems.Items.Clear();
			ProjectItems.Items.Clear();
			ReferenceItems.Items.Clear();
			IssueItems.Items.Clear();
		}

		public static MainWindowControl MainWindow;

	}
}
