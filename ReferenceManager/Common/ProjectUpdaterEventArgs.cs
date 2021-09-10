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
		public object TopData { get; set; }

		public void ClearTop()
		{
			TopIndex = default;
			TopCount = default;
			TopMessage = default;
			TopData = default;
		}

		// --------------------------
		// Sub Level
		// --------------------------

		/// <summary>Current amount of work done by the operation.</summary>
		public int SubIndex { get; set; }
		/// <summary>Total amount of work required to be done by the operation.</summary>
		public int SubCount { get; set; }
		public string SubMessage { get; set; }
		public object SubData { get; set; }

		public void ClearSub()
		{
			SubIndex = default;
			SubCount = default;
			SubMessage = default;
			SubData = default;
		}

	}
}
