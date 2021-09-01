using System;
using System.Collections.Generic;
using System.IO;

namespace JocysCom.VS.ReferenceManager
{
	public class ProjectScannerEventArgs : EventArgs
	{
		public int Level { get; set; }
		public FileInfo FileInfo { get; set; }
		public List<DirectoryInfo> Directories;
		public int DirectoryIndex { get; set; }
		public List<FileInfo> Files;
		public int FileIndex { get; set; }
		public int Skipped { get; set; }
		public int Added { get; set; }
		public int Updated { get; set; }
		public string Message { get; set; }
		public ProjectScannerStatus State { get; set; }
		public List<ReferenceItem>  Data { get; set; }

	}
}
