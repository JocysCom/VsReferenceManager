using System.Linq;

namespace JocysCom.VS.ReferenceManager
{
	public static class Global
	{

		public static AppData AppSettings =>
			AppData.Items.FirstOrDefault();

		public static ClassLibrary.Configuration.SettingsData<AppData> AppData =
			new ClassLibrary.Configuration.SettingsData<AppData>(null, false, null, System.Reflection.Assembly.GetExecutingAssembly());

		public static ClassLibrary.Configuration.SettingsData<ReferenceItem> ProjectItems =
			new ClassLibrary.Configuration.SettingsData<ReferenceItem>($"{nameof(ProjectItems)}.xml", false, null, System.Reflection.Assembly.GetExecutingAssembly());

		public static ClassLibrary.Configuration.SettingsData<ReferenceItem> SolutionItems =
			new ClassLibrary.Configuration.SettingsData<ReferenceItem>($"{nameof(SolutionItems)}.xml", false, null, System.Reflection.Assembly.GetExecutingAssembly());

		public static void SaveSettings()
		{
			AppData.Save();
			ProjectItems.Save();
			SolutionItems.Save();
		}

		public static void LoadSettings()
		{
			AppData.Load();
			ProjectItems.Load();
			SolutionItems.Load();
		}


		public static MainWindowControl MainWindow;

	}
}
