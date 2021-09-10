using System.Collections.Generic;

namespace JocysCom.VS.ReferenceManager
{
	public class ProjectUpdaterParam
	{
		public Dictionary<VSLangProj.VSProject, IList<ReferenceItem>> Data = new Dictionary<VSLangProj.VSProject, IList<ReferenceItem>>();
	}
}
