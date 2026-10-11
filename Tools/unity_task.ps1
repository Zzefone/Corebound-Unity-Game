param(
    [ValidateSet('Generate', 'Brand', 'Build', 'Verify', 'Complete', 'Check', 'Release')]
    [string]$Task = 'Build'
)
$ErrorActionPreference = 'Stop'
if ($Task -in @('Complete', 'Check', 'Release')) {
    if ($Task -eq 'Complete') { & $PSCommandPath -Task Generate }
    if ($Task -eq 'Release') { & $PSCommandPath -Task Brand }
    & $PSCommandPath -Task Build
    & $PSCommandPath -Task Verify
    exit
}
$projectPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$editorPath = 'D:\unity\6000.0.23f1\Editor\Unity.exe'
$verificationPath = Join-Path $projectPath 'Verification'
New-Item -ItemType Directory -Path $verificationPath -Force | Out-Null
if ($Task -eq 'Verify') {
    $playerPath = Join-Path $projectPath 'Builds\CoreboundWindows\Corebound.exe'
    if (-not (Test-Path -LiteralPath $playerPath)) { throw 'Build the Windows player first.' }
    $playerArgs = @('-coreRunnerVerify', $verificationPath, '-screen-width', '1600', '-screen-height', '900', '-windowed', '-logFile', (Join-Path $verificationPath 'player.log'))
    $process = Start-Process -FilePath $playerPath -WindowStyle Hidden -ArgumentList $playerArgs -PassThru
    if (-not $process.WaitForExit(90000)) {
        Stop-Process -Id $process.Id
        throw 'Verification player exceeded 90 seconds; see player.log.'
    }
    if ($process.ExitCode -ne 0) { throw "Verification exited with code $($process.ExitCode)." }
    $result = Get-Content -LiteralPath (Join-Path $verificationPath 'results.json') -Raw | ConvertFrom-Json
    $result.checks | Format-Table name,passed,detail -AutoSize
    if (-not $result.passed) { throw 'One or more player checks failed.' }
}
else {
    $method = switch ($Task) {
        'Generate' { 'ProjectBuilder.Generate' }
        'Brand' { 'ProjectBuilder.ApplyBrandName' }
        default { 'ProjectBuilder.BuildWindows' }
    }
    $logPath = Join-Path $verificationPath ($Task.ToLowerInvariant() + '.log')
    $editorArgs = @('-batchmode', '-nographics', '-projectPath', $projectPath, '-executeMethod', $method, '-quit', '-logFile', $logPath)
    $process = Start-Process -FilePath $editorPath -WindowStyle Hidden -ArgumentList $editorArgs -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Unity exited with code $($process.ExitCode). See $logPath" }
    Write-Output "$Task completed. Log: $logPath"
}
