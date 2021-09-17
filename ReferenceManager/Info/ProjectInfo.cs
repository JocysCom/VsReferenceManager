using Microsoft.Build.Construction;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace JocysCom.VS.ReferenceManager.Info
{
	public class ProjectInfo
	{
		// Path to project file
		public FileInfo File;
		public string ProjectName;
		public string RelativePath;
		public string ProjectGuid;
		public SolutionProjectType ProjectType;
		public string AssemblyName;
		public string ConfigName;
		public string OutputName;
		public string CategorySolution;
		public string CategoryFeature;
		public string CompanyName;
		public string CompanyDomain;
		public string ProductName;
		public string FileVersion;
		public string Description;
		public List<string> Problems { get; set; } = new List<string>();
		/// <summary>
		/// Recommended server name where projects should be deployed.
		/// </summary>
		public string ServerName;
		/// <summary>
		/// Recommended path where projects should be deployed.
		/// </summary>
		public string ServerPath;
		public ProjectOutputType OutputType;
		public List<ConfigInfo> Configs;

		/// <summary>
		/// Requires AssemblyName and OutputType to be set.
		/// </summary>
		public void UpdateOutputAndConfigName()
		{
			switch (OutputType)
			{
				case ProjectOutputType.Console:
				case ProjectOutputType.WinApp:
				case ProjectOutputType.WinService:
					OutputName = $"{AssemblyName}.exe";
					ConfigName = $"{AssemblyName}.exe.config";
					break;
				case ProjectOutputType.WebApp:
				case ProjectOutputType.WebService:
				case ProjectOutputType.WebSite:
					OutputName = $"bin\\{AssemblyName}.dll";
					ConfigName = "Web.config";
					break;
				case ProjectOutputType.LibService:
					OutputName = $"bin\\{AssemblyName}.dll";
					ConfigName = $"{AssemblyName}.exe.config";
					break;
				default:
					OutputName = $"{AssemblyName}.dll";
					ConfigName = $"{AssemblyName}.dll.config";
					break;
			}
		}

		public void FillCategory()
		{
			if (!string.IsNullOrEmpty(AssemblyName))
			{
				CategorySolution = AssemblyName.Split(' ').FirstOrDefault();
				CategoryFeature = AssemblyName.Split(' ').Skip(1).FirstOrDefault();
			}
			//CategorySolution = ProductName.Split(' ').FirstOrDefault();
			//CategoryFeature = ProductName.Split(' ').Skip(1).FirstOrDefault();
		}


		/// <summary>
		/// Requires OutputType, CompanyName and ProductName to be filled.
		/// </summary>
		/// <param name="item"></param>
		public void UpdateServerNameAndServerPath()
		{
			switch (OutputType)
			{
				// Services and console.
				case ProjectOutputType.LibService:
				case ProjectOutputType.Console:
				case ProjectOutputType.WinService:
					ServerName = "APP01";
					ServerPath = $"\\Program Files\\{CompanyName}\\{ProductName}";
					break;
				case ProjectOutputType.WebApp:
				case ProjectOutputType.WebSite:
				case ProjectOutputType.WebService:
					ServerName = "WEBCLST01";
					ServerPath = $"\\inetpub\\{CompanyName}.com\\{ProductName.Replace(" ", "")}";
					//p = string.Format(@"\inetpub\{0}.com\{1}", (CompanyName ?? "").ToLower(), ProductName.Replace(" ", ""));
					//p = string.Format(@"\inetpub\{0}\{1}", CompanyDomain, AssemblyName.Split('.').LastOrDefault());
					break;
				case ProjectOutputType.WinApp:
					ServerName = "RDSH01";
					ServerPath = $"\\Program Files\\{CompanyName}\\{ProductName}\\{FileVersion}".TrimEnd('\\', ' ');
					break;
				default:
					break;
			}
		}

		/// <summary>
		///  Requires File to be set.
		/// </summary>
		void ValidateAssemblyInfo(bool updateAssemblyFile = false)
		{
			// Websites don't have AssemblyInfo.cs
			// Note: WebSites can be converted to WebApp
			// https://devblogs.microsoft.com/aspnet/converting-a-web-site-project-to-a-web-application-project/
			if (OutputType == ProjectOutputType.WebSite)
				return;
			var dir = File.Directory.GetDirectories("*Properties").FirstOrDefault();
			if (dir == null)
				dir = File.Directory.GetDirectories("*My Project").FirstOrDefault();
			if (dir == null)
			{
				Problems.Add("AssemblyInfo: Properties/My Project folder not found.");
				return;
			}
			var files = dir.GetFiles("AssemblyInfo.cs", SearchOption.AllDirectories);
			if (files.Length == 0)
				files = dir.GetFiles("AssemblyInfo.vb", SearchOption.AllDirectories);
			if (files.Length == 0)
			{
				Problems.Add("AssemblyInfo file: not found.");
				return;
			}
			if (files.Length > 1)
			{
				Problems.Add($"AssemblyInfo.cs: {files.Length} files found.");
				return;
			}
			var contents = System.IO.File.ReadAllText(files[0].FullName);
			//[assembly: AssemblyTitle("<CompanyName> <ProductName>")]
			var assemblyTitle = InfoHelper.GetAssemblyValue("AssemblyTitle", contents);
			//[assembly: AssemblyCompany("<CompanyName>")]
			var assemblyCompany = InfoHelper.GetAssemblyValue("AssemblyCompany", contents);
			//[assembly: AssemblyProduct("<ProductName>")]
			var assemblyProduct = InfoHelper.GetAssemblyValue("AssemblyProduct", contents);
			//[assembly: AssemblyCopyright("Copyright © <FullCompanyName> 2019")]
			var assemblyCopyright = InfoHelper.GetAssemblyValue("AssemblyCopyright", contents);
			//[assembly: AssemblyDescription("<Description>")]
			var assemblyDescription = InfoHelper.GetAssemblyValue("AssemblyDescription", contents);
			// If this number changes, other assemblies have to update their references to your assembly!
			// Better to change this number if types or method-signatures have been removed or changed.
			//[assembly: AssemblyVersion("1.0.0.0")]
			var assemblyVersion = InfoHelper.GetAssemblyValue("AssemblyVersion", contents);
			// Used by setup programs. You can increase this number for every deployment.
			//[assembly: AssemblyFileVersion("1.0.0.0")]
			var assemblyFileVersion = InfoHelper.GetAssemblyValue("AssemblyFileVersion", contents);
			// Optional. AssemblyFileVersion is used if not specified. Use when talking to customers or for display on your website. 
			//[assembly: AssemblyInformationalVersion("1.0 RC1")]
			var assemblyInformationalVersion = InfoHelper.GetAssemblyValue("AssemblyInformationalVersion", contents);
			// Check values
			var versionRx = new Regex(@"^[0-9.]+$");
			if (versionRx.IsMatch(assemblyFileVersion ?? ""))
				FileVersion = assemblyFileVersion;
			Description = (assemblyDescription ?? "").Trim();
			// Fill empty values.
			if (string.IsNullOrEmpty(CompanyName))
				CompanyName = (assemblyCompany ?? "").Trim();
			if (string.IsNullOrEmpty(ProductName))
				ProductName = (assemblyProduct ?? "").Trim();
			if (string.IsNullOrEmpty(FileVersion))
				FileVersion = (assemblyVersion ?? "").Trim();
			// Check for problems.
			var problems = new List<string>();
			// Check company.
			if (string.IsNullOrEmpty(assemblyCompany))
				problems.Add("Company is empty");
			else if (CompanyName != assemblyCompany)
				problems.Add(string.Format("Company mismatch: '{0}'.", assemblyCompany));
			// Check product.
			if (string.IsNullOrEmpty(assemblyProduct))
				problems.Add("Product is empty");
			else if (ProductName != assemblyProduct)
				problems.Add(string.Format("Product mismatch: '{0}'. Expected: {1}.", assemblyProduct, ProductName));
			// Check title.
			var title = string.Format("{0} {1}", CompanyName, ProductName);
			if (title != assemblyTitle)
				problems.Add(string.Format("Title mismatch: '{0}'.", title));
			// Check description.
			if (string.IsNullOrEmpty(assemblyDescription))
				problems.Add("Description is empty");
			if (problems.Count > 0 && updateAssemblyFile)
			{
				Problems.Add("AssemblyInfo: " + string.Join(", ", problems));
				// Fix space.
				contents = contents.Replace("[assembly :", "[assembly:");
				contents = contents.Replace("<Assembly :", "<Assembly:");
				InfoHelper.SetAssemblyValue("AssemblyCopyright", "Copyright © CityFleet Networks Limited 2019", ref contents);
				InfoHelper.SetAssemblyValue("AssemblyCompany", "CityFleet", ref contents);
				InfoHelper.SetAssemblyValue("AssemblyTitle", title, ref contents);
				InfoHelper.SetAssemblyValue("AssemblyProduct", ProductName, ref contents);
				if (string.IsNullOrEmpty(Description))
				{
					InfoHelper.SetAssemblyValue("AssemblyDescription", ProductName, ref contents);
				}
				//SetAssemblyValue("AssemblyTitle", "CityFleet", ref contents);
				System.IO.File.WriteAllText(files[0].FullName, contents);
			}
		}

	}

}
