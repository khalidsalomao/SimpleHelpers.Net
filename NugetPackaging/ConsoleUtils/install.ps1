param($installPath, $toolsPath, $package, $project)

## Set property: CopyToOutputDirectory
$item = $project.ProjectItems.Item("help")
if ($item -ne $null) {
    $property = $item.Properties.Item("CopyToOutputDirectory")
	if ($property -ne $null) {
		$property.Value = 1    
	}
}

$item = $project.ProjectItems.Item("Configuration.md")
if ($item -ne $null) {
    $property = $item.Properties.Item("CopyToOutputDirectory")
	if ($property -ne $null) {
		$property.Value = 1    
	}
}
