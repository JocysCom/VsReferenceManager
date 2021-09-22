using JocysCom.ClassLibrary.Controls;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace JocysCom.VS.ReferenceManager.Controls
{

	public partial class SolutionsControl : UserControl
	{
		public SolutionsControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
		}

		private void ScanLocationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
		}

		bool Contains(string path)
		{
			return Global.AppSettings.ScanLocations
				.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase));
		}

	}
}