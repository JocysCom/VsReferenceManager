using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;

namespace JocysCom.VS.ReferenceManager
{
	public interface IScanner : IProgress<ProgressEventArgs>
	{
		event EventHandler<ProgressEventArgs> Progress;
		void Scan(string[] paths, IList<ProjectFileInfo> currentInfo, string fileName = null);
	}

}
