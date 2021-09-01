using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace JocysCom.VS.ReferenceManager
{
	public class ProjectScanner : IProgress<ProjectScannerEventArgs>
	{

		public ProjectScanner()
		{
			ff = new FileFinder();
			ff.FileFound += ff_FileFound;
		}

		#region ■ IProgress

		public event EventHandler<ProjectScannerEventArgs> Progress;

		public void Report(ProjectScannerEventArgs e)
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
			var e = new ProjectScannerEventArgs
			{
				State = ProjectScannerStatus.Started
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
				e = new ProjectScannerEventArgs
				{
					Message = string.Format("Step 2: Scan file {0} of {1}. Please wait...", i + 1, files.Count),
					FileIndex = i,
					Files = files,
					State = ProjectScannerStatus.FileUpdate,
					Added = added,
					Skipped = skipped,
					Updated = updated
				};
				Report(e);
				e = new ProjectScannerEventArgs
				{
					FileInfo = file
				};
				// Get info by full name.
				var info = currentInfo.FirstOrDefault(x => x.FullName.ToLower() == fileName);
				var data = FromDisk(file.FullName);
				e.Data = data;
				// If file doesn't exist current list then...
				if (info == null)
				{
					e.State = ProjectScannerStatus.DataFound;
					added++;
				}
				else
				{
					e.State = ProjectScannerStatus.DataUpdated;
					updated++;
				}
				Report(e);
			}
			_DateEnded = DateTime.Now;
			e = new ProjectScannerEventArgs
			{
				State = ProjectScannerStatus.Completed
			};
			Report(e);
		}

		private void ff_FileFound(object sender, FileFinderEventArgs e)
		{
			var e2 = new ProjectScannerEventArgs
			{
				DirectoryIndex = e.DirectoryIndex,
				Directories = e.Directories,
				FileIndex = e.FileIndex,
				Files = e.Files,
				State = ProjectScannerStatus.DirectoryUpdate,
				Message = string.Format("Step 1: {0} files found. Searching path {1} of {2}. Please wait...", e.Files.Count, e.DirectoryIndex + 1, e.Directories.Count)
			};
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
			var assemblyName = assemblyNodes.Count() > 0
					? assemblyNodes[0].Value
					: projectName;
			var fi = new FileInfo(fileName);
			var getNewItem = new Func<string, string, string, ReferenceItem>((name, path, project) =>
				{
					// Fill current project values.
					var item = new ReferenceItem();
					item.ProjectPath = fileName;
					item.ProjectName = projectName;
					item.ProjectAssemblyName = assemblyName;
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
