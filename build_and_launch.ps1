# Define the relative path to the KSP executable
$kspExecutablePath = "..\..\KSP_x64.exe"  # Update this path if necessary

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
	Write-Host "Build succeeded. Launching KSP..."
	
	# Launch the KSP executable
	Start-Process -FilePath $kspExecutablePath
} else {
	Write-Host "Build failed. Please check the error messages above."
}

# Navigate back to the original directory
Set-Location -Path $pwd