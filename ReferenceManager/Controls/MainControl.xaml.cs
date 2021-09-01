using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace JocysCom.VS.ReferenceManager.Controls
{
	/// <summary>
	/// Interaction logic for MainControl.xaml
	/// </summary>
	public partial class MainControl : UserControl
	{
		public MainControl()
		{
			InitializeComponent();
			SolutionListPanel.MainDataGrid.SelectionChanged += SolutionListPanel_MainDataGrid_SelectionChanged;
			ProjectListPanel.MainDataGrid.SelectionChanged += ProjectsListPanel_MainDataGrid_SelectionChanged;
			ProjectListPanel.UpdateButton.Click += ProjectListPanel_UpdateButton_Click;
		}

		private void SolutionListPanel_MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateProjects();
		}

		public SortableBindingList<ReferenceItem> SolutionList
			=> SolutionListPanel.ReferenceList;

		public SortableBindingList<ReferenceItem> ProjectList
			=> ProjectListPanel.ReferenceList;

		public SortableBindingList<ReferenceItem> ReferenceList
			=> ReferenceListPanel.ReferenceList;

		/// <summary>
		/// Handles click on the button by displaying a message box.
		/// </summary>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event args.</param>
		[SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
		[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			UpdateProjects();
		}

		private void ProjectsListPanel_MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateReferences();
		}

		void UpdateSolution()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			SolutionList.Clear();
			var solution = SolutionHelper.GetCurrentSolution();
			if (solution == null)
				return;
			var item = new ReferenceItem()
			{
				SolutionName = System.IO.Path.GetFileNameWithoutExtension(solution.FileName),
				SolutionPath = solution.FileName,
				//RefLibraryPath = reference.Identity,
			};
			// This is a project reference
			SolutionList.Add(item);
			if (SolutionList.Count > 0)
				SolutionListPanel.MainDataGrid.SelectedIndex = 0;
		}

		public void UpdateProjects()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var solution = SolutionHelper.GetCurrentSolution();
			ProjectList.Clear();
			if (solution == null)
				return;
			for (int i = 0; i < solution.Projects.Count; i++)
			{
				// Try cast item to project (could be solution item).
				var p = solution.Projects.Item(i + 1).Object as VSLangProj.VSProject;
				if (p != null)
				{
					var item = new ReferenceItem()
					{
						ProjectName = p.Project.Name,
						ProjectPath = p.Project.FullName,
						Tag = p,
					};
					ProjectList.Add(item);
				}
			}
			if (ProjectList.Count > 0)
				ProjectListPanel.MainDataGrid.SelectedIndex = 0;
		}

		public void UpdateReferences()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			ReferenceList.Clear();
			var solution = SolutionHelper.GetCurrentSolution();
			var selectedProject = (ReferenceItem)ProjectListPanel.MainDataGrid.SelectedItem;
			if (selectedProject == null)
				return;
			var p = (VSLangProj.VSProject)selectedProject.Tag;
			foreach (VSLangProj.Reference reference in p.References)
			{
				if (reference.SourceProject == null)
				{
					// This is an assembly reference
					var item = new ReferenceItem()
					{
						ReferenceName = reference.Name,
						ReferencePath = reference.Path,
					};
					ReferenceList.Add(item);
					//var fullName = GetFullName(reference);
					//var assemblyName = new AssemblyName(fullName);
				}
				else
				{
					// This is project reference.
					var item = new ReferenceItem()
					{
						ProjectName = reference.Name,
						ProjectPath = reference.Path,
					};
					ReferenceList.Add(item);
				}
			}
			if (ReferenceList.Count > 0)
				ReferenceListPanel.MainDataGrid.SelectedIndex = 0;
		}

		public static string GetFullName(VSLangProj.Reference reference)
		{
			return string.Format("{0}, Version={1}.{2}.{3}.{4}, Culture={5}, PublicKeyToken={6}",
				reference.Name,
				reference.MajorVersion, reference.MinorVersion, reference.BuildNumber, reference.RevisionNumber,
				string.IsNullOrEmpty(reference.Culture) ? "neutral" : reference.Culture,
				string.IsNullOrEmpty(reference.PublicKeyToken) ? "null" : reference.PublicKeyToken
			);
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			// Workaround for Visual Studio designer crash.
			// Move load process to another method or designer will fail when trying to load DTE class.
			UpdateSolution();
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
		}

		public VSLangProj.VSProject GetSelectedVsProject()
		{
			var solution = SolutionHelper.GetCurrentSolution();
			if (solution == null)
				return null;
			var selectedProjectItem = (ReferenceItem)ProjectListPanel.MainDataGrid.SelectedItem;
			if (selectedProjectItem == null)
				return null;
			var vsProject = SolutionHelper.GetVsProject(selectedProjectItem.ProjectName);
			return vsProject;
		}

		private void ProjectListPanel_UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var form = new MessageBoxWindow();
			var result = form.ShowDialog("Replace references with projects?", "Update", MessageBoxButton.OKCancel, MessageBoxImage.Question);
			if (result != MessageBoxResult.OK)
				return;
			// Set progress controls.
			_TaskPanel = ProjectListPanel;
			_TaskButton = ProjectListPanel.UpdateButton;
			_TaskTopLabel = ProjectListPanel.ProgressLevelTopLabel;
			_TaskSubLabel = ProjectListPanel.ProgressLevelSubLabel;
			_TaskTopBar = ProjectListPanel.ProgressLevelTopBar;
			_TaskSubBar = ProjectListPanel.ProgressLevelSubBar;
			// Begin.
			_TaskButton.IsEnabled = false;
			Global.MainWindow.HMan.AddTask(TaskName.Update);
			var success = System.Threading.ThreadPool.QueueUserWorkItem(ProjectUpdateTask);
			if (!success)
			{
				_TaskTopLabel.Text = "Scan failed!";
				_TaskSubLabel.Text = "";
				_TaskPanel.Visibility = Visibility.Visible;
				_TaskButton.IsEnabled = true;
				Global.MainWindow.HMan.RemoveTask(TaskName.Update);
			}
		}

		ProjectUpdater _ProjectUpdater;
		Button _TaskButton;
		TextBlock _TaskTopLabel;
		TextBlock _TaskSubLabel;
		ProgressBar _TaskTopBar;
		ProgressBar _TaskSubBar;
		UIElement _TaskPanel;

		void ProjectUpdateTask(object state)
		{
			var projects = new List<VSLangProj.VSProject>();
			ControlsHelper.Invoke(() =>
			{
				_TaskTopLabel.Text = "...";
				_TaskSubLabel.Text = "";
				_TaskPanel.Visibility = Visibility.Visible;
				var project = GetSelectedVsProject();
				if (project != null)
					projects.Add(project);
			});
			_ProjectUpdater = new ProjectUpdater();
			_ProjectUpdater.Progress += _ProjectUpdater_Progress;
			_ProjectUpdater.ProcessData(projects, ReferenceList.ToList());
		}

		private void _ProjectUpdater_Progress(object sender, ProjectUpdaterEventArgs e)
		{
			if (ControlsHelper.InvokeRequired)
			{
				ControlsHelper.Invoke(() =>
					_ProjectUpdater_Progress(sender, e)
				);
				return;
			}
			var scanner = (ProjectUpdater)sender;
			switch (e.State)
			{
				case ProjectUpdaterStatus.Started:
					_TaskTopLabel.Text = "Starting...";
					break;
				case ProjectUpdaterStatus.Updated:
					ControlsHelper.Invoke(() =>
					{
						_TaskTopLabel.Text = $"{e.TopIndex}/{e.TopCount} - {e.TopMessage}";
						_TaskTopBar.Value = e.TopIndex;
						_TaskTopBar.Maximum = e.TopCount;
						_TaskSubLabel.Text = $"{e.SubIndex}/{e.SubCount} - {e.SubMessage}";
						_TaskSubBar.Value = e.SubIndex;
						_TaskSubBar.Maximum = e.SubCount;
					});
					break;
				case ProjectUpdaterStatus.Completed:
					ControlsHelper.Invoke(() =>
					{
						_TaskButton.IsEnabled = true;
						_TaskPanel.Visibility = Visibility.Collapsed;
					});
					ProjectScanner.CashedData.Save();
					Global.ReferenceItems.Save();
					_TaskButton.IsEnabled = true;
					Global.MainWindow.HMan.RemoveTask(TaskName.Update);
					break;
				default:
					break;
			}
		}
	}
}
