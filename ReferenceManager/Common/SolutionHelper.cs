using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

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
			var solution = GetCurrentSolution();
			if (solution == null)
				return null;
			foreach (Project project in solution.Projects)
				if (project.Name == name)
					return project;
			return null;
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

		//public static Project AddProject(string path)
		//{
		//	string prjPath = @"C:\Projects\ClassLibrary1\ClassLibrary1\ClassLibrary1.csproj";
		//	// Add a project to the new solution folder.  
		//	SF.AddFromFile(prjPath);

		//}




	}
}
