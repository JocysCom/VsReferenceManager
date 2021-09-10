using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace JocysCom.VS.ReferenceManager
{
	public class ProjectScanner : IProgress<ProjectUpdaterEventArgs>
	{

		public ProjectScanner()
		{
			ff = new FileFinder();
			ff.FileFound += ff_FileFound;
		}

		#region ■ IProgress

		public event EventHandler<ProjectUpdaterEventArgs> Progress;

		public void Report(ProjectUpdaterEventArgs e)
			=> Progress?.Invoke(this, e);

		#endregion

		public DateTime DateStarted => _DateStarted;
		private DateTime _DateStarted;
		public DateTime DateEnded => _DateEnded;
		private DateTime _DateEnded;

		public static SettingsData<ProjectFileInfo> CashedData = new SettingsData<ProjectFileInfo>("ProjectsCache.xml", false, "Project scanner cache.", System.Reflection.Assembly.GetExecutingAssembly());
		private static readonly object FileInfoCacheLock = new object();

		public bool IsStopping { get => ff.IsStopping; set => ff.IsStopping = value; }

		public bool IsPaused { get => ff.IsPaused; set => ff.IsPaused = value; }

		private List<ReferenceItem> GetCachedData(FileInfo fi)
		{
			lock (FileInfoCacheLock)
			{
				var item = CashedData.Items.FirstOrDefault(x => string.Compare(x.FullName, fi.FullName, true) == 0);
				// if Found and changed then...
				if (item != null && (item.Modified != fi.LastWriteTimeUtc || item.Size != fi.Length))
				{
					CashedData.Remove(item);
					item = null;
				}
				if (item == null)
					return null;
				return item.Data;
			}
		}

		private void SetCachedData(FileInfo fi, List<ReferenceItem> data)
		{
			lock (FileInfoCacheLock)
			{
				var item = CashedData.Items.FirstOrDefault(x => string.Compare(x.FullName, fi.FullName, true) == 0);
				if (item != null)
					CashedData.Remove(item);
				item = new ProjectFileInfo
				{
					FullName = fi.FullName,
					Modified = fi.LastWriteTimeUtc,
					Size = fi.Length,
					Data = data,
				};
				CashedData.Add(item);
			}
		}

		private readonly FileFinder ff;

		public void Scan(string[] paths, IList<ProjectFileInfo> currentInfo, string fileName = null)
		{
			_DateStarted = DateTime.Now;
			IsStopping = false;
			IsPaused = false;
			// Step 1: Get list of files inside the folder.
			var e = new ProjectUpdaterEventArgs
			{
				State = ProjectUpdaterStatus.Started
			};
			Report(e);
			var skipped = 0;
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
				? ff.GetFiles("*.csproj;*.vbproj", true, dirs)
				// Scan specific file.
				: ff.GetFiles(fileName, false, dirs);
			// Step 2: Scan files.
			for (var i = 0; i < files.Count; i++)
			{
				var file = files[i];
				// If file doesn't exist in the game list then continue.
				e = new ProjectUpdaterEventArgs
				{
					TopMessage = $"Scan Files. Added = {added}, Skipped = {skipped}, Updated = {updated}",
					TopIndex = i,
					TopCount = files.Count,
					TopData = files,
					SubIndex = 0,
					SubCount = 0,
				};
				var size = file.Length / 1024;
				var name = file.FullName;
				e.SubMessage = $"Current File: {name}";
				Report(e);
				// Get info by full name.
				var info = currentInfo.FirstOrDefault(x => x.FullName.ToLower() == fileName);
				var data = FromDisk(file.FullName);
				e.SubData = data;
				// If file doesn't exist current list then...
				if (info == null)
				{
					e.State = ProjectUpdaterStatus.Updated;
					added++;
				}
				else
				{
					e.State = ProjectUpdaterStatus.Updated;
					updated++;
				}
				Report(e);
			}
			_DateEnded = DateTime.Now;
			e = new ProjectUpdaterEventArgs
			{
				State = ProjectUpdaterStatus.Completed
			};
			Report(e);
		}

		private void ff_FileFound(object sender, FileFinderEventArgs e)
		{
			var e2 = new ProjectUpdaterEventArgs
			{
				TopIndex = e.DirectoryIndex,
				TopCount = e.Directories.Count,
				TopData = e.Directories,
				SubIndex = e.FileIndex,
				SubCount = 0,
				SubData = e.Files,
				State = ProjectUpdaterStatus.Updated,
			};
			e2.TopMessage = $"Scan Folder: {e.Directories[e.DirectoryIndex].FullName}";
			var file = e.Files[e.FileIndex];
			var name = file.FullName;
			e2.SubMessage = $"File: {name}";
			Report(e2);
		}

		/// <summary>
		/// Create ReferenceItem list from project file.
		/// </summary>
		/// <param name="fileName">File name to check.</param>
		/// <param name="searchOption">If not specified then check specified file only.</param>
		/// <returns></returns>
		public List<ReferenceItem> FromDisk(string fileName)
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
					data.Add(item);
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
					data.Add(item);
			}
			// Fill data here.
			return data;
		}

	}
}
