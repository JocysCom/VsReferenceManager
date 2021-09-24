using System.Windows.Controls;

namespace JocysCom.VS.ReferenceManager.Controls
{

	public partial class CheckIssuesControl : UserControl
	{
		public CheckIssuesControl()
		{
			InitializeComponent();
			if (ClassLibrary.Controls.ControlsHelper.IsDesignMode(this))
				return;
			MainDataGrid.ItemsSource = Global.IssueItems.Items;
		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

		}

		private void HyperLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{

		}

		private void SolutionButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{

		}

		private void CheckButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			AppHelper.DetectIssues();
			Global.IssueItems.Save();
		}

		private void ClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			Global.IssueItems.Items.Clear();
		}
	}
}