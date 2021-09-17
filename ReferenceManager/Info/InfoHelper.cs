using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace JocysCom.VS.ReferenceManager.Info
{
	/// <summary>
	/// Helps to gather information and prepare report about folder with projects.
	/// </summary>
	public static partial class InfoHelper
	{

		/// <summary>
		/// Get value from AssemblyFile content.
		/// </summary>
		public static string GetAssemblyValue(string name, string contents)
		{
			Regex rx;
			Match match;
			rx = new Regex(@"\[assembly\s*:\s*" + name + @"\s*\(\s*""?(?<value>[^""\)\]]*)""?\s*\)\s*\]", RegexOptions.Singleline);
			match = rx.Match(contents);
			if (match.Success)
				return match.Groups["value"].Value;
			rx = new Regex(@"<Assembly\s*:\s*" + name + @"\s*\(\s*""?(?<value>[^""\)\]]*)""?\s*\)\s*>", RegexOptions.Singleline);
			match = rx.Match(contents);
			if (match.Success)
				return match.Groups["value"].Value;
			return null;
		}

		/// <summary>
		/// Set value inside AssemblyFile content.
		/// </summary>
		public static void SetAssemblyValue(string name, string value, ref string contents)
		{
			var rxCs = new Regex(@"\[assembly\s*:\s*" + name + @"\s*\(\s*""?(?<value>[^""\)\]]*)""?\s*\)\s*\]", RegexOptions.Singleline);
			contents = rxCs.Replace(contents, string.Format("[assembly: {0}(\"{1}\")]", name, value));
			var rxVb = new Regex(@"<Assembly\s*:\s*" + name + @"\s*\(\s*""?(?<value>[^""\)\]]*)""?\s*\)\s*\]", RegexOptions.Singleline);
			contents = rxVb.Replace(contents, string.Format("<Assembly: {0}(\"{1}\")]", name, value));
		}

		/// <summary>
		///  Get project output type from *.*proj file.
		/// </summary>
		static ProjectOutputType GetOutputType(string path, string projectName)
		{
			var input = File.ReadAllText(path);
			var rx = new Regex("<OutputType>(?<type>\\w+)</OutputType>", RegexOptions.IgnoreCase);
			var match = rx.Match(input);
			var outputType = string.Empty;
			if (match.Success)
				outputType = match.Groups["type"].Value;
			var isExe = outputType.ToUpper().Contains("EXE");
			var isWin = outputType.ToUpper().Contains("WIN");
			var isLib = outputType.ToUpper().Contains("LIBRARY");
			var rxWeb = new Regex("web\\.config", RegexOptions.IgnoreCase);
			var rxSrv = new Regex("Service|Server|API|\\.Ws", RegexOptions.IgnoreCase);
			var rxTst = new Regex("Tester", RegexOptions.IgnoreCase);
			//if (rxTst.IsMatch(projectName))
			var isSrv = rxSrv.IsMatch(projectName) && !rxTst.IsMatch(projectName);
			var isWeb =
				rxWeb.IsMatch(input) ||
				path.EndsWith("website.publishproj");
			if (isWeb)
				return isSrv
					? ProjectOutputType.WebService
					: path.EndsWith("website.publishproj")
						? ProjectOutputType.WebSite
						: ProjectOutputType.WebApp;
			if (isExe)
				return isWin
					? ProjectOutputType.WinApp
					: isSrv
						? ProjectOutputType.WinService
						: ProjectOutputType.Console;
			if (isLib)
				return
					isSrv
					? ProjectOutputType.LibService
					: ProjectOutputType.Library;
			return ProjectOutputType.Unknown;
		}


		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static T[] GetAll<T>(XmlNode node, bool includeTop = false)
		{
			if (node == null)
				return new T[0];
			var type = typeof(T);
			// Get all child controls.
			var nodes = node.ChildNodes.Cast<XmlNode>().ToList();
			// Get children of controls and flatten resulting sequences into one sequence.
			var result = nodes.SelectMany(x => GetAll<XmlNode>(x)).ToList();
			// Merge controls with their children.
			result.AddRange(nodes);
			// Include top control if required.
			if (includeTop)
				result.Add(node);
			// Filter controls by type.
			result = result.Where(x => type.IsAssignableFrom(x.GetType())).ToList();
			// Cast to required type.
			var result2 = result.Select(x => (T)(object)x).ToArray();
			return result2;
		}

		static string GetElementInnerText(string path, string tag)
		{
			var rx = new Regex("<" + tag + ">(?<type>[\\w\\. ]+)</" + tag + ">", RegexOptions.IgnoreCase);
			var projContent = File.ReadAllText(path);
			var match = rx.Match(projContent);
			if (!match.Success)
				return null;
			var value = match.Groups["type"].Value;
			return value;
		}


		static void GetConfigurations(DirectoryInfo dir, out List<ConfigInfo> configs)
		{
			configs = new List<ConfigInfo>();
			var ff = new ClassLibrary.IO.FileFinder();
			var files = ff.GetFiles("*.config", true, dir.FullName);
			Console.Write("Analyse config file: ");
			for (int i = 0; i < files.Count; i++)
			{
				var ci = new ConfigInfo();
				ci.File = files[i];
				List<SqlConnectionStringBuilder> builders;
				List<string> strings;
				GetConnectionStrings(ci.File.FullName, out builders, out strings);
				ci.ConnectionBuilders = builders;
				ci.ConnectionStrings = strings;
				configs.Add(ci);
			}
			Console.WriteLine();
		}

		/// <summary>
		/// Extract all connection strings from XML configuration files.
		/// </summary>
		/// <param name="configFile"></param>
		/// <param name="builders"></param>
		/// <param name="strings"></param>
		static void GetConnectionStrings(string configFile, out List<SqlConnectionStringBuilder> builders, out List<string> strings)
		{
			builders = new List<SqlConnectionStringBuilder>();
			strings = new List<string>();
			var doc = new XmlDocument();
			doc.Load(configFile);
			var elements = GetAll<XmlElement>(doc.DocumentElement);
			var attributes = elements.SelectMany(x => x.Attributes.Cast<XmlAttribute>()).ToArray();
			// Look for connection strings inside attributes
			foreach (var attribute in attributes)
			{
				var builder = GetConnectionString(attribute.Value, configFile);
				if (builder == null)
					continue;
				var cs = GetBasicConnection(builder).ToUpper();
				if (strings.Contains(cs))
					continue;
				strings.Add(cs);
				builders.Add(builder);
			}
			// Look for connection strings inside elements.
			foreach (var element in elements)
			{
				// If element has other elements as nodes then continue.
				if (element.ChildNodes.OfType<XmlElement>().Any())
					continue;
				var builder = GetConnectionString(element.InnerText, configFile);
				if (builder == null)
					continue;
				var cs = GetBasicConnection(builder).ToUpper();
				if (strings.Contains(cs))
					continue;
				strings.Add(cs);
				builders.Add(builder);
			}
		}

		static SqlConnectionStringBuilder GetConnectionString(string connectionString, string filename = null)
		{
			if (string.IsNullOrEmpty(connectionString))
				return null;
			if (!connectionString.Contains('='))
				return null;
			if (connectionString.Contains("PublicKeyToken="))
				return null;
			if (connectionString.Contains("CN=MemberAccountServerCertificate"))
				return null;
			var rx = new Regex(@"ApplicationName\s*=\s*\w+;", RegexOptions.IgnoreCase);
			connectionString = rx.Replace(connectionString, "");
			if (
				// Server synonyms
				connectionString.ToUpper().Contains("DATA SOURCE") ||
				//connectionString.ToUpper().Contains("ADDR") ||
				//connectionString.ToUpper().Contains("ADDRESS") ||
				//connectionString.ToUpper().Contains("NETWORKADDRESS") ||
				connectionString.ToUpper().Contains("SERVER") ||
				// Database synonyms
				connectionString.ToUpper().Contains("DATABASE") ||
				connectionString.ToUpper().Contains("CATALOG") // Initial Catalog
			)
			{
				var ct = "Entity Connection";
				try
				{
					// Try to find entity connection.
					if (connectionString.ToUpper().Contains("METADATA"))
					{
						// Get connection string from entity connection string.
						var ecsb = new System.Data.EntityClient.EntityConnectionStringBuilder(connectionString);
						connectionString = ecsb.ProviderConnectionString;
					}
					ct = "SQL Connection";
					var builder = new SqlConnectionStringBuilder(connectionString);
					return builder;
				}
				catch (Exception ex)
				{
					Console.WriteLine("\r\n{0}: {1}", ct, connectionString);
					Console.WriteLine("Error: {0}", ex.Message);
					if (!string.IsNullOrEmpty(filename))
						Console.WriteLine("File: {0}", filename);
					return null;
				}
			}
			return null;

		}

		static string GetBasicConnection(SqlConnectionStringBuilder builder)
		{
			var s = string.Format("Server={0};Database={1}", builder.DataSource.ToUpper(), builder.InitialCatalog);
			if (builder.IntegratedSecurity)
			{
				s += ";Integrated Security=true";
			}
			else
			{
				if (!string.IsNullOrEmpty(builder.UserID))
					s += string.Format(";User ID={0}", builder.UserID);
				if (!string.IsNullOrEmpty(builder.Password))
					s += string.Format(";Password=********");
			}
			return s;
		}

		#region Solution and Project Methods


		static FileInfo[] GetFiles(DirectoryInfo dir, string searchPattern)
		{
			Console.Write("Searching for {0}...", searchPattern);
			var files = dir.GetFiles(searchPattern, SearchOption.AllDirectories).OrderBy(x => x.FullName).ToArray();
			// Exclude all build folders.
			var rxBuild = new Regex(@"\\obj\\|\\bin\\", RegexOptions.IgnoreCase);
			files = files.Where(x => !rxBuild.IsMatch(x.FullName)).ToArray();
			Console.WriteLine(" {0} files found.", files.Length);
			return files;
		}

		/// <summary>
		/// Get all solutions with list of all projects in them.
		/// </summary>
		/// <param name="dir">Root folder to search for solutions.</param>
		public static List<SolutionInfo> GetSolutionsWithProjects(string scanPath)
		{
			var solutions = new List<SolutionInfo>();
			// Find all solution files.
			var dir = new DirectoryInfo(scanPath);
			var slns = GetFiles(dir, "*.sln");
			for (int s = 0; s < slns.Length; s++)
			{
				var si = new SolutionInfo();
				si.File = slns[s];
				var solution = Microsoft.Build.Construction.SolutionFile.Parse(si.File.FullName);
				solutions.Add(si);
				var solutionProjects = solution.ProjectsInOrder;
				for (int p = 0; p < solutionProjects.Count; p++)
				{
					var solutionProject = solutionProjects[p];
					var fullName = Path.Combine(si.File.Directory.FullName, solutionProject.RelativePath);
					// Fix dot notations.
					fullName = Path.GetFullPath(fullName);
					if (si.Projects.Any(x => x.File.FullName == fullName))
						continue;
					var pi = new ProjectInfo();
					pi.File = new FileInfo(fullName);
					// if this is folder then...
					if (pi.File.Attributes.HasFlag(FileAttributes.Directory))
					{
						var newPath = Path.Combine(pi.File.Directory.FullName, "website.publishproj");
						pi.File = new FileInfo(newPath);
					}
					// Skip if project exists.
					if (!pi.File.Exists)
						continue;
					pi.ProjectName = solutionProject.ProjectName;
					pi.RelativePath = solutionProject.RelativePath;
					pi.ProjectGuid = solutionProject.ProjectGuid;
					pi.ProjectType = solutionProject.ProjectType;
					pi.OutputType = GetOutputType(pi.File.FullName, solutionProject.ProjectName);
					pi.AssemblyName = GetElementInnerText(pi.File.FullName, "AssemblyName");
					si.Projects.Add(pi);
				}
				solutions.Add(si);
			}
			return solutions;
		}

		#endregion


	}
}
