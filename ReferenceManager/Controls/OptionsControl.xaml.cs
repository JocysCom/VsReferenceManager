using JocysCom.ClassLibrary.Controls;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace JocysCom.VS.ReferenceManager.Controls
{

	public partial class OptionsControl : UserControl
	{
		public OptionsControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			LocationFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			ScanLocationsListBox.ItemsSource = Global.AppSettings.ScanLocations;
		}

		System.Windows.Forms.FolderBrowserDialog LocationFolderBrowserDialog;

		private void LocationAddButton_Click(object sender, RoutedEventArgs e)
		{
			var fbd = LocationFolderBrowserDialog;
			var path = fbd.SelectedPath;
			if (string.IsNullOrEmpty(path))
				path = (string)ScanLocationsListBox.SelectedValue;
			if (string.IsNullOrEmpty(path))
				path = System.IO.Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			fbd.SelectedPath = path;
			fbd.Description = "Browse for Scan Location";
			var result = fbd.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				// Don't allow to add windows folder.
				var winFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				if (fbd.SelectedPath.StartsWith(winFolder, StringComparison.OrdinalIgnoreCase))
				{
					MessageBoxWindow.Show("Windows folders are not allowed.", "Windows Folder", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
				}
				else
				{
					if (!Contains(fbd.SelectedPath))
					{
						Global.AppSettings.ScanLocations.Add(fbd.SelectedPath);
						Global.AppData.Save();
						// Change selected index for change event to fire.
						ScanLocationsListBox.SelectedItem = fbd.SelectedPath;
					}
				}
			}
		}

		private void LocationRemoveButton_Click(object sender, RoutedEventArgs e)
		{
			if (ScanLocationsListBox.SelectedIndex == -1)
				return;
			var currentIndex = ScanLocationsListBox.SelectedIndex;
			var currentItem = ScanLocationsListBox.SelectedItem as string;
			Global.AppSettings.ScanLocations.Remove(currentItem);
			// Change selected index for change event to fire.
			ScanLocationsListBox.SelectedIndex = Math.Min(currentIndex, ScanLocationsListBox.Items.Count - 1);
		}

		private void LocationRefreshButton_Click(object sender, RoutedEventArgs e)
		{
			var solution = SolutionHelper.GetCurrentSolution();
			if (solution == null)
				return;
			var fi = new FileInfo(solution.FullName);
			var path = fi.Directory.FullName;
			if (!Contains(path))
				Global.AppSettings.ScanLocations.Add(path);
		}

		private void ScanLocationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			LocationRemoveButton.IsEnabled = ScanLocationsListBox.SelectedIndex > -1;
		}

		bool Contains(string path)
		{
			return Global.AppSettings.ScanLocations
				.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase));
		}

	}
}