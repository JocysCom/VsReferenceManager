using Microsoft.VisualStudio.Shell;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace JocysCom.VS.ReferenceManager
{
	/// <summary>
	/// This class implements the tool window exposed by this package and hosts a user control.
	/// </summary>
	/// <remarks>
	/// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
	/// usually implemented by the package implementer.
	/// <para>
	/// This class derives from the ToolWindowPane class provided from the MPF in order to use its
	/// implementation of the IVsUIElementPane interface.
	/// </para>
	/// </remarks>
	[Guid("f64b3057-0ebe-4e76-8a7a-d9a70b24f65e")]
	public class MainWindow : ToolWindowPane
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MainWindow"/> class.
		/// </summary>
		public MainWindow() : base(null)
		{
			var assembly = Assembly.GetExecutingAssembly();
			var product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute))).Product;
			this.Caption = product;
			// This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
			// we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
			// the object returned by the Content property.
			Global.MainWindow = new MainWindowControl();
			this.Content = Global.MainWindow;
		}
	}
}
