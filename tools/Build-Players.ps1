param([string]$UnityEditor = $env:UNITY_EDITOR)
$ErrorActionPreference = 'Stop'
if (-not $UnityEditor) { $UnityEditor = 'E:\unityeditor\6000.5.9f1\Editor\Unity.exe' }
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw 'Pass -UnityEditor with the Unity 6000.5.9f1 Editor executable path.' }
$projectRoot = Split-Path -Parent $PSScriptRoot
$logFolder = Join-Path $projectRoot 'reference\builds'
New-Item -ItemType Directory -Force $logFolder | Out-Null
$buildArgs = @('-batchmode','-quit','-projectPath',('"'+(Join-Path $projectRoot 'Unity')+'"'),'-executeMethod','BablePlayerBuilds.Both','-logFile',('"'+(Join-Path $logFolder 'build.log')+'"'))
$process = Start-Process -FilePath $UnityEditor -ArgumentList $buildArgs -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw ('Build failed. Read '+(Join-Path $logFolder 'build.log')) }
Write-Host 'Built campaign and practice from the same Unity project.'
