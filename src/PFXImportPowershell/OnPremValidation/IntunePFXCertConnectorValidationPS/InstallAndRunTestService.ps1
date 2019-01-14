param(
	[string] $serviceDirectory = "..\IntunePFXCertConnectorValidationService\bin\x64\Debug\",
	[string] $testResultsFileName = "PFXImportTestResultsMarker.txt",
	[string] $serviceName = "PFXImportDecryptTest",
	[string] $serviceDescription = "Tests if the system account can decrpyt with a key in a service"
	
)

$serviceUserName = "NT AUTHORITY\SYSTEM"
$serviceUserPassword = "EmptyString" | ConvertTo-SecureString -AsPlainText -Force
$credentials = New-Object System.Management.Automation.PSCredential ($serviceUserName, $serviceUserPassword)



$binaryPath = "$($serviceDirectory)\Microsoft.Management.PFXImport.IIntunePFXCertConnectorTestService.exe"

# Creating Service
Write-Host "Installing service: $serviceName"
New-Service -name $serviceName -binaryPathName $binaryPath -Description $serviceDescription -displayName $serviceName -startupType "Manual" -credential $credentials

Write-Host "Installation completed: $serviceName"

# Start new service
Write-Host "Starting service: $serviceName"
$service = Get-WmiObject -Class Win32_Service -Filter "name='$serviceName'"
$service.startservice()
Write-Host "Starting Service: $serviceName"

#Wait for service to run
Start-Sleep -s 10
while (!(Test-Path "$($serviceDirectory)\$($testResultsFileName)")) 
{ 
    Write-Host "Waiting for the service to start and run...It can take a few minutes"
    Start-Sleep 10 
}

#Output the results
$result = Get-Content -Path "$($serviceDirectory)\$($testResultsFileName)"
Write-Host $result -ForegroundColor Yellow