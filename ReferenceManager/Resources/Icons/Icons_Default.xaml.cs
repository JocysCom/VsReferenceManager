using System.Windows;

namespace JocysCom.VS.ReferenceManager
{
	partial class Icons_Default : ResourceDictionary
	{
		public Icons_Default()
		{
			InitializeComponent();
		}

		public static Icons_Default Current => _Current = _Current ?? new Icons_Default();
		private static Icons_Default _Current;

		public const string Icon_elements_tree = nameof(Icon_elements_tree);
		public const string Icon_folders = nameof(Icon_folders);
		public const string Icon_gearwheel = nameof(Icon_gearwheel);
		public const string Icon_magnifying_glass = nameof(Icon_magnifying_glass);
		public const string Icon_nav_plain = nameof(Icon_nav_plain);
		public const string Icon_objects_exchange = nameof(Icon_objects_exchange);

	}
}
