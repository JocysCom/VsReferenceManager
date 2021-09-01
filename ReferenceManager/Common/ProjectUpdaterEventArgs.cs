namespace JocysCom.VS.ReferenceManager
{
	public class ProjectUpdaterEventArgs
	{
		public ProjectUpdaterStatus State { get; set; }

		// --------------------------
		// Top Level
		// --------------------------

		/// <summary>Current amount of work done by the operation.</summary>
		public int TopIndex { get; set; }
		/// <summary>Total amount of work required to be done by the operation.</summary>
		public int TopCount { get; set; }
		public string TopMessage { get; set; }
		public VSLangProj.VSProject TopItem { get; set; }

		public void ClearTop()
		{
			TopIndex = default;
			TopCount = default;
			TopMessage = default;
			TopItem = default;
		}

		// --------------------------
		// Sub Level
		// --------------------------

		/// <summary>Current amount of work done by the operation.</summary>
		public int SubIndex { get; set; }
		/// <summary>Total amount of work required to be done by the operation.</summary>
		public int SubCount { get; set; }
		public string SubMessage { get; set; }
		public ReferenceItem SubItem { get; set; }

		public void ClearSub()
		{
			SubIndex = default;
			SubCount = default;
			SubMessage = default;
			SubItem = default;
		}

	}
}
