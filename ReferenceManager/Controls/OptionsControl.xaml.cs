using JocysCom.ClassLibrary.Controls;
using System.Windows;
using System.Windows.Controls;

namespace JocysCom.VS.ReferenceManager.Controls
{

	public partial class OptionsControl : UserControl
	{
		public OptionsControl()
		{
			InitializeComponent();
			if (ClassLibrary.Controls.ControlsHelper.IsDesignMode(this))
				return;
			SettingsFolderTextBox.Text = Global.AppData.XmlFile.Directory.FullName;
		}

		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			ControlsHelper.OpenUrl(Global.AppData.XmlFile.Directory.FullName);
		}
	}
}