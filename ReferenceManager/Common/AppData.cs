using JocysCom.ClassLibrary.ComponentModel;
using System.Collections.Generic;

namespace JocysCom.VS.ReferenceManager
{
	public class AppData : JocysCom.ClassLibrary.Configuration.ISettingsItem
	{
		public SortableBindingList<string> ScanLocations
		{
			get
			{
				if (_ScanLocations == null)
					_ScanLocations = new SortableBindingList<string>();
				return _ScanLocations;
			}
			set { _ScanLocations = value; }
		}
		private SortableBindingList<string> _ScanLocations;

		public bool Enabled { get; set; }

		public bool IsEmpty =>
			(ScanLocations?.Count ?? 0) == 0;
	}
}
