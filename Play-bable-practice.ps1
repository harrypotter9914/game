$gamePath = Join-Path $PSScriptRoot 'Builds\bable-practice\bable-practice.exe'
if (-not (Test-Path -LiteralPath $gamePath)) { throw 'Build both players with Bable/Build/Build Both Windows Players in Unity first.' }
Start-Process -FilePath $gamePath -WorkingDirectory (Split-Path -Parent $gamePath)
