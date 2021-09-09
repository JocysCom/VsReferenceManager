using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Linq;

namespace JocysCom.VS.ReferenceManager
{
	public static class SolutionHelper
	{

		public static DTE2 GetCurrentService()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;
			return dte;
		}

		public static Solution2 GetCurrentSolution()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;
			if (dte == null)
				return null;
			if (dte.Solution.Count == 0)
				return null;
			return dte.Solution as Solution2;
		}

		public static string References = "References";

		public static SolutionFolder GetReferencesFolder()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var solution = GetCurrentSolution();
			if (solution == null)
				return null;
			foreach (var project in solution.Projects)
			{
				var p = project as Project;
				if (p == null)
					continue;
				var f = p.Object as SolutionFolder;
				if (f == null)
					continue;
				if (p.Name != References)
					continue;
				return f;
			}
			return null;
		}

		public static Project GetProject(string name)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var projects = GetAllProjects();
			var project = projects.FirstOrDefault(x => x.Name == name);
			return project;
		}

		public static IList<Project> GetAllProjects()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var list = new List<Project>();
			var solution = GetCurrentSolution();
			if (solution == null)
				return list;
			var projects = solution.Projects;
			var item = projects.GetEnumerator();
			while (item.MoveNext())
			{
				var project = item.Current as Project;
				if (project == null)
					continue;
				if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
					list.AddRange(GetSolutionFolderProjects(project));
				else
					list.Add(project);
			}
			return list;
		}

		private static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var list = new List<Project>();
			for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
			{
				var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
				if (subProject == null)
					continue;
				if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
					list.AddRange(GetSolutionFolderProjects(subProject));
				else
					list.Add(subProject);
			}
			return list;
		}

		public static VSLangProj.VSProject GetVsProject(string name)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var project = GetProject(name);
			return project?.Object as VSLangProj.VSProject;
		}

		public static VSLangProj.Reference GetReference(VSLangProj.VSProject project, string name)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			foreach (VSLangProj.Reference reference in project.References)
				if (reference.Name == name)
					return reference;
			return null;
		}

		public static SolutionFolder GetOrCreateReferencesFolder()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var folder = GetReferencesFolder();
			if (folder != null)
				return folder;
			var solution = GetCurrentSolution();
			// Add a solution folder.
			var project = solution.AddSolutionFolder(References);
			folder = (SolutionFolder)project.Object;
			return folder;
		}

	}
}
