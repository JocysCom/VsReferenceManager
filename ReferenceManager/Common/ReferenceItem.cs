using JocysCom.ClassLibrary.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace JocysCom.VS.ReferenceManager
{
	public class ReferenceItem : ISettingsItem, INotifyPropertyChanged
	{
		public string SolutionName { get => _SolutionName; set => SetProperty(ref _SolutionName, value); }
		string _SolutionName;

		public string SolutionPath { get => _SolutionPath; set => SetProperty(ref _SolutionPath, value); }
		string _SolutionPath;

		public string ProjectName { get => _ProjectName; set => SetProperty(ref _ProjectName, value); }
		string _ProjectName;

		public string ProjectPath { get => _ProjectPath; set => SetProperty(ref _ProjectPath, value); }
		string _ProjectPath;

		public string ProjectAssemblyName { get => _ProjectAssemblyName; set => SetProperty(ref _ProjectAssemblyName, value); }
		string _ProjectAssemblyName;

		public string ReferenceName { get => _ReferenceName; set => SetProperty(ref _ReferenceName, value); }
		string _ReferenceName;

		public string ReferencePath { get => _ReferencePath; set => SetProperty(ref _ReferencePath, value); }
		string _ReferencePath;

		public string ReferenceProject { get => _ReferenceProject; set => SetProperty(ref _ReferenceProject, value); }
		string _ReferenceProject;

		public bool ReferenceExists { get => _ReferenceExists; set => SetProperty(ref _ReferenceExists, value); }
		bool _ReferenceExists;

		public string StatusText { get => _StatusText; set => SetProperty(ref _StatusText, value); }
		string _StatusText;

		public System.Windows.MessageBoxImage StatusCode { get => _StatusCode; set => SetProperty(ref _StatusCode, value); }
		System.Windows.MessageBoxImage _StatusCode;

		public string ProjectFrameworkVersion { get => _ProjectFrameworkVersion; set => SetProperty(ref _ProjectFrameworkVersion, value); }
		string _ProjectFrameworkVersion;

		public bool IsChecked
		{
			get => _IsChecked;
			set => SetProperty(ref _IsChecked, value);
		}
		bool _IsChecked;

		public bool IsEnabled { get => _IsEnabled; set => SetProperty(ref _IsEnabled, value); }
		bool _IsEnabled;

		public ItemType ItemType { get => _ItemType; set => SetProperty(ref _ItemType, value); }
		ItemType _ItemType;

	public bool IsReference =>
			string.IsNullOrEmpty(ProjectName) && !string.IsNullOrEmpty(ReferenceName);

		[XmlIgnore]
		public object Tag;

		[XmlIgnore]
		public List<Info.ProjectInfo> Projects { get; set; } = new List<Info.ProjectInfo>();

		#region ■ ISettingsItem
		bool ISettingsItem.Enabled { get => IsEnabled; set => IsEnabled = value; }

		public bool IsEmpty =>
			string.IsNullOrEmpty(SolutionName) &&
			string.IsNullOrEmpty(SolutionPath) &&
			string.IsNullOrEmpty(ProjectName) &&
			string.IsNullOrEmpty(ProjectPath) &&
			string.IsNullOrEmpty(ProjectAssemblyName) &&
			string.IsNullOrEmpty(ReferenceName) &&
			string.IsNullOrEmpty(ReferencePath) &&
			string.IsNullOrEmpty(ReferenceProject);

		#endregion

		#region ■ INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected void SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
		{
			property = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}
}
