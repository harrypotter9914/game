$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path $projectRoot ('reference\smoke-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force $testRoot | Out-Null
foreach ($flavor in @('bable','bable-practice')) {
    $exe = Join-Path $projectRoot ('Builds\'+$flavor+'\'+$flavor+'.exe')
    $report = Join-Path $testRoot ($flavor+'.json')
    $smokeArgs = @('-batchmode','-nographics','-bableSmokeReport',('"'+$report+'"'),'-bableSaveRoot',('"'+(Join-Path $testRoot ($flavor+'-save'))+'"'),'-logFile',('"'+(Join-Path $testRoot ($flavor+'.log'))+'"'))
    $process = Start-Process -FilePath $exe -ArgumentList $smokeArgs -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(240000)) { $process.Kill(); throw "$flavor smoke test timed out." }
    if (-not (Test-Path -LiteralPath $report)) { throw "$flavor produced no test report." }
    $result = Get-Content -Raw -LiteralPath $report | ConvertFrom-Json
    if ($process.ExitCode -ne 0 -or -not $result.passed) { throw "$flavor smoke failed. See $report" }
    Write-Host "$flavor passed: $report"
}
