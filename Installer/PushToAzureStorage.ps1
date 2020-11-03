$parentDir = Split-Path -Path (Get-Location) -Parent
$Assembly = [Reflection.Assembly]::LoadFile("$parentDir\bin\Release\netcoreapp3.1\win-x64\PingLogger.dll")
$version = $Assembly.GetName().Version;
$versionPath = "v$($version.Major)$($version.Minor)$($version.Build)" 
$acctKey = Get-Content -Path "./azureKey"
$Context = New-AzStorageContext -ConnectionString "DefaultEndpointsProtocol=https;AccountName=pingloggerfiles;AccountKey=$acctKey;EndpointSuffix=core.windows.net"
try {
    New-AzStorageContainer -Context $Context -Name $versionPath -Permission Container -ErrorAction SilentlyContinue
} catch {
    Write-Host "Container already exists"
}
$version | ConvertTo-Json | Out-File -FilePath "./latest.json"
Set-AzStorageBlobContent -Context $Context -Container $versionPath -File './Release/PingLogger-Setup.msi' -Blob "PingLogger-Setup.msi" -Force
Set-AzStorageBlobContent -Context $Context -Container $versionPath -File './Release/PingLogger.exe' -Blob "PingLogger.exe" -Force
Set-AzStorageBlobContent -Context $Context -Container "version" -File './latest.json' -Blob "latest.json" -Force