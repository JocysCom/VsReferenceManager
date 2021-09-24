using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace JocysCom.VS.ReferenceManager.Controls
{

	public partial class ScanFoldersControl : UserControl
	{
		public ScanFoldersControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			_Scanner = new LocationsScanner();
			_Scanner.Progress += _Scanner_Progress;
			Global.AppSettings.ScanLocations.ListChanged += ScanLocations_ListChanged;
			Global.SolutionItems.Items.ListChanged += SolutionItems_Items_ListChanged;
			Global.ProjectItems.Items.ListChanged += ProjectItems_Items_ListChanged;
			Global.ReferenceItems.Items.ListChanged += ReferenceItems_Items_ListChanged;
			LocationFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			ScanLocationsListBox.ItemsSource = Global.AppSettings.ScanLocations;
			ScanProgressPanel.UpdateProgress();
			AppHelper.SetText(FolderHeadLabel, "Folder", Global.AppSettings.ScanLocations.Count);
			AppHelper.SetText(SolutionsHeadLabel, "Solution", Global.SolutionItems.Items.Count);
			AppHelper.SetText(ProjectsHeadLabel, "Project", Global.ProjectItems.Items.Count);
			AppHelper.SetText(ReferencesHeadLabel, "Reference", Global.ReferenceItems.Items.Count);
		}

		private void ScanLocations_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
			=> AppHelper.SetText(FolderHeadLabel, "Folder", Global.AppSettings.ScanLocations.Count);

		private void SolutionItems_Items_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
			=> AppHelper.SetText(SolutionsHeadLabel, "Solution", Global.SolutionItems.Items.Count);

		private void ProjectItems_Items_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
			=> AppHelper.SetText(ProjectsHeadLabel, "Project", Global.ProjectItems.Items.Count);

		private void ReferenceItems_Items_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
			=> AppHelper.SetText(ReferencesHeadLabel, "Reference", Global.ReferenceItems.Items.Count);

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

		#region Locations Scanner

		DateTime ScanStarted;
		IScanner _Scanner;
		object AddAndUpdateLock = new object();

		/// <summary>
		/// Scan for games
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ScanButton_Click(object sender, RoutedEventArgs e)
		{
			var form = new MessageBoxWindow();
			var result = form.ShowDialog("Start scan?", "Scan", MessageBoxButton.OKCancel, MessageBoxImage.Question);
			if (result != MessageBoxResult.OK)
				return;
			ScanButton.IsEnabled = false;
			Global.MainWindow.InfoPanel.AddTask(TaskName.Scan);
			ScanStarted = DateTime.Now;
			Global.ClearItems();
			var success = System.Threading.ThreadPool.QueueUserWorkItem(ScanTask);
			if (!success)
			{
				ScanProgressPanel.UpdateProgress("Scan failed!", "", true);
				ScanButton.IsEnabled = true;
				Global.MainWindow.InfoPanel.RemoveTask(TaskName.Scan);
			}
		}

		void ScanTask(object state)
		{
			var scanPath = state as string;
			string[] paths;
			string name = null;
			if (string.IsNullOrEmpty(scanPath))
			{
				paths = Global.AppSettings.ScanLocations.ToArray();
			}
			else
			{
				// Set properties to scan single file.
				paths = new string[] { System.IO.Path.GetDirectoryName(scanPath) };
				name = System.IO.Path.GetFileName(scanPath);
			}
			ControlsHelper.Invoke(() =>
			{
				ScanProgressPanel.UpdateProgress("...", "");
				ScanProgressPanel.Visibility = Visibility.Visible;
				ScanButton.IsEnabled = false;
			});
			//var games = SettingsManager.UserGames.Items;
			//var programs = SettingsManager.Programs.Items;
			var currentInfo = new List<ProjectFileInfo>();
			_Scanner.Scan(paths, currentInfo, name);
		}

		private void _Scanner_Progress(object sender, ProgressEventArgs e)
		{
			if (ControlsHelper.InvokeRequired)
			{
				ControlsHelper.Invoke(() =>
					_Scanner_Progress(sender, e)
				);
				return;
			}
			var scanner = (IScanner)sender;
			switch (e.State)
			{
				case ProgressStatus.Started:
					ScanProgressPanel.UpdateProgress("Started...", "");
					break;
				case ProgressStatus.Updated:
					lock (AddAndUpdateLock)
					{
						if (e.SubData is List<ReferenceItem> ris)
							foreach (var ri in ris)
							{
								switch (ri.ItemType)
								{
									case ItemType.Solution:
										Global.SolutionItems.Add(ri);
										break;
									case ItemType.Project:
										Global.ProjectItems.Add(ri);
										break;
									case ItemType.Reference:
										Global.ReferenceItems.Add(ri);
										break;
									default:
										break;
								}
							}
					}
					ScanProgressPanel.UpdateProgress(e);
					break;
				case ProgressStatus.Completed:
					ScanProgressPanel.UpdateProgress();
					AppHelper.DetectIssues();
					Global.SaveSettings();
					ScanButton.IsEnabled = true;
					Global.MainWindow.InfoPanel.RemoveTask(TaskName.Scan);
					break;
				default:
					break;
			}
		}


		#endregion
	}
}