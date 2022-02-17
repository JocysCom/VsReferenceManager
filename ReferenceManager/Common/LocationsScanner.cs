using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.IO;
using JocysCom.VS.ReferenceManager.Info;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace JocysCom.VS.ReferenceManager
{
	public class LocationsScanner : IScanner
	{

		public LocationsScanner()
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
				? ff.GetFiles("*.csproj;*.vbproj;*.sln", true, dirs)
				// Scan specific file.
				: ff.GetFiles(fileName, false, dirs);
			// Step 2: Scan files.
			for (var i = 0; i < files.Count; i++)
			{
				var file = files[i];
				// If file doesn't exist in the game list then continue.
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
				var name = file.FullName;
				e.SubMessage = $"File: {name} ({size})";
				Report(e);
				// Get info by full name.
				var info = currentInfo.FirstOrDefault(x => x.FullName.ToLower() == fileName);
				var isSolution = file.Extension.Equals(".sln", StringComparison.InvariantCultureIgnoreCase);
				e.SubData = isSolution
					? ParseSolutionFile(file.FullName)
					: ParseProjectFile(file.FullName);
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
		/// Create ReferenceItem list from project file.
		/// </summary>
		/// <param name="fileName">File name to check.</param>
		/// <param name="searchOption">If not specified then check specified file only.</param>
		/// <returns></returns>
		public List<ReferenceItem> ParseProjectFile(string fileName)
		{
			var data = new List<ReferenceItem>();
			var projectNode = XElement.Load(fileName);
			XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
			var referenceNodes = projectNode.Descendants(ns + "ItemGroup").Descendants(ns + "Reference").ToArray();
			var projectReferenceNodes = projectNode.Descendants(ns + "ItemGroup").Descendants(ns + "ProjectReference").ToArray();
			var projectName = System.IO.Path.GetFileNameWithoutExtension(fileName);
			var assemblyNodes = projectNode.Descendants(ns + "PropertyGroup").Descendants(ns + "AssemblyName").ToArray();
			var assemblyName = assemblyNodes.FirstOrDefault()?.Value ?? projectName;
			var versionNodes = projectNode.Descendants(ns + "PropertyGroup").Descendants(ns + "TargetFrameworkVersion").ToArray();
			var versionValue = versionNodes.FirstOrDefault()?.Value;
			var fi = new FileInfo(fileName);
			var getNewItem = new Func<string, string, string, ReferenceItem>((name, path, project) =>
				{
					// Fill current project values.
					var item = new ReferenceItem();
					item.ProjectPath = fileName;
					item.ProjectName = projectName;
					item.ProjectAssemblyName = assemblyName;
					item.ProjectFrameworkVersion = versionValue;
					// Fill reference part.
					var fullPath = "";
					if (!string.IsNullOrEmpty(path))
						fullPath = PathHelper.GetPathRooted(fi.Directory.FullName + "\\", path);
					item.ReferenceName = name ?? "";
					item.ReferencePath = fullPath ?? "";
					item.ReferenceProject = project ?? "";
					item.ReferenceExists = System.IO.File.Exists(fullPath);
					return item;
				});
			// Add project without references.
			var projectItem = getNewItem(null, null, null);
			projectItem.ItemType = ItemType.Project;
			data.Add(projectItem);
			for (int i = 0; i < referenceNodes.Count(); i++)
			{
				// <Reference Include="NameSpace.ProjectName">
				//   <HintPath>..\Folder\NameSpace.ProjectName</HintPath>
				// </Reference>
				var rn = referenceNodes[i];
				var name = rn.Attributes("Include").FirstOrDefault()?.Value;
				var path = rn.Descendants(ns + "HintPath").FirstOrDefault()?.Value;
				var item = getNewItem(name, path, null);
				// If reference have path then...
				if (!string.IsNullOrEmpty(path))
				{
					item.ItemType = ItemType.Reference;
					data.Add(item);
				}
			}
			for (int i = 0; i < projectReferenceNodes.Count(); i++)
			{
				// <ProjectReference Include="..\Folder\NameSpace.ProjectName.csproj">
				//   <Project>{e4ed2d0f-2e78-8e2f-884c-4bc26b1ac5e4}</Project>
				//   <Name>NameSpace.ProjectName</Name>
				// </ProjectReference>
				var rn = projectReferenceNodes[i];
				var name = rn.Descendants(ns + "Name").FirstOrDefault()?.Value;
				var path = rn.Attributes("Include").FirstOrDefault()?.Value;
				var proj = rn.Descendants(ns + "Project").FirstOrDefault()?.Value;
				var item = getNewItem(name, path, proj);
				// If reference have path then...
				if (!string.IsNullOrEmpty(path))
				{
					item.ItemType = ItemType.Reference;
					data.Add(item);
				}
			}
			// Fill data here.
			return data;
		}

		/// <summary>
		/// Get all solutions with list of all projects in them.
		/// </summary>
		/// <param name="dir">Root folder to search for solutions.</param>
		public List<ReferenceItem> ParseSolutionFile(string fileName)
		{
			var ri = new ReferenceItem();
			ri.SolutionName = System.IO.Path.GetFileNameWithoutExtension(fileName);
			ri.SolutionPath = fileName;
			ri.ItemType = ItemType.Solution;
			var solution = Microsoft.Build.Construction.SolutionFile.Parse(ri.SolutionPath);
			var solutionProjects = solution.ProjectsInOrder;
			for (int p = 0; p < solutionProjects.Count; p++)
			{
				var sp = solutionProjects[p];
				// If path starts with "http[s]://" then skip.
				if (sp.RelativePath.StartsWith("http://") || sp.RelativePath.StartsWith("https://"))
					continue;
				// Get full path.
				var fullName = Path.Combine(ri.SolutionPath, sp.RelativePath);
				// Fix dot notations.
				fullName = Path.GetFullPath(fullName);
				if (ri.Projects.Any(x => x.File.FullName == fullName))
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
				pi.ProjectName = sp.ProjectName;
				pi.RelativePath = sp.RelativePath;
				pi.ProjectGuid = sp.ProjectGuid;
				pi.ProjectType = sp.ProjectType;
				pi.OutputType = InfoHelper.GetOutputType(pi.File.FullName, sp.ProjectName);
				pi.AssemblyName = InfoHelper.GetElementInnerText(pi.File.FullName, "AssemblyName");
				ri.Projects.Add(pi);
			}
			// Fill data here.
			return new List<ReferenceItem>() { ri };
		}



	}
}
