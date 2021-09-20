using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.Shell;
using System;
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
			SolutionListPanel.UpdateButton.Click += SolutionListPanel_UpdateButton_Click;
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
			UpdateReferences_FindProjects(selectedProject, ReferenceList);
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

		void UpdateReferences_FindProjects(ReferenceItem project, IList<ReferenceItem> references)
		{
			for (int r = 0; r < references.Count; r++)
			{
				var ri = references[r];
				// Find project for reference.
				var refProjects = Global.ReferenceItems.Items.Where(x => x.ProjectAssemblyName == ri.ReferenceName && x.IsProject).ToList();
				// Continue if no projects found.
				if (refProjects.Count == 0)
					continue;
				// If more than 1 project found then...
				if (refProjects.Count() > 1)
				{
					ControlsHelper.Invoke(() =>
					{
						ri.StatusCode = MessageBoxImage.Warning;
						ri.StatusText = "Multiple Projects found";

					});
					continue;
				}
				// Get reference.
				var refProject = refProjects.First();
				Version refVersion;
				Version projectVersion;
				if (Version.TryParse(project.ProjectFrameworkVersion, out projectVersion) &&
					Version.TryParse(refProject.ProjectFrameworkVersion, out refVersion) && refVersion > projectVersion)
				{
					ControlsHelper.Invoke(() =>
					{
						ri.StatusCode = MessageBoxImage.Warning;
						ri.StatusText = "High Reference Version";
					});
					continue;
				}
				ControlsHelper.Invoke(() =>
				{
					ri.ProjectName = refProject.ProjectName;
					ri.ProjectPath = refProject.ProjectPath;
					ri.ProjectAssemblyName = refProject.ProjectAssemblyName;
					ri.StatusCode = MessageBoxImage.Information;
					ri.StatusText = "Project Found";
				});
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

		void StartUpdater(ProjectsListControl control, IList<ReferenceItem> projectItems, IList<ReferenceItem> referenceItems = null)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var param = new ProjectUpdaterParam();
			foreach (var projectItem in projectItems)
			{
				// Create list to gather information about references.
				var references = referenceItems;
				if (references == null)
				{
					references = new List<ReferenceItem>();
					// Fill list with reference projects.
					FillReferencesFromSelectedProject(projectItem, references);
					// Mark records which can be updated.
					UpdateReferences_FindProjects(projectItem, references);
					// Leave only updatable records i.e. records which have "Information" status code.
					references = references.Where(x => x.StatusCode == MessageBoxImage.Information).ToList();
				}
				// If found references to update then...
				if (references.Count > 0)
				{
					// Get Visual studio objects.
					var project = SolutionHelper.GetVsProject(projectItem.ProjectName);
					param.Data.Add(project, references);
				}
			};
			var form = new MessageBoxWindow();
			var projectCount = param.Data.Keys.Count;
			var referenceCount = param.Data.Values.Select(x => x.Count).Sum();
			var referenceText = $"{referenceCount} reference" + (referenceCount > 1 ? "s" : "");
			var projectText = $"{projectCount} project" + (projectCount > 1 ? "s" : "");
			if (projectCount == 0)
			{
				form.ShowDialog($"There are no project references to update.", "Update", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}
			var message = $"Update {referenceText} on {projectText}?";
			if (referenceCount < 8)
			{
				message = "";
				foreach (var project in param.Data.Keys)
				{
					message += $"\r\nProject \"{project.Project?.Name}\" references:\r\n\r\n";
					foreach (var reference in param.Data[project])
						message += $"\t{reference.ReferenceName}\r\n";
				}
				message = message.Trim('\r', '\n');
			}
			var result = form.ShowDialog(message, "Update References", MessageBoxButton.OKCancel, MessageBoxImage.Question);
			if (result != MessageBoxResult.OK)
				return;
			// Set progress controls.
			_TaskControl = control;
			// Begin.
			Global.MainWindow.InfoPanel.AddTask(TaskName.Update);
			var success = System.Threading.ThreadPool.QueueUserWorkItem(ProjectUpdateTask, param);
			if (!success)
			{
				_TaskControl.ScanProgressPanel.UpdateProgress("Scan failed!", "", true);
				Global.MainWindow.InfoPanel.RemoveTask(TaskName.Update);
			}
		}

		private void SolutionListPanel_UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			var projectItems = ProjectListPanel.ReferenceList.ToList();
			StartUpdater(SolutionListPanel, projectItems);
		}

		private void ProjectListPanel_UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			bool containsChecked;
			var projectItems = GetCheckedOrSelectedReferences(ProjectListPanel, out containsChecked);
			StartUpdater(ProjectListPanel, projectItems);
		}

		private void ReferenceListPanel_UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedProjectItem = (ReferenceItem)ProjectListPanel.MainDataGrid.SelectedItem;
			if (selectedProjectItem == null)
				return;
			var projectItems = new List<ReferenceItem>() { selectedProjectItem };
			bool containsChecked;
			var referenceItems = GetCheckedOrSelectedReferences(ReferenceListPanel, out containsChecked);
			StartUpdater(ReferenceListPanel, projectItems, referenceItems);
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

		ProjectsListControl _TaskControl;
		ProjectUpdater _ProjectUpdater;

		void ProjectUpdateTask(object state)
		{
			var projects = new List<VSLangProj.VSProject>();
			ControlsHelper.Invoke(() =>
			{
				_TaskControl.ScanProgressPanel.UpdateProgress("Starting...", "", true);
			});
			_ProjectUpdater = new ProjectUpdater();
			_ProjectUpdater.Progress += _ProjectUpdater_Progress;
			var param = (ProjectUpdaterParam)state;
			_ProjectUpdater.ProcessData(param);
		}

		private void _ProjectUpdater_Progress(object sender, ProgressEventArgs e)
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
				case ProgressStatus.Started:
					_TaskControl.ScanProgressPanel.UpdateProgress("Started...", "");
					break;
				case ProgressStatus.Updated:
					_TaskControl.ScanProgressPanel.UpdateProgress(e);
					break;
				case ProgressStatus.Completed:
					_TaskControl.ScanProgressPanel.UpdateProgress();
					ProjectScanner.CashedData.Save();
					Global.ReferenceItems.Save();
					UpdateSolution();
					UpdateProjects();
					UpdateReferences();
					Global.MainWindow.InfoPanel.RemoveTask(TaskName.Update);
					break;
				default:
					break;
			}
		}
	}
}
