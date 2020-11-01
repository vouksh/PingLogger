$parentDir = Split-Path -Path (Get-Location) -Parent
$Assembly = [Reflection.Assembly]::LoadFile("$parentDir\bin\Release\netcoreapp3.1\win-x64\PingLogger.dll")
$version = $Assembly.GetName().Version;
$versionPath = "v$($version.Major)$($version.Minor)$($version.Build)" 
$acctKey = Get-item -Path "./azureKey"
$Context = New-AzStorageContext -ConnectionString "DefaultEndpointsProtocol=https;AccountName=pingloggerfiles;AccountKey=$acctKey;EndpointSuffix=core.windows.net"
try {
    #New-AzStorageDirectory -Path $versionPath -ShareName "versions" -Context $Context -ErrorAction SilentlyContinue
    New-AzStorageContainer -Context $Context -Name $versionPath -Permission Container
} catch {
    Write-Host "Directory already exists"
}
$version | ConvertTo-Json | Out-File -FilePath "./latest.json"
#Set-AzStorageFileContent -Context $Context -ShareName "versions" -Source "./latest" -Path "latest" -Force
#Set-AzStorageFileContent -Context $Context -ShareName "versions" -Source "./Release/Installer.msi" -Path "$versionPath/PingLogger-Setup.msi" -Force
#Set-AzStorageFileContent -Context $Context -ShareName "versions" -Source "./Release/PingLogger.exe" -Path "$versionPath/PingLogger.exe" -Force

Set-AzStorageBlobContent -Context $Context -Container $versionPath -File './Release/PingLogger-Setup.msi' -Blob "PingLogger-Setup.msi"
Set-AzStorageBlobContent -Context $Context -Container $versionPath -File './Release/PingLogger.exe' -Blob "PingLogger.exe"
Set-AzStorageBlobContent -Context $Context -Container "version" -File './latest.json' -Blob "latest.json" -Force