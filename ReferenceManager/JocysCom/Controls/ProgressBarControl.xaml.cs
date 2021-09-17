using System.Windows.Controls;


namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Interaction logic for ProgressBarControl.xaml
	/// </summary>
	public partial class ProgressBarControl : UserControl
	{
		public ProgressBarControl()
		{
			InitializeComponent();
		}

		public void UpdateProgress(ProgressEventArgs e)
		{
			if (e.TopCount > 0)
			{
				if (ProgressLevelTopBar.Maximum != e.TopCount)
					ProgressLevelTopBar.Maximum = e.TopCount;
				if (ProgressLevelTopBar.Value != e.TopIndex)
					ProgressLevelTopBar.Value = e.TopIndex;
			}
			else
			{
				if (ProgressLevelTopBar.Maximum != 100)
					ProgressLevelTopBar.Maximum = 100;
				if (ProgressLevelTopBar.Value != 0)
					ProgressLevelTopBar.Value = 0;
			}
			if (e.SubCount > 0)
			{
				if (ProgressLevelSubBar.Maximum != e.SubCount)
					ProgressLevelSubBar.Maximum = e.SubCount;
				if (ProgressLevelSubBar.Value != e.SubIndex)
					ProgressLevelSubBar.Value = e.SubIndex;
			}
			else
			{
				if (ProgressLevelSubBar.Maximum != 100)
					ProgressLevelSubBar.Maximum = 100;
				if (ProgressLevelSubBar.Value != 0)
					ProgressLevelSubBar.Value = 0;
			}
			// Create top message.
			var tm = "";
			if (e.TopCount > 0)
			{
				ControlsHelper.SetText(ProgressLevelTopBarTextBlock, $"{e.TopIndex}/{e.TopCount}");
				tm += $"{e.TopIndex}/{e.TopCount} - ";
			}
			tm += $"{e.TopMessage}";
			// Create sub message.
			var sm = $"{e.SubIndex}";
			if (e.SubCount > 0)
			{
				ControlsHelper.SetText(ProgressLevelSubBarTextBlock, $"{e.SubIndex}/{e.SubCount}");
				sm += $"/{e.SubCount}";
			}
			sm += $" - {e.SubMessage}";
			UpdateProgress(tm, sm);
		}

		public void UpdateProgress(string topText = "", string SubText = "", bool? resetBars = null)
		{
			ControlsHelper.SetText(ProgressLevelTopLabel, topText);
			ControlsHelper.SetText(ProgressLevelSubLabel, SubText);
			if (resetBars.GetValueOrDefault())
			{
				ProgressLevelTopBar.Maximum = 100;
				ProgressLevelTopBar.Value = 0;
				ProgressLevelSubBar.Maximum = 100;
				ProgressLevelSubBar.Value = 0;
			}
			ControlsHelper.SetVisible(ScanProgressPanel, !string.IsNullOrEmpty(topText));
		}
	}
}
