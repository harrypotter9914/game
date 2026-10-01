$gamePath = Join-Path $PSScriptRoot 'Builds\bable\bable.exe'
if (-not (Test-Path -LiteralPath $gamePath)) { throw 'Build the Windows player with Bable/Build Windows Player in Unity first.' }
Start-Process -FilePath $gamePath -WorkingDirectory (Split-Path -Parent $gamePath)
