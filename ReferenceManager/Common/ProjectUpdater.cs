
using EnvDTE;
using EnvDTE80;
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JocysCom.VS.ReferenceManager
{
	public partial class ProjectUpdater : IProgress<ProjectUpdaterEventArgs>
	{

		#region ■ IProgress

		public event EventHandler<ProjectUpdaterEventArgs> Progress;

		public void Report(ProjectUpdaterEventArgs e)
			=> Progress?.Invoke(this, e);

		#endregion

		public void ProcessData(List<VSLangProj.VSProject> projects, List<ReferenceItem> references)
		{
			var e = new ProjectUpdaterEventArgs();
			// Create "References" solution folder.
			e.TopMessage = "Checking 'References' virtual solution folder...";
			e.State = ProjectUpdaterStatus.Started;
			Report(e);
			SolutionFolder folder = null;
			ControlsHelper.Invoke(() =>
			{
				folder = SolutionHelper.GetOrCreateReferencesFolder();
			});
			for (int p = 0; p < projects.Count; p++)
			{
				VSLangProj.VSProject project = null;
				ControlsHelper.Invoke(() =>
				{
					ThreadHelper.ThrowIfNotOnUIThread();
					project = projects[p];
					e.State = ProjectUpdaterStatus.Updated;
					e.TopIndex = p;
					e.TopCount = projects.Count;
					e.TopItem = project;
					e.TopMessage = $"Project: {project.Project.Name}";
					e.ClearSub();
					Report(e);
				});
				var addedProjects = new Dictionary<ReferenceItem, Project>();
				// Step 2: Add projects to solution.
				for (int r = 0; r < references.Count; r++)
				{
					var ri = references[r];
					e.SubIndex = r;
					e.SubCount = references.Count;
					e.SubItem = ri;
					e.SubMessage = $"Step 2: Adding Project to Solution: {ri.ProjectName}";
					Report(e);
					// If project name not available then skip.
					if (string.IsNullOrEmpty(ri.ProjectPath))
						continue;
					// If reference path not available then skip.
					if (string.IsNullOrEmpty(ri.ReferencePath))
						continue;

					VSLangProj.VSProject vsProject = null;
					ControlsHelper.Invoke(() =>
					{
						vsProject = SolutionHelper.GetVsProject(ri.ProjectName);
					});
					// if Project was found inside current solution then...
					if (vsProject != null)
						continue;
					Project refProject = null;
					ControlsHelper.Invoke(() =>
					{
						// Add project to solution.
						refProject = folder.AddFromFile(ri.ProjectPath);
					});
					addedProjects.Add(ri, refProject);
				}
				// Step 3: Remove References add projects.
				var addedReferences = addedProjects.Keys.ToArray();
				for (int r = 0; r < addedProjects.Count; r++)
				{
					var addedRi = addedReferences[r];
					var ri = references[r];
					e.SubIndex = r;
					e.SubCount = references.Count;
					e.SubItem = ri;
					e.SubMessage = $"Step 3: Updating Reference: {ri.ReferenceName}";
					Report(e);
					ControlsHelper.Invoke(() =>
					{
						// Remove reference.
						var reference = SolutionHelper.GetReference(project, addedRi.ReferenceName);
						reference.Remove();
						// Add Project.
						var refProject = addedProjects[addedRi];
						project.References.AddProject(refProject);
					});
				}
				ControlsHelper.Invoke(() =>
				{
					Global.MainWindow.HMan.RemoveTask(TaskName.Update);
				});
			}
		}

	}
}
