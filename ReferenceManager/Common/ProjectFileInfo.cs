using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace JocysCom.VS.ReferenceManager
{
	public class ProjectFileInfo
	{

		[XmlAttribute]
		public DateTime Modified { get; set; }
		
		[XmlAttribute]
		public long Size { get; set; }

		[XmlAttribute]
		public string FullName { get; set; }

		// Project reference data
		public List<ReferenceItem> Data;

	}
}
