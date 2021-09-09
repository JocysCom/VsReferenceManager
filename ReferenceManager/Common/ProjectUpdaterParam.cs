using System.Collections.Generic;

namespace JocysCom.VS.ReferenceManager
{
	public class ProjectUpdaterParam
	{
		public Dictionary<VSLangProj.VSProject, List<ReferenceItem>> Data = new Dictionary<VSLangProj.VSProject, List<ReferenceItem>>();
	}
}
