$distPath = "dist/GameData/MechJebHopper/"
$distDllPath = "$distPath/Plugins/"
$dllPath = "Plugins\net472\MechJebHopper.dll"

# Get the current working directory
$projectPath = Get-Location

# Navigate to the project directory
Write-Host "Navigating to project directory: $projectPath"
Set-Location -Path $projectPath

# Run the dotnet build command and capture the output
Write-Host "Building the project..."
$buildOutput = dotnet build 2>&1

# Display the build output
Write-Host "Build Output:"
Write-Host $buildOutput

# Check if the build was successful
if ($buildOutput -match "Build succeeded.") {
	Write-Host "Build succeeded. Packaging the project..."

	# delete dist/ folder if exists
	if (Test-Path -Path "dist") {
		Remove-Item -Path "dist" -Recurse -Force
	}

	# create dist/ folder
	New-Item -ItemType Directory -Path $distDllPath -Force

	# copy the built .dll file to the dist/ folder
	Copy-Item -Path $dllPath -Destination $distDllPath

	# Copy license
	Copy-Item -Path "LICENSE" -Destination $distPath
} else {
	Write-Host "Build failed. Please check the error messages above."
}

# Navigate back to the original directory
Set-Location -Path $pwd