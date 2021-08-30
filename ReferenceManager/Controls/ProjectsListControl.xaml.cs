﻿using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using JocysCom.VS.ReferenceManager.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
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
			ExportSaveFileDialog = new System.Windows.Forms.SaveFileDialog();
			// Configure converter.
			var gridFormattingConverter = MainDataGrid.Resources.Values.Cast<ItemFormattingConverter>().First();
			gridFormattingConverter.ConvertFunction = _MainDataGridFormattingConverter_Convert;
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
					ShowColumns(StatusCodeColumn, StatusTextColumn, SolutionNameColumn, SolutionPathColumn);
					ShowButtons(RefreshButton);
					TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_Visual_Studio];
					break;
				case ProjectsControlType.Projects:
					HeadLabel.Content = "Projects";
					ShowColumns(StatusCodeColumn, StatusTextColumn, ProjectNameColumn, ProjectPathColumn);
					ShowButtons(RefreshButton);
					TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_windows];
					break;
				case ProjectsControlType.References:
					HeadLabel.Content = "References";
					ShowColumns(StatusCodeColumn, StatusTextColumn, ProjectNameColumn, ReferenceNameColumn, ReferencePathColumn);
					ShowButtons(ScanButton, UpdateButton, RefreshButton);
					TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_arrow_fork2];
					break;
				case ProjectsControlType.ScanResults:
					HeadLabel.Content = "Scan Results";
					if (!ControlsHelper.IsDesignMode(this))
					{
						ReferenceList = Global.ReferenceItems.Items;
						MainDataGrid.ItemsSource = ReferenceList;
					}
					ShowColumns(StatusCodeColumn, StatusTextColumn, ProjectNameColumn, ProjectAssemblyNameColumn, ReferenceNameColumn, ReferencePathColumn);
					ShowButtons(ScanButton, ExportButton);
					TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_clipboard_checks];
					break;
				default:
					break;
			}
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
				ScanProgressLevel0Label.Text = "Scan failed!";
				ScanProgressLevel1Label.Text = "";
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
				ScanProgressLevel0Label.Text = "...";
				ScanProgressLevel1Label.Text = "";
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

		private void _Scanner_Progress(object sender, ProjectScannerEventArgs e)
		{
			if (ControlsHelper.InvokeRequired)
			{
				ControlsHelper.Invoke(() =>
					_Scanner_Progress(sender, e)
				);
				return;
			}
			var scanner = (ProjectScanner)sender;
			var label = e.Level == 0
				? ScanProgressLevel0Label
				: ScanProgressLevel1Label;
			switch (e.State)
			{
				case ProjectScannerState.Started:
					label.Text = "Scanning...";
					break;
				case ProjectScannerState.DataFound:
				case ProjectScannerState.DataUpdated:
					lock (AddAndUpdateLock)
					{
						var data = e.Data;
						foreach (var r in data)
							ReferenceList.Add(r);
					}
					break;
				case ProjectScannerState.DirectoryUpdate:
				case ProjectScannerState.FileUpdate:
					var sb = new StringBuilder();
					sb.AppendLine(e.Message);
					if (e.State == ProjectScannerState.DirectoryUpdate && e.Directories != null)
					{
						sb.AppendFormat("Current Folder: {0}", e.Directories[e.DirectoryIndex].FullName);
					}
					if (e.State == ProjectScannerState.FileUpdate && e.Files != null)
					{
						var file = e.Files[e.FileIndex];
						var size = file.Length / 1024 / 1024;
						sb.AppendFormat("Current File ({0:0.0} MB): {1} ", size, file.FullName);
					}
					if (e.Level == 0)
					{
						sb.AppendLine();
						sb.AppendFormat("Skipped = {0}, Added = {1}, Updated = {2}", e.Skipped, e.Added, e.Updated);
					}
					sb.AppendLine();
					ControlsHelper.Invoke(() =>
					{
						label.Text = sb.ToString();
					});
					break;
				case ProjectScannerState.Completed:
					ControlsHelper.Invoke(() =>
					{
						ScanButton.IsEnabled = true;
						ScanProgressPanel.Visibility = Visibility.Collapsed;
					});
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
		{

		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{

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

	}
}
