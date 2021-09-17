using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
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
		/// Read assembly files and return version.
		/// </summary>
		public static string FindVersion(string assemblyVersion, params string[] paths)
		{
			if (string.IsNullOrEmpty(assemblyVersion))
				return null;
			if (assemblyVersion == "1.0.*")
				return assemblyVersion;
			var rx = new Regex("\\s+");
			var p = assemblyVersion.Split('.').LastOrDefault().Replace("FileVersion", "Version").Replace("Version", "FileVersion");
			//Console.WriteLine("FindVersion: {0}", assemblyVersion);
			foreach (var path in paths)
			{
				var fi = new FileInfo(path);
				if (!fi.Exists)
				{
					//Console.WriteLine("FindVersion(...): file not found: {0}", path);
					continue;
				}
				//Console.WriteLine("    {0}", path);
				var contents = File.ReadAllText(path);
				// remove all spaces to make ti easier
				var mvRx = new Regex(@"string\s+MajorVersion\s*=\s*""(?<version>[0-9.]+)""");
				var mvMatch = mvRx.Match(contents);
				if (mvMatch.Success)
				{
					//Console.WriteLine("MajorVersion: {0}", mvMatch.Groups["version"].Value);
					var verRx = new Regex(@"string\s+" + p + @"\s*=\s*MajorVersion\s*\+\s*""(?<version>[0-9.]+)""");
					var verMatch = verRx.Match(contents);
					if (verMatch.Success)
					{
						var ver = string.Format("{0}{1}", mvMatch.Groups["version"].Value, verMatch.Groups["version"].Value);
						Console.WriteLine("{0}: {1}", p, ver);
						return ver;
					}
				}
			}
			Console.WriteLine("{0}: version not found", p);
			return null;
		}

		/// <summary>
		/// Get value from AssemblyFile content.
		/// </summary>
		static string GetAssemblyValue(string name, string contents)
		{
			var rx = new Regex(@"\[assembly\s*:\s*" + name + @"\s*\(\s*""?(?<value>[^""\)\]]*)""?\s*\)\s*\]", RegexOptions.Singleline);
			var match = rx.Match(contents);
			return match.Success
				? match.Groups["value"].Value
				: null;
		}

		/// <summary>
		/// Set value inside AssemblyFile content.
		/// </summary>
		static void SetAssemblyValue(string name, string value, ref string contents)
		{
			var rx = new Regex(@"\[assembly\s*:\s*" + name + @"\s*\(\s*""?(?<value>[^""\)\]]*)""?\s*\)\s*\]", RegexOptions.Singleline);
			contents = rx.Replace(contents, string.Format("[assembly: {0}(\"{1}\")]", name, value));
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
			var rxSrv = new Regex("Service|Server|API", RegexOptions.IgnoreCase);
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
		/// Get all child nodes.
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
				if (!strings.Contains(cs))
				{
					strings.Add(cs);
					builders.Add(builder);
				}
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
				if (!strings.Contains(cs))
				{
					strings.Add(cs);
					builders.Add(builder);
				}
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


	}
}
