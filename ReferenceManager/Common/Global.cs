using System.Linq;

namespace JocysCom.VS.ReferenceManager
{
	public static class Global
	{

		public static AppData AppSettings =>
			AppData.Items.FirstOrDefault();

		public static ClassLibrary.Configuration.SettingsData<AppData> AppData =
			new ClassLibrary.Configuration.SettingsData<AppData>(null, false, null, System.Reflection.Assembly.GetExecutingAssembly());

		public static ClassLibrary.Configuration.SettingsData<ReferenceItem> ReferenceItems =
			new ClassLibrary.Configuration.SettingsData<ReferenceItem>(null, false, null, System.Reflection.Assembly.GetExecutingAssembly());

		public static MainWindowControl MainWindow;

	}
}
