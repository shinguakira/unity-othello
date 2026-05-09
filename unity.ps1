param(
    [string]$Action = "open"
)

$Unity   = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"
$Project = $PSScriptRoot
$Log     = "E:\tmp\unity-build.log"
$TestLog     = "E:\tmp\unity-test.log"
$TestResults = "E:\tmp\unity-test-results.xml"

switch ($Action) {
    "compile" {
        Write-Host "Compiling Unity project..."
        & $Unity -projectPath $Project -batchmode -quit -logFile $Log

        $errors = Select-String -Path $Log -Pattern "error CS\d+" -ErrorAction SilentlyContinue
        if ($errors) {
            Write-Host "COMPILE ERRORS:"
            $errors | ForEach-Object { Write-Host $_.Line }
            exit 1
        }
        else {
            Write-Host "OK - No errors"
        }
    }

    "test" {
        Write-Host "Running EditMode tests..."
        & $Unity -batchmode -runTests -testPlatform EditMode `
            -projectPath $Project -testResults $TestResults -logFile $TestLog

        if (-not (Test-Path $TestResults)) {
            Write-Host "ERROR: test results not found. Check $TestLog"
            exit 1
        }

        [xml]$xml = Get-Content $TestResults
        $total  = $xml.'test-run'.total
        $passed = $xml.'test-run'.passed
        $failed = $xml.'test-run'.failed
        Write-Host "Total: $total  Passed: $passed  Failed: $failed"

        if ([int]$failed -gt 0) {
            Select-String -Path $TestLog -Pattern "FAILED" -ErrorAction SilentlyContinue |
                ForEach-Object { Write-Host $_.Line }
            exit 1
        }
    }

    "playmode" {
        Write-Host "Running PlayMode tests..."
        $PlayLog     = "E:\tmp\unity-test-playmode.log"
        $PlayResults = "E:\tmp\unity-test-playmode-results.xml"
        & $Unity -batchmode -runTests -testPlatform PlayMode `
            -projectPath $Project -testResults $PlayResults -logFile $PlayLog

        if (-not (Test-Path $PlayResults)) {
            Write-Host "ERROR: test results not found. Check $PlayLog"
            exit 1
        }

        [xml]$xml = Get-Content $PlayResults
        $total  = $xml.'test-run'.total
        $passed = $xml.'test-run'.passed
        $failed = $xml.'test-run'.failed
        Write-Host "Total: $total  Passed: $passed  Failed: $failed"

        if ([int]$failed -gt 0) {
            Select-String -Path $PlayLog -Pattern "FAILED|error CS" -ErrorAction SilentlyContinue |
                ForEach-Object { Write-Host $_.Line }
            exit 1
        }
    }

    default {
        Write-Host "Opening Unity Editor..."
        Start-Process -FilePath $Unity -ArgumentList "-projectPath `"$Project`""
    }
}
