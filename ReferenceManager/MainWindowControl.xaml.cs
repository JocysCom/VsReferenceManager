using JocysCom.ClassLibrary.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using JocysCom.ClassLibrary.Controls.IssuesControl;

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
			Global.LoadSettings();
			if (Global.AppData.Items.Count == 0)
			{
				Global.AppData.Items.Add(new AppData());
				Global.AppData.Save();
			}
			InitializeComponent();
			SeverityConverter = new SeverityToImageConverter();
			UpdateIssuesIcon();
		}

		private void MainWindowPanel_Unloaded(object sender, RoutedEventArgs e)
		{
			Global.SaveSettings();
		}

		private void MainWindowPanel_Loaded(object sender, RoutedEventArgs e)
		{
			Global.IssueItems.Items.ListChanged += IssueItems_Items_ListChanged;
			AppHelper.SetText(IssueHeadLabel, "Issue", Global.IssueItems.Items.Count);
		}

		IssueSeverity? LastSeverity;
		SeverityToImageConverter SeverityConverter;

		private void IssueItems_Items_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
		{
			UpdateIssuesIcon();
			AppHelper.SetText(IssueHeadLabel, "Issue", Global.IssueItems.Items.Count);
		}


		void UpdateIssuesIcon()
		{
			var severity = Global.IssueItems.Items.Max(x => x.Severity);
			if (LastSeverity != severity)
			{
				LastSeverity = severity;
				IssueIconContent.Content = SeverityConverter.Convert(severity, null, null, null);
			}
		}

	}
}