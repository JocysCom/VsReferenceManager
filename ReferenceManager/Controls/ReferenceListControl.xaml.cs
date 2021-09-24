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
	public partial class ReferenceListControl : UserControl
	{
		public ReferenceListControl()
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
					s = "Solution";
					break;
				case ProjectsControlType.Projects:
					s = "Project";
					updatable = list.Count(x => x.StatusCode == MessageBoxImage.Information);
					var containsCheckedP = list.Any(x => x.IsChecked);
					var actionP = containsCheckedP ? "Checked" : "Selected";
					var bnP = $"Update References of {actionP} Projects";
					ControlsHelper.SetText(UpdateButtonLabel, bnP);
					break;
				case ProjectsControlType.References:
					s = "Reference";
					updatable = list.Count(x => x.StatusCode == MessageBoxImage.Information);
					var containsCheckedR = list.Any(x => x.IsChecked);
					var action = containsCheckedR ? "Checked" : "Selected";
					var bnR = $"Update {action} References to Projects";
					ControlsHelper.SetText(UpdateButtonLabel, bnR);
					break;
				case ProjectsControlType.ReferenceItems:
					break;
				case ProjectsControlType.SolutionItems:
					break;
				default:
					break;
			}
			if (!string.IsNullOrEmpty(s))
			{
				AppHelper.SetText(TitleLabel, s, count, updatable);
			}
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

		public SortableBindingList<ReferenceItem> ReferenceList { get; set; } = new SortableBindingList<ReferenceItem>();

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
					//HeadLabel.Content = "Solution";
					ShowColumns(SolutionNameColumn, SolutionPathColumn);
					ShowButtons(UpdateButton, RefreshButton);
					//TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_Visual_Studio];
					break;
				case ProjectsControlType.Projects:
					//HeadLabel.Content = "Projects";
					ShowColumns(IsCheckedColumn, StatusCodeColumn, StatusTextColumn, ProjectNameColumn, ProjectPathColumn);
					ShowButtons(UpdateButton, RefreshButton);
					//TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_windows];
					break;
				case ProjectsControlType.References:
					//HeadLabel.Content = "References";
					UpdateButtonLabel.Content = "Update Selected References to Projects";
					ShowColumns(IsCheckedColumn, StatusCodeColumn, StatusTextColumn, ProjectNameColumn, ReferenceNameColumn, ReferencePathColumn);
					ShowButtons(UpdateButton, RefreshButton);
					//TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_arrow_fork2];
					break;
				case ProjectsControlType.SolutionItems:
					InitControlByType(Global.SolutionItems.Items);
					ShowButtons(ExportButton);
					ShowColumns(SolutionNameColumn, SolutionPathColumn);
					//TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_clipboard_checks];
					break;
				case ProjectsControlType.ProjectItems:
					InitControlByType(Global.ProjectItems.Items);
					ShowColumns(ProjectNameColumn, ProjectAssemblyNameColumn, ProjectFrameworkVersionColumn, ProjectPathColumn);
					ShowButtons(ExportButton);
					//TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_clipboard_checks];
					break;
				case ProjectsControlType.ReferenceItems:
					InitControlByType(Global.ReferenceItems.Items);
					ShowColumns(ProjectNameColumn, ReferenceNameColumn, ReferencePathColumn);
					ShowButtons(ExportButton);
					//TabIconContentControl.Content = Icons_Default.Current[Icons_Default.Icon_clipboard_checks];
					break;
				default:
					break;
			}
			// Re-attach events and update header.
			ReferenceList.ListChanged -= ReferenceList_ListChanged;
			ReferenceList.ListChanged += ReferenceList_ListChanged;
			UpdateControlsFromList();
		}

		void InitControlByType(SortableBindingList<ReferenceItem> items)
		{
			//HeadLabel.Content = $"{ProjectsControlType}".Replace("Item", "");
			if (!ControlsHelper.IsDesignMode(this))
			{
				ReferenceList = items;
				MainDataGrid.ItemsSource = ReferenceList;
				if (ReferenceList.Count > 0)
					MainDataGrid.SelectedIndex = 0;
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
			var all = new Button[] { UpdateButton, RefreshButton, ExportButton };
			foreach (var control in all)
				control.Visibility = args.Contains(control) ? Visibility.Visible : Visibility.Collapsed;
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
			if (string.IsNullOrEmpty(dialog.FileName))
				dialog.FileName = $"{ProjectsControlType}";
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
			var isBusy = (Global.MainWindow?.InfoPanel?.Tasks?.Count ?? 0) > 0;
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
			Global.MainWindow.InfoPanel.Tasks.ListChanged -= Tasks_ListChanged;
			Global.MainWindow.InfoPanel.Tasks.ListChanged += Tasks_ListChanged;
		}

	}
}