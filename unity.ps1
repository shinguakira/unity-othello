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

    "playmode-themes" {
        # Run PlayMode tests once per theme variant; copy screenshots into a
        # per-theme folder under Tests/Design-Themes/screenshots/<Theme>/.
        $Themes = @("Riso", "Wabi", "Neon", "Pieces")
        $ScreenshotSrc = Join-Path $env:USERPROFILE "AppData\LocalLow\Indie\Othell\TestArtifacts"
        $OutRoot       = Join-Path $Project "Tests\Design-Themes\screenshots"

        New-Item -ItemType Directory -Force -Path $OutRoot | Out-Null

        foreach ($Theme in $Themes) {
            Write-Host ""
            Write-Host "============================================="
            Write-Host "  Theme: $Theme"
            Write-Host "============================================="

            # Make sure no Unity is holding the project before launching.
            Get-Process Unity -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 3

            # Wipe stale screenshots and prior XML so we know the artifacts
            # in TestArtifacts/ came from THIS run.
            if (Test-Path $ScreenshotSrc) {
                Get-ChildItem -Path $ScreenshotSrc -Filter *.png -ErrorAction SilentlyContinue |
                    Remove-Item -Force -ErrorAction SilentlyContinue
            }

            $PlayLog     = "E:\tmp\unity-test-playmode-$Theme.log"
            $PlayResults = "E:\tmp\unity-test-playmode-$Theme.xml"
            if (Test-Path $PlayResults) { Remove-Item $PlayResults -Force }

            # Use Start-Process -Wait so PowerShell really blocks until Unity
            # has fully exited and flushed the result XML.
            $proc = Start-Process -FilePath $Unity -PassThru -Wait -ArgumentList @(
                "-batchmode", "-runTests",
                "-testPlatform", "PlayMode",
                "-projectPath", $Project,
                "-testResults", $PlayResults,
                "-logFile", $PlayLog,
                "-theme=$Theme"
            )

            # Extra safety: small delay so any deferred file flush completes.
            Start-Sleep -Seconds 2

            $OutDir = Join-Path $OutRoot $Theme
            New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
            Get-ChildItem -Path $OutDir -Filter *.png -ErrorAction SilentlyContinue |
                Remove-Item -Force -ErrorAction SilentlyContinue

            if (Test-Path $ScreenshotSrc) {
                $shots = Get-ChildItem -Path $ScreenshotSrc -Filter *.png -ErrorAction SilentlyContinue
                foreach ($f in $shots) {
                    Copy-Item -Path $f.FullName -Destination $OutDir -Force
                }
                Write-Host "  Screenshots: $($shots.Count) -> $OutDir"
            } else {
                Write-Host "  WARN: no screenshot source dir"
            }

            if (Test-Path $PlayResults) {
                [xml]$xml = Get-Content $PlayResults
                Write-Host "  Tests: $($xml.'test-run'.passed)/$($xml.'test-run'.total) passed"
            } else {
                Write-Host "  ERROR: no XML for $Theme (exit $($proc.ExitCode); see $PlayLog)"
            }
        }

        Write-Host ""
        Write-Host "Done. Compare at: $OutRoot"
    }

    default {
        Write-Host "Opening Unity Editor..."
        Start-Process -FilePath $Unity -ArgumentList "-projectPath `"$Project`""
    }
}
