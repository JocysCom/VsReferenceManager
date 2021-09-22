using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.IO;
using JocysCom.VS.ReferenceManager.Info;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JocysCom.VS.ReferenceManager
{
	public class SolutionScanner : IScanner
	{

		public SolutionScanner()
		{
			ff = new FileFinder();
			ff.FileFound += ff_FileFound;
		}

		private void ff_FileFound(object sender, ProgressEventArgs e)
			=> Report(e);


		#region ■ IProgress

		public event EventHandler<ProgressEventArgs> Progress;

		public void Report(ProgressEventArgs e)
			=> Progress?.Invoke(this, e);

		#endregion

		public DateTime DateStarted => _DateStarted;
		private DateTime _DateStarted;
		public DateTime DateEnded => _DateEnded;
		private DateTime _DateEnded;

		public bool IsStopping { get => ff.IsStopping; set => ff.IsStopping = value; }

		public bool IsPaused { get => ff.IsPaused; set => ff.IsPaused = value; }

		private readonly FileFinder ff;

		public void Scan(string[] paths, IList<ProjectFileInfo> currentInfo, string fileName = null)
		{
			_DateStarted = DateTime.Now;
			IsStopping = false;
			IsPaused = false;
			// Step 1: Get list of files inside the folder.
			var e = new ProgressEventArgs
			{
				State = ProgressStatus.Started
			};
			Report(e);
			var added = 0;
			var updated = 0;
			var winFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			var dirs = paths
				.Select(x => x)
				// Except win folders.
				.Where(x => !x.StartsWith(winFolder, StringComparison.OrdinalIgnoreCase))
				.ToArray();
			// Create list to store file to scan.
			var files = string.IsNullOrEmpty(fileName)
				// Scan all files.
				? ff.GetFiles("*.sln;", true, dirs)
				// Scan specific file.
				: ff.GetFiles(fileName, false, dirs);
			// Step 2: Scan files.
			for (var i = 0; i < files.Count; i++)
			{
				var file = files[i];
				e = new ProgressEventArgs
				{
					TopMessage = $"Scan Files. Added = {added}, Updated = {updated}",
					TopIndex = i,
					TopCount = files.Count,
					TopData = files,
					SubIndex = 0,
					SubCount = 0,
				};
				var size = FileFinder.BytesToString(file.Length);
				e.SubMessage = $"File: {file.FullName} ({size})";
				Report(e);
				// Get info by full name.
				var info = currentInfo.FirstOrDefault(x => x.FullName.ToLower() == fileName);
				var data = FromDisk(file.FullName);
				e.SubData = data;
				// If file doesn't exist current list then...
				if (info == null)
				{
					e.State = ProgressStatus.Updated;
					added++;
				}
				else
				{
					e.State = ProgressStatus.Updated;
					updated++;
				}
				Report(e);
			}
			_DateEnded = DateTime.Now;
			e = new ProgressEventArgs
			{
				State = ProgressStatus.Completed
			};
			Report(e);
		}

		/// <summary>
		/// Get all solutions with list of all projects in them.
		/// </summary>
		/// <param name="dir">Root folder to search for solutions.</param>
		public List<ReferenceItem> FromDisk(string fileName)
		{
			var si = new ReferenceItem();
			si.SolutionName = System.IO.Path.GetFileNameWithoutExtension(fileName);
			si.SolutionPath = fileName;
			var solution = Microsoft.Build.Construction.SolutionFile.Parse(si.SolutionPath);
			var solutionProjects = solution.ProjectsInOrder;
			for (int p = 0; p < solutionProjects.Count; p++)
			{
				var solutionProject = solutionProjects[p];
				var fullName = Path.Combine(si.SolutionPath, solutionProject.RelativePath);
				// Fix dot notations.
				fullName = Path.GetFullPath(fullName);
				if (si.Projects.Any(x => x.File.FullName == fullName))
					continue;
				var pi = new ProjectInfo();
				pi.File = new FileInfo(fullName);
				// if this is folder then...
				if (pi.File.Attributes.HasFlag(FileAttributes.Directory))
				{
					var newPath = Path.Combine(pi.File.Directory.FullName, "website.publishproj");
					pi.File = new FileInfo(newPath);
				}
				// Skip if project exists.
				if (!pi.File.Exists)
					continue;
				pi.ProjectName = solutionProject.ProjectName;
				pi.RelativePath = solutionProject.RelativePath;
				pi.ProjectGuid = solutionProject.ProjectGuid;
				pi.ProjectType = solutionProject.ProjectType;
				pi.OutputType = InfoHelper.GetOutputType(pi.File.FullName, solutionProject.ProjectName);
				pi.AssemblyName = InfoHelper.GetElementInnerText(pi.File.FullName, "AssemblyName");
				si.Projects.Add(pi);
			}
			// Fill data here.
			return new List<ReferenceItem>() { si };
		}

	}
}
