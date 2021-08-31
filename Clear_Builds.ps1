# Removes temporary bin and obj folders.
# ----------------------------------------------------------------------------
# Get current command path.
[string]$current = $MyInvocation.MyCommand.Path
# Get calling command path.
[string]$calling = @(Get-PSCallStack)[1].InvocationInfo.MyCommand.Path
# If executed directly then...
if ($calling -ne "") {
    $current = $calling
}

$file = Get-Item $current
# Working folder.
$wdir = $file.Directory.FullName;

# ----------------------------------------------------------------------------

$global:removeCount = 0;
$global:skipCount = 0;

Function RemoveDirectories
{
	# Parameters.
	Param ($pattern)
	# Function.
	$items = Get-ChildItem $wdir -Filter $pattern -Recurse -Force | Where-Object {$_ -is [System.IO.DirectoryInfo]};
	foreach ($item in $items)
	{
		[System.IO.DirectoryInfo] $parent = $item.Parent;
		$projects = $parent.GetFiles("*.*proj", [System.IO.SearchOption]::TopDirectoryOnly);
		# If Project file in parent folder was found then...
		if ($projects.length -gt 0){
			Write-Output "Remove: $($item.FullName)";
			$global:removeCount += 1;
			Remove-Item -LiteralPath $item.FullName -Force -Recurse
		}
		else
		{
			Write-Output "Skip:   $($item.FullName)";
			$global:skipCount += 1;
		}
	}
}
# Clear directories.
RemoveDirectories "bin"
RemoveDirectories "obj"
Write-Output "Skipped: $global:skipCount, Removed: $global:removeCount";
pause