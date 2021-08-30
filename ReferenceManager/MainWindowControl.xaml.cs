using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.Controls;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace JocysCom.VS.ReferenceManager
{
	/// <summary>
	/// Interaction logic for MainWindowControl.
	/// </summary>
	public partial class MainWindowControl : UserControl
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MainWindowControl"/> class.
		/// </summary>
		public MainWindowControl()
		{
			ControlsHelper.InitInvokeContext();
			Global.AppData.Load();
			Global.ReferenceItems.Load();
			if (Global.AppData.Items.Count == 0)
			{
				Global.AppData.Items.Add(new AppData());
				Global.AppData.Save();
			}
			this.InitializeComponent();
			HMan = new BaseWithHeaderManager<TaskName>(HelpHeadLabel, HelpBodyLabel, LeftIcon, RightIcon, this);
			var assembly = Assembly.GetExecutingAssembly();
			//var company = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute))).Company;
			var product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute))).Product;
			//var title = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyTitleAttribute))).Title;
			var description = ((AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute))).Description;
			HMan.SetBodyInfo(description);
			HMan.SetHead(product);
		}

		public BaseWithHeaderManager<TaskName> HMan;

		private void MainWindowPanel_Unloaded(object sender, RoutedEventArgs e)
		{
			Global.AppData.Save();
			Global.ReferenceItems.Save();
		}

	}
}