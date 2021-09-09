using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
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
			SolutionListPanel.RefreshButton.Click += SolutionListPanel_RefreshButton_Click;
			SolutionListPanel.MainDataGrid.SelectionChanged += SolutionListPanel_MainDataGrid_SelectionChanged;
			// Trigger projects update.
			ProjectListPanel.RefreshButton.Click += ProjectListPanel_RefreshButton_Click;
			ProjectListPanel.MainDataGrid.SelectionChanged += ProjectListPanel_MainDataGrid_SelectionChanged;
			ProjectListPanel.UpdateButton.Click += ProjectListPanel_UpdateButton_Click;
			// Trigger references update.
			ReferenceListPanel.RefreshButton.Click += ReferenceListPanel_RefreshButton_Click;
			ReferenceListPanel.UpdateButton.Click += ReferenceListPanel_UpdateButton_Click;
		}

		public SortableBindingList<ReferenceItem> SolutionList
			=> SolutionListPanel.ReferenceList;

		public SortableBindingList<ReferenceItem> ProjectList
			=> ProjectListPanel.ReferenceList;

		public SortableBindingList<ReferenceItem> ReferenceList
			=> ReferenceListPanel.ReferenceList;

		private void SolutionListPanel_RefreshButton_Click(object sender, RoutedEventArgs e)
			=> UpdateSolution();

		private void SolutionListPanel_MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
			=> UpdateProjects();

		private void ProjectListPanel_RefreshButton_Click(object sender, RoutedEventArgs e)
			=> UpdateProjects();

		private void ProjectListPanel_MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
			=> UpdateReferences();

		private void ReferenceListPanel_RefreshButton_Click(object sender, RoutedEventArgs e)
			=> UpdateReferences();

		#region Update Solution 

		void UpdateSolution()
		{
			var grid = SolutionListPanel.MainDataGrid;
			var key = nameof(ReferenceItem.SolutionName);
			var selection = ControlsHelper.GetSelection<string>(grid, key);
			UpdateSolutionFromDTE();
			ControlsHelper.RestoreSelection(grid, key, selection);
		}

		void UpdateSolutionFromDTE()
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
		}

		#endregion

		#region Update Projects

		void UpdateProjects()
		{
			var grid = ProjectListPanel.MainDataGrid;
			var key = nameof(ReferenceItem.ProjectName);
			var selection = ControlsHelper.GetSelection<string>(grid, key);
			UpdateProjectsFromSelectedSolution();
			ControlsHelper.RestoreSelection(grid, key, selection);
		}

		void UpdateProjectsFromSelectedSolution()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var solution = SolutionHelper.GetCurrentSolution();
			ProjectList.Clear();
			if (solution == null)
				return;
			var projects = SolutionHelper.GetAllProjects();
			for (int i = 0; i < projects.Count; i++)
			{
				// Try cast item to project (could be solution item).
				var p = projects[i].Object as VSLangProj.VSProject;
				if (p == null)
					continue;
				var item = new ReferenceItem()
				{
					ProjectName = p.Project.Name,
					ProjectPath = p.Project.FullName,
					Tag = p,
				};
				ProjectList.Add(item);
			}
		}

		#endregion

		#region Update References

		void UpdateReferences()
		{
			var grid = ReferenceListPanel.MainDataGrid;
			var key = nameof(ReferenceItem.ReferenceName);
			var selection = ControlsHelper.GetSelection<string>(grid, key);
			var selectedProject = (ReferenceItem)ProjectListPanel.MainDataGrid.SelectedItem;
			FillReferencesFromSelectedProject(selectedProject, ReferenceList);
			UpdateReferences_FindProjects(ReferenceList);
			ControlsHelper.RestoreSelection(grid, key, selection);
		}

		void FillReferencesFromSelectedProject(ReferenceItem project, IList<ReferenceItem> references)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			references.Clear();
			var solution = SolutionHelper.GetCurrentSolution();
			if (project == null)
				return;
			var p = (VSLangProj.VSProject)project.Tag;
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
					references.Add(item);
				}
				else
				{
					// This is project reference.
					var item = new ReferenceItem()
					{
						ProjectName = reference.Name,
						ProjectPath = reference.Path,
					};
					references.Add(item);
				}
			}
		}

		void UpdateReferences_FindProjects(IList<ReferenceItem> references)
		{
			for (int r = 0; r < references.Count; r++)
			{
				var ri = references[r];
				// Find project for reference.
				var refProjects = Global.ReferenceItems.Items.Where(x => x.ProjectAssemblyName == ri.ReferenceName && x.IsProject).ToList();
				if (refProjects.Count() == 1)
				{
					ControlsHelper.Invoke(() =>
					{
						var refProject = refProjects[0];
						ri.ProjectName = refProject.ProjectName;
						ri.ProjectPath = refProject.ProjectPath;
						ri.ProjectAssemblyName = refProject.ProjectAssemblyName;
						ri.StatusCode = MessageBoxImage.Information;
						ri.StatusText = "Project Found";
					});
				}
				else if (refProjects.Count() > 1)
				{
					ControlsHelper.Invoke(() =>
					{
						ri.StatusCode = MessageBoxImage.Warning;
						ri.StatusText = "Multiple Projects found";
					});
				}
			}
		}

		#endregion

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
			var param = new ProjectUpdaterParam();
			bool containsChecked;
			var projectItems = GetCheckedOrSelectedReferences(ProjectListPanel, out containsChecked);
			int referenceCount = 0;
			foreach (var projectItem in projectItems)
			{
				// Create list to gather information about references.
				var references = new List<ReferenceItem>();
				// Fill list with reference projects.
				FillReferencesFromSelectedProject(projectItem, references);
				// Mark records which can be updated.
				UpdateReferences_FindProjects(references);
				// Leave only updatable records i.e. records which have "Information" status code.
				references = references.Where(x => x.StatusCode == MessageBoxImage.Information).ToList();
				// Get Visual studio objects.
				var project = SolutionHelper.GetVsProject(projectItem.ProjectName);
				param.Data.Add(project, references);
				referenceCount += references.Count;
			};
			var form = new MessageBoxWindow();
			var result = form.ShowDialog($"Replace {referenceCount} references on {param.Data.Keys.Count} projects?", "Update", MessageBoxButton.OKCancel, MessageBoxImage.Question);
			if (result != MessageBoxResult.OK)
				return;
			StartUpdate(param);
		}

		private void ReferenceListPanel_UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var args = new ProjectUpdaterParam();
			var project = GetSelectedVsProject();
			bool containsChecked;
			var references = GetCheckedOrSelectedReferences(ReferenceListPanel, out containsChecked);
			var action = containsChecked ? "Checked" : "Selected";
			args.Data.Add(project, references);
			var form = new MessageBoxWindow();
			var result = form.ShowDialog($"Replace {references.Count} {action} references on {project?.Project?.Name} project?", "Update", MessageBoxButton.OKCancel, MessageBoxImage.Question);
			if (result != MessageBoxResult.OK)
				return;
			StartUpdate(args);
		}

		List<ReferenceItem> GetCheckedOrSelectedReferences(ProjectsListControl control, out bool containsChecked)
		{
			var list = control.ReferenceList;
			containsChecked = list.Any(x => x.IsChecked);
			var references = containsChecked
				? list.Where(x => x.IsChecked).ToList()
				: control.MainDataGrid.SelectedItems.Cast<ReferenceItem>().ToList();
			if (control.ProjectsControlType == ProjectsControlType.References)
				references = references.Where(x => x.StatusCode == MessageBoxImage.Information).ToList();
			return references;
		}

		void StartUpdate(ProjectUpdaterParam state)
		{
			// Set progress controls.
			_TaskPanel = ProjectListPanel.ScanProgressPanel;
			_TaskTopLabel = ProjectListPanel.ProgressLevelTopLabel;
			_TaskSubLabel = ProjectListPanel.ProgressLevelSubLabel;
			_TaskTopBar = ProjectListPanel.ProgressLevelTopBar;
			_TaskSubBar = ProjectListPanel.ProgressLevelSubBar;
			// Begin.
			Global.MainWindow.HMan.AddTask(TaskName.Update);
			var success = System.Threading.ThreadPool.QueueUserWorkItem(ProjectUpdateTask, state);
			if (!success)
			{
				_TaskTopLabel.Text = "Scan failed!";
				_TaskSubLabel.Text = "";
				_TaskPanel.Visibility = Visibility.Visible;
				Global.MainWindow.HMan.RemoveTask(TaskName.Update);
			}
		}

		ProjectUpdater _ProjectUpdater;
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
			});
			_ProjectUpdater = new ProjectUpdater();
			_ProjectUpdater.Progress += _ProjectUpdater_Progress;
			var param = (ProjectUpdaterParam)state;
			_ProjectUpdater.ProcessData(param);
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
						_TaskPanel.Visibility = Visibility.Collapsed;
					});
					ProjectScanner.CashedData.Save();
					Global.ReferenceItems.Save();
					Global.MainWindow.HMan.RemoveTask(TaskName.Update);
					ControlsHelper.BeginInvoke(() =>
					{
						UpdateSolution();
						UpdateProjects();
						UpdateReferences();
					});
					break;
				default:
					break;
			}
		}
	}
}
