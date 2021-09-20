using JocysCom.ClassLibrary.Controls;
using System.Windows;
using System.Windows.Controls;

namespace JocysCom.VS.ReferenceManager
{
	/// <summary>
	/// Interaction logic for MainWindowControl.
	/// </summary>
	public partial class MainWindowControl : UserControl
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MainWindowControl"/> class.
		/// </summary>
		public MainWindowControl()
		{
			ControlsHelper.InitInvokeContext();
			Global.AppData.Load();
			Global.ReferenceItems.Load();
			if (Global.AppData.Items.Count == 0)
			{
				Global.AppData.Items.Add(new AppData());
				Global.AppData.Save();
			}
			InitializeComponent();
		}

	private void MainWindowPanel_Unloaded(object sender, RoutedEventArgs e)
		{
			Global.AppData.Save();
			Global.ReferenceItems.Save();
		}

	}
}