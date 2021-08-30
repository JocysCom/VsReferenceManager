using EnvDTE;
using EnvDTE80;
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
			ProjectListPanel.MainDataGrid.SelectionChanged += ProjectsListPanel_MainDataGrid_SelectionChanged;
			SolutionListPanel.MainDataGrid.SelectionChanged += SolutionListPanel_MainDataGrid_SelectionChanged;
			ReferencesListPanel.UpdateButton.Click += ReferencesListPanel_UpdateButton_Click;
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
			=> ReferencesListPanel.ReferenceList;

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

		private void ReferencesListPanel_UpdateButton_Click(object sender, RoutedEventArgs e)
		{
				ThreadHelper.ThrowIfNotOnUIThread();
			var form = new MessageBoxWindow();
			var result = form.ShowDialog("Replace references with projects?", "Update", MessageBoxButton.OKCancel, MessageBoxImage.Question);
			if (result != MessageBoxResult.OK)
				return;
			var selectedVsProject = GetSelectedVsProject();
			if (selectedVsProject == null)
				return;
			// Step 1: Create "References" solution folder.
			var folder = SolutionHelper.GetOrCreateReferencesFolder();
			// Step 2: Find original projects.
			foreach (var item in ReferenceList)
			{
				// find project for reference.
				var projects = Global.ReferenceItems.Items.Where(x => x.ProjectAssemblyName == item.ReferenceName && x.IsProject).ToList();
				if (projects.Count() > 0)
				{
					var p = projects[0];
					item.ProjectName = p.ProjectName;
					item.ProjectPath = p.ProjectPath;
					item.ProjectAssemblyName = p.ProjectAssemblyName;
					item.StatusCode = MessageBoxImage.Information;
					item.StatusText = "Project Found";
				}
			}
			var addedProjects = new Dictionary<ReferenceItem, Project>();
			// Step 3: Add projects to solution.
			foreach (var item in ReferenceList)
			{
				// If project name not available then skip.
				if (string.IsNullOrEmpty(item.ProjectPath))
					continue;
				// If reference path not available then skip.
				if (string.IsNullOrEmpty(item.ReferencePath))
					continue;
				var vsProject = SolutionHelper.GetVsProject(item.ProjectName);
				// if Project was found inside current solution then...
				if (vsProject != null)
					continue;
				// Add project to solution.
				var project = folder.AddFromFile(item.ProjectPath);
				addedProjects.Add(item, project);
			}
			// Step 4: Remove References add projects.
			foreach (var item in addedProjects.Keys)
			{
				// Remove reference.
				var reference = SolutionHelper.GetReference(selectedVsProject, item.ReferenceName);
				reference.Remove();
				// Add Project.
				var project = addedProjects[item];
				selectedVsProject.References.AddProject(project);
			}
		}

	}
}
