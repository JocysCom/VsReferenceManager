using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace JocysCom.VS.ReferenceManager.Info
{
	public class ConfigInfo
	{
		public FileInfo File;
		public List<string> ConnectionStrings;
		public List<SqlConnectionStringBuilder> ConnectionBuilders;
	}

}
