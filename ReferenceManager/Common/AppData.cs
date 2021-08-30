using JocysCom.ClassLibrary.ComponentModel;
using System.Collections.Generic;

namespace JocysCom.VS.ReferenceManager
{
	public class AppData : JocysCom.ClassLibrary.Configuration.ISettingsItem
	{
		public SortableBindingList<string> ScanLocations { get; set; }

		public bool Enabled { get; set; }

		public bool IsEmpty =>
			(ScanLocations?.Count ?? 0) == 0;
	}
}
