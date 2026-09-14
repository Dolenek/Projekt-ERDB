param(
    [Parameter(Mandatory = $true)]
    [int]$RootProcessId,
    [Parameter(Mandatory = $true)]
    [ValidateSet(1, 3, 5)]
    [int]$AccountCount,
    [ValidateRange(1, 300)]
    [int]$Samples = 30,
    [ValidateRange(100, 60000)]
    [int]$IntervalMilliseconds = 1000,
    [string]$OutputPath
)

function Get-DescendantProcessIds {
    param([int]$ParentProcessId)

    $allProcesses = Get-CimInstance Win32_Process |
        Select-Object ProcessId, ParentProcessId, Name
    $selectedIds = [System.Collections.Generic.HashSet[int]]::new()
    [void]$selectedIds.Add($ParentProcessId)

    do {
        $previousCount = $selectedIds.Count
        foreach ($process in $allProcesses) {
            if ($selectedIds.Contains([int]$process.ParentProcessId)) {
                [void]$selectedIds.Add([int]$process.ProcessId)
            }
        }
    } while ($selectedIds.Count -gt $previousCount)

    return $selectedIds
}

if (-not (Get-Process -Id $RootProcessId -ErrorAction SilentlyContinue)) {
    throw "Process $RootProcessId is not running."
}

$measurements = for ($sample = 1; $sample -le $Samples; $sample++) {
    $processIds = Get-DescendantProcessIds -ParentProcessId $RootProcessId
    $processes = Get-Process -Id ([int[]]$processIds) -ErrorAction SilentlyContinue
    $webViewProcesses = @($processes | Where-Object ProcessName -eq 'msedgewebview2')
    $workingSetMb = ($processes | Measure-Object WorkingSet64 -Sum).Sum / 1MB
    $webViewWorkingSetMb = ($webViewProcesses | Measure-Object WorkingSet64 -Sum).Sum / 1MB

    [pscustomobject]@{
        Timestamp = Get-Date -Format 'o'
        AccountCount = $AccountCount
        TotalWorkingSetMB = [math]::Round($workingSetMb, 1)
        WebViewWorkingSetMB = [math]::Round($webViewWorkingSetMb, 1)
        RelatedProcessCount = @($processes).Count
        WebViewProcessCount = $webViewProcesses.Count
    }

    if ($sample -lt $Samples) {
        Start-Sleep -Milliseconds $IntervalMilliseconds
    }
}

$summary = [pscustomobject]@{
    AccountCount = $AccountCount
    Samples = $Samples
    AverageTotalWorkingSetMB = [math]::Round(($measurements.TotalWorkingSetMB | Measure-Object -Average).Average, 1)
    PeakTotalWorkingSetMB = [math]::Round(($measurements.TotalWorkingSetMB | Measure-Object -Maximum).Maximum, 1)
    AverageWebViewWorkingSetMB = [math]::Round(($measurements.WebViewWorkingSetMB | Measure-Object -Average).Average, 1)
    PeakWebViewWorkingSetMB = [math]::Round(($measurements.WebViewWorkingSetMB | Measure-Object -Maximum).Maximum, 1)
}

if ($OutputPath) {
    $measurements | Export-Csv -LiteralPath $OutputPath -NoTypeInformation -Encoding UTF8
}

$summary
