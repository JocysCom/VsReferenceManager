
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

		public void ProcessData(ProjectUpdaterParam param)
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
			var projects = param.Data.Keys.ToList();

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
				var references = param.Data[project];
				// Step 2: Add projects to solution.
				for (int r = 0; r < references.Count; r++)
				{
					var ri = references[r];
					e.SubIndex = r;
					e.SubCount = references.Count;
					e.SubItem = ri;
					e.SubMessage = $"Adding Reference Project to Solution: {ri.ProjectName}";
					Report(e);
					// If project name not available then skip.
					if (string.IsNullOrEmpty(ri.ProjectPath))
						continue;
					// If reference path not available then skip.
					//if (string.IsNullOrEmpty(ri.ReferencePath))
					//	continue;
					VSLangProj.VSProject vsProject = null;
					ControlsHelper.Invoke(() =>
					{
						vsProject = SolutionHelper.GetVsProject(ri.ProjectName);
					});
					// If Project was not found inside current solution then...
					var refProject = vsProject?.Project;
					if (refProject == null)
					{
						ControlsHelper.Invoke(() =>
						{
							// Add project to solution.
							refProject = folder.AddFromFile(ri.ProjectPath);
						});
					}
					e.SubMessage = $"Updating Reference: {ri.ReferenceName}";
					Report(e);
					ControlsHelper.Invoke(() =>
					{
						// Remove reference.
						var reference = SolutionHelper.GetReference(project, ri.ReferenceName);
						if (reference != null)
						{
							reference.Remove();
							// Add Project.
							project.References.AddProject(refProject);
						}
					});
				}
			}
			e = new ProjectUpdaterEventArgs
			{
				State = ProjectUpdaterStatus.Completed
			};
			Report(e);
			ControlsHelper.Invoke(() =>
			{
				Global.MainWindow.HMan.RemoveTask(TaskName.Update);
			});
		}

	}
}
