using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using JocysCom.VS.ReferenceManager.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace JocysCom.VS.ReferenceManager.Controls
{
	/// <summary>
	/// Interaction logic for ProjectsListControl.xaml
	/// </summary>
	public partial class ProjectsListControl : UserControl
	{
		public ProjectsListControl()
		{
			InitializeComponent();
			ScanProgressPanel.Visibility = Visibility.Collapsed;
			if (ControlsHelper.IsDesignMode(this))
				return;
			ReferenceList = new SortableBindingList<ReferenceItem>();
			MainDataGrid.ItemsSource = ReferenceList;
			if (ReferenceList.Count > 0)
				MainDataGrid.SelectedIndex = 0;
			ExportSaveFileDialog = new System.Windows.Forms.SaveFileDialog();
			// Configure converter.
			var gridFormattingConverter = MainDataGrid.Resources.Values.Cast<ItemFormattingConverter>().First();
			gridFormattingConverter.ConvertFunction = _MainDataGridFormattingConverter_Convert;
		}

		private void Tasks_ListChanged(object sender, ListChangedEventArgs e)
			=> UpdateUpdateButton();

		bool selectionsUpdating = false;
		private void ReferenceList_ListChanged(object sender, ListChangedEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				UpdateControlsFromList();
				if (e.ListChangedType == ListChangedType.ItemChanged)
				{
					if (!selectionsUpdating && e.PropertyDescriptor?.Name == nameof(ReferenceItem.IsChecked))
					{
						selectionsUpdating = true;
						var selectedItems = MainDataGrid.SelectedItems.Cast<ReferenceItem>().ToList();
						// Get updated item.
						var item = (ReferenceItem)MainDataGrid.Items[e.NewIndex];
						if (selectedItems.Contains(item))
						{
							// Update other items to same value.
							selectedItems.Remove(item);
							foreach (var selecetdItem in selectedItems)
								if (selecetdItem.IsChecked != item.IsChecked)
									selecetdItem.IsChecked = item.IsChecked;
						}
						selectionsUpdating = false;
					}
				}
			});
		}

		void UpdateControlsFromList()
		{
			var list = ReferenceList;
			var updatable = 0;
			var count = list.Count;
			var s = "";
			switch (ProjectsControlType)
			{
				case ProjectsControlType.Solution:
					s += "Solution";
					break;
				case ProjectsControlType.Projects:
					s += "Projects";
					updatable = list.Count(x => x.StatusCode == MessageBoxImage.Information);
					var containsCheckedP = list.Any(x => x.IsChecked);
					var actionP = containsCheckedP ? "Checked" : "Selected";
					var bnP = $"Update References of {actionP} Projects";
					ControlsHelper.SetText(UpdateButtonLabel, bnP);
					break;
				case ProjectsControlType.References:
					s += "References";
					updatable = list.Count(x => x.StatusCode == MessageBoxImage.Information);
					var containsCheckedR = list.Any(x => x.IsChecked);
					var action = containsCheckedR ? "Checked" : "Selected";
					var bnR = $"Update {action} References to Projects";
					ControlsHelper.SetText(UpdateButtonLabel, bnR);
					break;
				case ProjectsControlType.ScanResults:
					s += "Scan Results";
					break;
				default:
					break;
			}
			if (count > 0)
				s += $" ({count})";
			if (updatable > 0)
				s += $" Updatable ({updatable})";
			ControlsHelper.SetText(HeadLabel, s);
		}

		object _MainDataGridFormattingConverter_Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var sender = (FrameworkElement)values[0];
			var template = (FrameworkElement)values[1];
			var cell = (DataGridCell)(template ?? sender).Parent;
			var value = values[2];
			var item = (ReferenceItem)cell.DataContext;
			// Format ConnectionClassColumn value.
			if (cell.Column == ReferencePathColumn)
			{
				cell.Foreground = item.ReferenceExists
					? System.Windows.Media.Brushes.Black
					: System.Windows.Media.Brushes.Gray;
			}
			// Format StatusCodeColumn value.
			if (cell.Column == StatusCodeColumn)
			{
				switch (item.StatusCode)
				{
					case MessageBoxImage.Error:
						return Icons.Current[Icons.Icon_Error];
					case MessageBoxImage.Question:
						return Icons.Current[Icons.Icon_Question];
					case MessageBoxImage.Warning:
						return Icons.Current[Icons.Icon_Warning];
					case MessageBoxImage.Information:
						return Icons.Current[Icons.Icon_Information];
					default:
						return null;
				}
			}
			return value;
		}

		public SortableBindingList<ReferenceItem> ReferenceList { get; set; }

		#region ■ Properties

		[Category("Main"), DefaultValue(ProjectsControlType.None)]
		public ProjectsControlType ProjectsControlType
		{
			get => _ProjectControlType;
			set { _ProjectControlType = value; UpdateType(); }
		}
		private ProjectsControlType _ProjectControlType;

		void UpdateType()
		{
			switch (ProjectsControlType)
			{
				case ProjectsControlType.Solution:
					HeadLabel.Content = "Solution";
					ShowColumns(SolutionNameColumn, SolutionPathColumn);
					ShowButtons(UpdateButton, RefreshButton);
					TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_Visual_Studio];
					break;
				case ProjectsControlType.Projects:
					HeadLabel.Content = "Projects";
					ShowColumns(IsCheckedColumn, StatusCodeColumn, StatusTextColumn, ProjectNameColumn, ProjectPathColumn);
					ShowButtons(UpdateButton, RefreshButton);
					TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_windows];
					break;
				case ProjectsControlType.References:
					HeadLabel.Content = "References";
					UpdateButtonLabel.Content = "Update Selected References to Projects";
					ShowColumns(IsCheckedColumn, StatusCodeColumn, StatusTextColumn, ProjectNameColumn, ReferenceNameColumn, ReferencePathColumn);
					ShowButtons(UpdateButton, RefreshButton);
					TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_arrow_fork2];
					break;
				case ProjectsControlType.ScanResults:
					HeadLabel.Content = "Scan Results";
					if (!ControlsHelper.IsDesignMode(this))
					{
						ReferenceList = Global.ReferenceItems.Items;
						MainDataGrid.ItemsSource = ReferenceList;
						if (ReferenceList.Count > 0)
							MainDataGrid.SelectedIndex = 0;
					}
					ShowColumns(ProjectNameColumn, ProjectAssemblyNameColumn, ProjectFrameworkVersionColumn, ReferenceNameColumn, ReferencePathColumn);
					ShowButtons(ScanButton, ExportButton);
					TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_clipboard_checks];
					break;
				default:
					break;
			}
			// Re-attach events and update header.
			ReferenceList.ListChanged -= ReferenceList_ListChanged;
			ReferenceList.ListChanged += ReferenceList_ListChanged;
			UpdateControlsFromList();
		}

		public void ShowColumns(params DataGridColumn[] args)
		{
			var all = MainDataGrid.Columns.ToArray();
			foreach (var control in all)
				control.Visibility = args.Contains(control) ? Visibility.Visible : Visibility.Collapsed;
		}

		public void ShowButtons(params Button[] args)
		{
			var all = new Button[] { UpdateButton, RefreshButton, ScanButton, ExportButton };
			foreach (var control in all)
				control.Visibility = args.Contains(control) ? Visibility.Visible : Visibility.Collapsed;
		}

		#endregion

		#region ■ Scan

		DateTime ScanStarted;
		object AddAndUpdateLock = new object();

		/// <summary>
		/// Scan for games
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ScanButton_Click(object sender, RoutedEventArgs e)
		{
			var form = new MessageBoxWindow();
			var result = form.ShowDialog("Start folders scan for projects?", "Scan", MessageBoxButton.OKCancel, MessageBoxImage.Question);
			if (result != MessageBoxResult.OK)
				return;
			ScanButton.IsEnabled = false;
			Global.MainWindow.HMan.AddTask(TaskName.Scan);
			ScanStarted = DateTime.Now;
			ReferenceList.Clear();
			var success = System.Threading.ThreadPool.QueueUserWorkItem(ScanTask);
			if (!success)
			{
				ProgressLevelTopLabel.Text = "Scan failed!";
				ProgressLevelSubLabel.Text = "";
				ScanButton.IsEnabled = true;
				Global.MainWindow.HMan.RemoveTask(TaskName.Scan);
			}
		}

		ProjectScanner _Scanner;

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
				ProgressLevelTopLabel.Text = "...";
				ProgressLevelSubLabel.Text = "";
				ScanProgressPanel.Visibility = Visibility.Visible;
				ScanButton.IsEnabled = false;
			});
			_Scanner = new ProjectScanner();
			_Scanner.Progress += _Scanner_Progress;
			//var games = SettingsManager.UserGames.Items;
			//var programs = SettingsManager.Programs.Items;
			var currentInfo = new List<ProjectFileInfo>();
			_Scanner.Scan(paths, currentInfo, name);
		}

		private void _Scanner_Progress(object sender, ProjectUpdaterEventArgs e)
		{
			if (ControlsHelper.InvokeRequired)
			{
				ControlsHelper.Invoke(() =>
					_Scanner_Progress(sender, e)
				);
				return;
			}
			var scanner = (ProjectScanner)sender;
			switch (e.State)
			{
				case ProjectUpdaterStatus.Started:
					UpdateProgress("Started...", "");
					break;
				case ProjectUpdaterStatus.Updated:
					lock (AddAndUpdateLock)
					{
						if (e.SubData is List<ReferenceItem> ris)
						foreach (var ri in ris)
							ReferenceList.Add(ri);
					}
					UpdateProgress(e);
					break;
				case ProjectUpdaterStatus.Completed:
					UpdateProgress();
					ProjectScanner.CashedData.Save();
					Global.ReferenceItems.Save();
					ScanButton.IsEnabled = true;
					Global.MainWindow.HMan.RemoveTask(TaskName.Scan);
					break;
				default:
					break;
			}
		}

		#endregion

		System.Windows.Forms.SaveFileDialog ExportSaveFileDialog;

		private void ExportButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = ExportSaveFileDialog;
			dialog.DefaultExt = "*.csv";
			dialog.Filter = "Data (*.csv)|*.csv|All files (*.*)|*.*";
			dialog.FilterIndex = 1;
			dialog.RestoreDirectory = true;
			if (string.IsNullOrEmpty(dialog.FileName)) dialog.FileName = "Projects_Data";
			//if (string.IsNullOrEmpty(dialog.InitialDirectory)) dialog.InitialDirectory = ;
			dialog.Title = "Export Data File";
			var result = dialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				var table = ConvertToTable(ReferenceList);
				var data = JocysCom.ClassLibrary.Files.CsvHelper.Write(table);
				System.IO.File.WriteAllText(dialog.FileName, data);
			}
		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
			=> UpdateUpdateButton();

		void UpdateUpdateButton()
		{
			var allowEnable = true;
			if (ProjectsControlType == ProjectsControlType.References)
			{
				// Count updatable references.
				allowEnable = MainDataGrid.SelectedItems.Cast<ReferenceItem>()
					.Count(x => x.StatusCode == MessageBoxImage.Information) > 0;

			}
			var isBusy = (Global.MainWindow?.HMan?.Tasks?.Count ?? 0) > 0;
			UpdateButton.IsEnabled = !isBusy && allowEnable;
		}

		/// <summary>
		/// Convert List to DataTable. Can be used to pass data into stored procedures. 
		/// </summary>
		public static DataTable ConvertToTable<T>(IEnumerable<T> list)
		{
			if (list == null) return null;
			var table = new DataTable();
			var props = typeof(T).GetProperties().Where(x => x.CanRead).ToArray();
			foreach (var prop in props)
				table.Columns.Add(prop.Name, prop.PropertyType);
			var values = new object[props.Length];
			foreach (T item in list)
			{
				for (int i = 0; i < props.Length; i++)
					values[i] = props[i].GetValue(item, null);
				table.Rows.Add(values);
			}
			return table;
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			Global.MainWindow.HMan.Tasks.ListChanged -= Tasks_ListChanged;
			Global.MainWindow.HMan.Tasks.ListChanged += Tasks_ListChanged;
		}

		public void UpdateProgress(ProjectUpdaterEventArgs e)
		{
			if (e.TopCount > 0)
			{
				if (ProgressLevelTopBar.Maximum != e.TopCount)
					ProgressLevelTopBar.Maximum = e.TopCount;
				if (ProgressLevelTopBar.Value != e.TopIndex)
					ProgressLevelTopBar.Value = e.TopIndex;

			}
			else
			{
				if (ProgressLevelTopBar.Maximum != 100)
					ProgressLevelTopBar.Maximum = 100;
				if (ProgressLevelTopBar.Value != 0)
					ProgressLevelTopBar.Value = 0;

			}
			if (e.SubCount > 0)
			{
				if (ProgressLevelSubBar.Maximum != e.SubCount)
					ProgressLevelSubBar.Maximum = e.SubCount;
				if (ProgressLevelSubBar.Value != e.SubIndex)
					ProgressLevelSubBar.Value = e.SubIndex;
			}
			else
			{
				if (ProgressLevelSubBar.Maximum != 100)
					ProgressLevelSubBar.Maximum = 100;
				if (ProgressLevelSubBar.Value != 0)
					ProgressLevelSubBar.Value = 0;
			}
			// Create top message.
			var tm = "";
			if (e.TopCount > 0)
				tm += $"{e.TopIndex}/{e.TopCount} - ";
			tm += $"{e.TopMessage}";
			// Create sub message.
			var sm = "";
			if (e.SubCount > 0)
				sm += $"{e.SubIndex}/{e.SubCount} - ";
			sm += $"{e.SubMessage}";
			UpdateProgress(tm, sm);
		}

		public void UpdateProgress(string topText = "", string SubText = "", bool? resetBars = null)
		{
			ControlsHelper.SetText(ProgressLevelTopLabel, topText);
			ControlsHelper.SetText(ProgressLevelSubLabel, SubText);
			if (resetBars.GetValueOrDefault())
			{
				ProgressLevelTopBar.Maximum = 100;
				ProgressLevelTopBar.Value = 0;
				ProgressLevelSubBar.Maximum = 100;
				ProgressLevelSubBar.Value = 0;
			}
			ControlsHelper.SetVisible(ScanProgressPanel, !string.IsNullOrEmpty(topText));
		}
	}
}