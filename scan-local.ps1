param(
    [ValidateSet("prod", "dev")]
    [string]$Environment = "prod",
    [string]$FrontendTargetUrl,
    [string]$ApiTargetUrl,
    [switch]$SkipGitleaks,
    [switch]$SkipDotnetVuln,
    [switch]$SkipNpmAudit,
    [switch]$SkipSemgrep,
    [switch]$ScanGitHistory,
    [switch]$SkipPentestRegression,
    [switch]$SkipDynamicPentest,
    [switch]$RunZapBaseline,
    [string]$ZapTargetUrl,
    [int]$RateLimitProbeCount = 8
)

$ErrorActionPreference = "Continue"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$backendPath = Join-Path $root "SolvoRefAppBackend-dev"
$frontendPath = Join-Path $root "SolvoRefAppFrontend"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportDir = Join-Path $root ("reports\" + $timestamp)
$gitleaksConfig = Join-Path $root ".gitleaks.toml"
$dotnetHome = Join-Path $root ".dotnet"
$nugetPackages = Join-Path $root ".nuget\packages"
$npmCache = Join-Path $root ".npm-cache"

$prodFrontendUrl = "https://solvoreferalapp.solvoglobal.com"
$prodApiUrl = "https://ref-api-prod-akhebmazgjhfbxc0.eastus2-01.azurewebsites.net"
$devFrontendUrl = "https://pruebasolvoreferalapp.solvoglobal.com"
$devApiUrl = "https://sol-ref-api-dtb6dpftdsema2gt.eastus2-01.azurewebsites.net"

if ([string]::IsNullOrWhiteSpace($FrontendTargetUrl)) {
    $FrontendTargetUrl = if ($Environment -eq "prod") { $prodFrontendUrl } else { $devFrontendUrl }
}
if ([string]::IsNullOrWhiteSpace($ApiTargetUrl)) {
    $ApiTargetUrl = if ($Environment -eq "prod") { $prodApiUrl } else { $devApiUrl }
}
if ([string]::IsNullOrWhiteSpace($ZapTargetUrl)) {
    $ZapTargetUrl = $FrontendTargetUrl
}

$FrontendTargetUrl = $FrontendTargetUrl.TrimEnd("/")
$ApiTargetUrl = $ApiTargetUrl.TrimEnd("/")
$ZapTargetUrl = $ZapTargetUrl.TrimEnd("/")

New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
New-Item -ItemType Directory -Path $dotnetHome -Force | Out-Null
New-Item -ItemType Directory -Path $nugetPackages -Force | Out-Null
New-Item -ItemType Directory -Path $npmCache -Force | Out-Null

$env:DOTNET_CLI_HOME = $dotnetHome
$env:NUGET_PACKAGES = $nugetPackages
$env:NPM_CONFIG_CACHE = $npmCache

$summary = [System.Collections.Generic.List[string]]::new()
$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Test-CommandAvailable {
    param([string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Get-SemgrepRunner {
    $runners = @()

    foreach ($binName in @("semgrep", "pysemgrep")) {
        $cmd = Get-Command $binName -ErrorAction SilentlyContinue
        if ($null -ne $cmd) {
            $runners += @{ Kind = "exe"; Value = $cmd.Source }
        }
    }

    $candidateDirs = Get-ChildItem -Path (Join-Path $env:APPDATA "Python") -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "Python*" }

    foreach ($dir in $candidateDirs) {
        foreach ($binName in @("semgrep.exe", "pysemgrep.exe")) {
            $candidate = Join-Path $dir.FullName ("Scripts\" + $binName)
            if (Test-Path $candidate) {
                $runners += @{ Kind = "exe"; Value = $candidate }
            }
        }
    }

    foreach ($runner in $runners) {
        try {
            & $runner.Value --version | Out-Null

            if ($LASTEXITCODE -eq 0) {
                return $runner
            }
        } catch {
            continue
        }
    }

    return $null
}

function Get-ZapRunner {
    foreach ($binName in @("zap.bat", "zap.exe", "zaproxy")) {
        $cmd = Get-Command $binName -ErrorAction SilentlyContinue
        if ($null -ne $cmd) {
            return $cmd.Source
        }
    }

    foreach ($candidate in @(
        "C:\Program Files\OWASP\Zed Attack Proxy\zap.bat",
        "C:\Program Files\ZAP\Zed Attack Proxy\zap.bat"
    )) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    return $null
}

function Run-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan
    try {
        & $Action
        if ($LASTEXITCODE -ne $null -and $LASTEXITCODE -ne 0) {
            $errors.Add("$Name failed with exit code $LASTEXITCODE")
            Write-Host "FAILED: $Name (exit $LASTEXITCODE)" -ForegroundColor Red
        } else {
            $summary.Add($Name)
            Write-Host "OK: $Name" -ForegroundColor Green
        }
    } catch {
        $errors.Add("$Name failed: $($_.Exception.Message)")
        Write-Host "FAILED: $Name" -ForegroundColor Red
    }
}

function Assert-PatternPresent {
    param(
        [string]$CheckId,
        [string]$Description,
        [string]$Path,
        [string]$Pattern
    )

    $match = Select-String -Path $Path -Pattern $Pattern -SimpleMatch -ErrorAction SilentlyContinue
    if ($null -eq $match) {
        $errors.Add("$CheckId FAILED: $Description")
    } else {
        $summary.Add("$CheckId OK: $Description")
    }
}

function Assert-PatternAbsent {
    param(
        [string]$CheckId,
        [string]$Description,
        [string]$Path,
        [string]$Pattern
    )

    $match = Select-String -Path $Path -Pattern $Pattern -SimpleMatch -ErrorAction SilentlyContinue
    if ($null -ne $match) {
        $errors.Add("$CheckId FAILED: $Description")
    } else {
        $summary.Add("$CheckId OK: $Description")
    }
}

function Get-HeaderValue {
    param(
        [object]$Headers,
        [string]$Name
    )

    if ($null -eq $Headers) {
        return ""
    }

    foreach ($key in $Headers.Keys) {
        $keyText = [string]$key
        if ($keyText -ieq $Name) {
            $value = $Headers[$keyText]
            if ($value -is [array]) {
                return ($value -join ", ")
            }
            return [string]$value
        }
    }

    return ""
}

function Invoke-PentestRequest {
    param(
        [string]$Method = "GET",
        [string]$Url,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [string]$ContentType = "application/json"
    )

    $parameters = @{
        Uri = $Url
        Method = $Method
        Headers = $Headers
        TimeoutSec = 30
        MaximumRedirection = 0
        UseBasicParsing = $true
    }

    if ($null -ne $Body) {
        $parameters.Body = $Body
        $parameters.ContentType = $ContentType
    }

    try {
        $response = Invoke-WebRequest @parameters
        return [pscustomobject]@{
            Url = $Url
            Method = $Method
            StatusCode = [int]$response.StatusCode
            Headers = $response.Headers
            Body = [string]$response.Content
            Error = $null
        }
    } catch {
        $statusCode = 0
        $headers = @{}
        $body = ""

        if ($_.Exception.Response) {
            try {
                $statusCode = [int]$_.Exception.Response.StatusCode
                $headers = @{}
                foreach ($headerName in $_.Exception.Response.Headers.AllKeys) {
                    $headers[$headerName] = ($_.Exception.Response.Headers.GetValues($headerName) -join ", ")
                }
                $stream = $_.Exception.Response.GetResponseStream()
                if ($null -ne $stream) {
                    $reader = New-Object System.IO.StreamReader($stream)
                    $body = $reader.ReadToEnd()
                    $reader.Dispose()
                }
            } catch {
                # Keep original request error below.
            }
        }

        return [pscustomobject]@{
            Url = $Url
            Method = $Method
            StatusCode = $statusCode
            Headers = $headers
            Body = $body
            Error = $_.Exception.Message
        }
    }
}

function Add-DynamicFinding {
    param(
        [System.Collections.Generic.List[object]]$Findings,
        [string]$Id,
        [string]$Title,
        [string]$Severity,
        [bool]$Passed,
        [string]$Evidence,
        [string]$Target
    )

    $Findings.Add([pscustomobject]@{
        id = $Id
        title = $Title
        severity = $Severity
        status = if ($Passed) { "PASS" } else { "FAIL" }
        target = $Target
        evidence = $Evidence
    })
}

function Test-HeaderEquals {
    param([object]$Headers, [string]$Name, [string]$Expected)
    return (Get-HeaderValue -Headers $Headers -Name $Name).Equals($Expected, [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-HeaderContains {
    param([object]$Headers, [string]$Name, [string[]]$ExpectedParts)
    $value = Get-HeaderValue -Headers $Headers -Name $Name
    foreach ($part in $ExpectedParts) {
        if ($value.IndexOf($part, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            return $false
        }
    }
    return $true
}

function Write-DynamicPentestHtml {
    param(
        [string]$Path,
        [object]$Report
    )

    $rows = foreach ($finding in $Report.findings) {
        $statusClass = if ($finding.status -eq "PASS") { "pass" } else { "fail" }
        "<tr class='$statusClass'><td>$($finding.id)</td><td>$($finding.severity)</td><td>$($finding.status)</td><td>$([System.Net.WebUtility]::HtmlEncode($finding.title))</td><td>$([System.Net.WebUtility]::HtmlEncode($finding.target))</td><td><pre>$([System.Net.WebUtility]::HtmlEncode($finding.evidence))</pre></td></tr>"
    }

    $html = @"
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>SolvoRefApp Dynamic Pentest Regression - $($Report.environment.ToUpperInvariant())</title>
  <style>
    body { font-family: Segoe UI, Arial, sans-serif; margin: 32px; color: #1f2937; }
    h1 { margin-bottom: 4px; }
    .meta { color: #4b5563; margin-bottom: 24px; }
    table { border-collapse: collapse; width: 100%; table-layout: fixed; }
    th, td { border: 1px solid #d1d5db; padding: 8px; vertical-align: top; word-break: break-word; }
    th { background: #f3f4f6; text-align: left; }
    tr.pass td:first-child { border-left: 6px solid #027a48; }
    tr.fail td:first-child { border-left: 6px solid #b42318; }
    pre { white-space: pre-wrap; margin: 0; font-family: Consolas, monospace; font-size: 12px; }
  </style>
</head>
<body>
  <h1>SolvoRefApp Dynamic Pentest Regression</h1>
  <div class="meta">
    Environment: <strong>$($Report.environment)</strong><br />
    Frontend: $([System.Net.WebUtility]::HtmlEncode($Report.frontendUrl))<br />
    API: $([System.Net.WebUtility]::HtmlEncode($Report.apiUrl))<br />
    Generated: $($Report.generatedAt)
  </div>
  <table>
    <thead><tr><th>ID</th><th>Severity</th><th>Status</th><th>Check</th><th>Target</th><th>Evidence</th></tr></thead>
    <tbody>
      $($rows -join "`n")
    </tbody>
  </table>
</body>
</html>
"@

    Set-Content -Path $Path -Value $html -Encoding UTF8
}

function Run-DynamicPentestRegression {
    param(
        [string]$EnvironmentName,
        [string]$FrontendUrl,
        [string]$ApiUrl,
        [string]$OutputDirectory,
        [int]$BurstCount
    )

    $findings = [System.Collections.Generic.List[object]]::new()

    $frontendResponse = Invoke-PentestRequest -Url $FrontendUrl
    Add-DynamicFinding $findings "DF-FE-00" "Frontend production target is reachable" "Info" ($frontendResponse.StatusCode -lt 400 -and $frontendResponse.StatusCode -gt 0) "HTTP $($frontendResponse.StatusCode). Error: $($frontendResponse.Error)" $FrontendUrl
    Add-DynamicFinding $findings "F-23" "Frontend Permissions-Policy is present" "Low" (Test-HeaderContains $frontendResponse.Headers "Permissions-Policy" @("camera=()", "geolocation=()", "microphone=()")) (Get-HeaderValue $frontendResponse.Headers "Permissions-Policy") $FrontendUrl
    Add-DynamicFinding $findings "DF-04/F-13" "Frontend security headers are present" "Medium" (
        (Test-HeaderEquals $frontendResponse.Headers "X-Content-Type-Options" "nosniff") -and
        (Test-HeaderEquals $frontendResponse.Headers "X-Frame-Options" "DENY") -and
        (Test-HeaderContains $frontendResponse.Headers "Content-Security-Policy" @("object-src 'none'", "frame-ancestors 'none'", "base-uri 'self'"))
    ) "XCTO=$(Get-HeaderValue $frontendResponse.Headers "X-Content-Type-Options"); XFO=$(Get-HeaderValue $frontendResponse.Headers "X-Frame-Options"); CSP=$(Get-HeaderValue $frontendResponse.Headers "Content-Security-Policy")" $FrontendUrl
    Add-DynamicFinding $findings "F-03" "Initial frontend HTML does not expose bearer token in URL/script" "Critical" (-not ($frontendResponse.Body -match "bearer\s+[a-z0-9._-]+|auth_token=|access_token=")) "Initial HTML inspected." $FrontendUrl

    $apiMe = Invoke-PentestRequest -Url "$ApiUrl/api/auth/me" -Headers @{ "X-CSRF-Token" = "1" }
    Add-DynamicFinding $findings "DF-AUTH-01" "Unauthenticated profile endpoint is rejected" "High" (@(401, 403) -contains $apiMe.StatusCode) "HTTP $($apiMe.StatusCode). Error: $($apiMe.Error)" "$ApiUrl/api/auth/me"
    Add-DynamicFinding $findings "DF-04/F-13" "API security headers are present" "Medium" (
        (Test-HeaderEquals $apiMe.Headers "X-Content-Type-Options" "nosniff") -and
        (Test-HeaderContains $apiMe.Headers "X-Frame-Options" @("DENY")) -and
        (Test-HeaderContains $apiMe.Headers "Content-Security-Policy" @("object-src 'none'", "frame-ancestors 'none'", "base-uri 'self'"))
    ) "XCTO=$(Get-HeaderValue $apiMe.Headers "X-Content-Type-Options"); XFO=$(Get-HeaderValue $apiMe.Headers "X-Frame-Options"); CSP=$(Get-HeaderValue $apiMe.Headers "Content-Security-Policy")" "$ApiUrl/api/auth/me"

    $logout = Invoke-PentestRequest -Method "POST" -Url "$ApiUrl/api/auth/logout" -Headers @{ "X-CSRF-Token" = "1" }
    Add-DynamicFinding $findings "DF-02/DF-01" "Logout endpoint is not anonymously usable" "Low" (@(401, 403) -contains $logout.StatusCode) "HTTP $($logout.StatusCode). Error: $($logout.Error)" "$ApiUrl/api/auth/logout"

    $swagger = Invoke-PentestRequest -Url "$ApiUrl/swagger/index.html"
    Add-DynamicFinding $findings "F-08" "Swagger UI is not publicly accessible in production" "Medium" (@(401, 403, 404) -contains $swagger.StatusCode) "HTTP $($swagger.StatusCode). Error: $($swagger.Error)" "$ApiUrl/swagger/index.html"

    $fabricPayload = @{ phone = "0000000000"; email = "pentest.invalid@example.invalid"; referralId = "PENTEST-INVALID" } | ConvertTo-Json -Compress
    $fabric = Invoke-PentestRequest -Method "POST" -Url "$ApiUrl/api/fabric/validate/referred" -Headers @{ "X-CSRF-Token" = "1" } -Body $fabricPayload
    Add-DynamicFinding $findings "DF-09" "Fabric validation rejects invalid referral identity" "High" ($fabric.StatusCode -ge 400 -or $fabric.Body -match '"validation"\s*:\s*false') "HTTP $($fabric.StatusCode). Body: $($fabric.Body.Substring(0, [Math]::Min(500, $fabric.Body.Length)))" "$ApiUrl/api/fabric/validate/referred"

    $upload = Invoke-PentestRequest -Method "POST" -Url "$ApiUrl/api/content/admin/upload-image" -Headers @{ "X-CSRF-Token" = "1" } -Body "not-an-image" -ContentType "text/plain"
    Add-DynamicFinding $findings "DF-13" "Admin upload endpoint is protected from anonymous upload" "High" (@(401, 403, 415, 400) -contains $upload.StatusCode) "HTTP $($upload.StatusCode). Error: $($upload.Error)" "$ApiUrl/api/content/admin/upload-image"

    $rateStatuses = [System.Collections.Generic.List[int]]::new()
    for ($i = 0; $i -lt $BurstCount; $i++) {
        $rateResponse = Invoke-PentestRequest -Url "$ApiUrl/api/auth/me" -Headers @{ "X-CSRF-Token" = "1" }
        $rateStatuses.Add($rateResponse.StatusCode)
        Start-Sleep -Milliseconds 100
    }
    Add-DynamicFinding $findings "DF-07/F-11" "Low-volume unauthenticated burst does not destabilize API" "High" (-not ($rateStatuses -contains 500 -or $rateStatuses -contains 502 -or $rateStatuses -contains 503 -or $rateStatuses -contains 504)) "Statuses: $($rateStatuses -join ', ')" "$ApiUrl/api/auth/me"

    $report = [pscustomobject]@{
        generatedAt = (Get-Date).ToString("s")
        environment = $EnvironmentName
        frontendUrl = $FrontendUrl
        apiUrl = $ApiUrl
        findings = $findings
        passed = -not ($findings | Where-Object { $_.status -eq "FAIL" })
    }

    $jsonPath = Join-Path $OutputDirectory "dynamic-pentest-regression.json"
    $htmlPath = Join-Path $OutputDirectory "dynamic-pentest-regression.html"
    $report | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8
    Write-DynamicPentestHtml -Path $htmlPath -Report $report

    foreach ($finding in $findings) {
        $line = "$($finding.id) $($finding.status): $($finding.title) [$($finding.target)]"
        if ($finding.status -eq "FAIL") {
            $errors.Add($line)
        } else {
            $summary.Add($line)
        }
    }

    $summary.Add("Dynamic pentest JSON report: $jsonPath")
    $summary.Add("Dynamic pentest HTML report: $htmlPath")
}

if (-not (Test-Path $backendPath)) {
    throw "Backend folder not found: $backendPath"
}
if (-not (Test-Path $frontendPath)) {
    throw "Frontend folder not found: $frontendPath"
}

if (-not $SkipGitleaks) {
    if (-not (Test-CommandAvailable "gitleaks")) {
        $errors.Add("gitleaks is not installed. Install: winget install --id Gitleaks.Gitleaks -e")
    } else {
        $gitleaksModeArg = if ($ScanGitHistory) { @() } else { @("--no-git") }
        $gitleaksConfigArg = if (Test-Path $gitleaksConfig) { @("--config", $gitleaksConfig) } else { @() }
        Run-Step "Gitleaks backend" {
            $reportPath = Join-Path $reportDir "gitleaks-back.sarif"
            Push-Location $backendPath
            gitleaks detect @gitleaksModeArg @gitleaksConfigArg --source . --report-format sarif --report-path $reportPath
            if ($LASTEXITCODE -eq 1 -and (Test-Path $reportPath)) {
                $res = Get-Content $reportPath -Raw | ConvertFrom-Json
                $count = @($res.runs.results).Count
                $warnings.Add("Gitleaks backend leaks found: $count")
                $global:LASTEXITCODE = 0
            }
            Pop-Location
        }
        Run-Step "Gitleaks frontend" {
            $reportPath = Join-Path $reportDir "gitleaks-front.sarif"
            Push-Location $frontendPath
            gitleaks detect @gitleaksModeArg @gitleaksConfigArg --source . --report-format sarif --report-path $reportPath
            if ($LASTEXITCODE -eq 1 -and (Test-Path $reportPath)) {
                $res = Get-Content $reportPath -Raw | ConvertFrom-Json
                $count = @($res.runs.results).Count
                $warnings.Add("Gitleaks frontend leaks found: $count")
                $global:LASTEXITCODE = 0
            }
            Pop-Location
        }
    }
}

if (-not $SkipDotnetVuln) {
    if (-not (Test-CommandAvailable "dotnet")) {
        $errors.Add("dotnet is not installed or not in PATH.")
    } else {
        Run-Step ".NET vulnerable packages (backend)" {
            $outFile = Join-Path $reportDir "dotnet-vulnerable-back.txt"
            Push-Location (Join-Path $backendPath "Api")
            dotnet list package --vulnerable --include-transitive | Tee-Object -FilePath $outFile
            Pop-Location
        }
    }
}

if (-not $SkipNpmAudit) {
    if (-not (Test-CommandAvailable "npm")) {
        $errors.Add("npm is not installed or not in PATH.")
    } else {
        Run-Step "npm audit (frontend)" {
            $outFile = Join-Path $reportDir "npm-audit-front.json"
            Push-Location $frontendPath
            npm audit --audit-level=high --json | Tee-Object -FilePath $outFile | Out-Null
            if (Test-Path $outFile) {
                $audit = Get-Content $outFile -Raw | ConvertFrom-Json
                if ($null -ne $audit.metadata -and $null -ne $audit.metadata.vulnerabilities) {
                    $v = $audit.metadata.vulnerabilities
                    $warnings.Add("npm audit frontend: high=$($v.high), critical=$($v.critical), total=$($v.total)")
                }
                # npm audit uses non-zero exit code when vulnerabilities are found.
                $global:LASTEXITCODE = 0
            }
            Pop-Location
        }
    }
}

if (-not $SkipSemgrep) {
    $semgrepRunner = Get-SemgrepRunner
    if ($null -eq $semgrepRunner) {
        $errors.Add("semgrep is not available. Install: pip install semgrep and ensure semgrep.exe exists in %APPDATA%\\Python\\PythonXX\\Scripts.")
    } else {
        Run-Step "Semgrep OWASP backend" {
            Push-Location $backendPath
            & $semgrepRunner.Value --config p/owasp-top-ten --json --output (Join-Path $reportDir "semgrep-back.json")
            Pop-Location
        }
        Run-Step "Semgrep OWASP frontend" {
            Push-Location $frontendPath
            & $semgrepRunner.Value --config p/owasp-top-ten --json --output (Join-Path $reportDir "semgrep-front.json")
            Pop-Location
        }
    }
}

if (-not $SkipPentestRegression) {
    Run-Step "Pentest regression checks" {
        $backendAuthController = Join-Path $backendPath "Api\Controller\AuthController.cs"
        $backendProgram = Join-Path $backendPath "Api\Program.cs"
        $backendContentController = Join-Path $backendPath "Api\Controller\ContentController.cs"
        $backendCreateReferral = Join-Path $backendPath "Core\Feature\Referrals\CreateReferral\CreateReferralRequestHandler.cs"
        $backendFabricValidation = Join-Path $backendPath "Core\Feature\Fabric\GetValidateReferred\GetValidateReferredRequestHandler.cs"
        $backendAuthService = Join-Path $backendPath "Core\Service\Identity\AuthService.cs"
        $backendCoreRegistration = Join-Path $backendPath "Core\CoreServiceRegistration.cs"
        $backendDbContext = Join-Path $backendPath "Core\DBContext\SolvoRefAppContext.cs"
        $backendReferralRepo = Join-Path $backendPath "Core\Repositories\Referral\ReferralRepository.cs"
        $backendFileValidator = Join-Path $backendPath "Core\Security\FileUploadValidator.cs"
        $backendDuplicateKey = Join-Path $backendPath "Core\Security\ReferralDuplicateKey.cs"
        $backendInputSanitizer = Join-Path $backendPath "Core\Security\InputSanitizer.cs"
        $backendMigration = Join-Path $backendPath "Core\Migrations\20260424230130_AddReferralDuplicateSubmissionKey.cs"
        $backendSwaggerFilter = Join-Path $backendPath "Api\Swagger\AuthorizeCheckOperationFilter.cs"
        $frontendAuthContext = Join-Path $frontendPath "src\components\AuthContextComponent\AuthContext.js"
        $frontendUseApi = Join-Path $frontendPath "src\components\CustomHook\UseApi.jsx"

        Assert-PatternAbsent "F-03" "Frontend should not read auth_token from URL query string." $frontendAuthContext "searchParams.get('auth_token')"
        Assert-PatternAbsent "F-03" "Frontend should not write auth_token to URL query string." $frontendAuthContext "searchParams.set('auth_token')"
        Assert-PatternAbsent "F-03" "Frontend should not reference auth_token query parameter." $frontendAuthContext "auth_token="

        Assert-PatternAbsent "F-04" "Frontend should not persist auth token in localStorage." $frontendAuthContext "localStorage.setItem('authToken'"
        Assert-PatternAbsent "F-04" "Frontend should not read auth token from localStorage." $frontendAuthContext "localStorage.getItem('authToken'"
        Assert-PatternAbsent "F-04" "Frontend should not use localStorage for tokens." $frontendAuthContext "localStorage"

        Assert-PatternPresent "DF-01" "Logout endpoint should require authorization." $backendAuthController "[Authorize]"
        Assert-PatternPresent "DF-01" "Logout should rotate security stamp." $backendAuthController "UpdateSecurityStampAsync"
        Assert-PatternPresent "DF-01" "Auth service should emit security stamp claim." $backendAuthService '"sstamp"'
        Assert-PatternPresent "DF-01" "JWT middleware should revoke mismatched stamps." $backendCoreRegistration "Token revoked."

        Assert-PatternPresent "DF-07/F-11" "Global rate limiter should be configured." $backendProgram "AddRateLimiter"
        Assert-PatternPresent "DF-07/F-11" "Referral create limiter should exist." $backendProgram "referral-create"
        Assert-PatternPresent "DF-07/F-11" "Fabric validation limiter should exist." $backendProgram "fabric-validate"
        Assert-PatternPresent "DF-07/F-11" "Admin write limiter should exist." $backendProgram "admin-content-write"
        Assert-PatternPresent "DF-07/F-11" "Rate limiter middleware should be active." $backendProgram "UseRateLimiter();"

        Assert-PatternPresent "DF-08" "Duplicate key generator should exist." $backendDuplicateKey "SHA256"
        Assert-PatternPresent "DF-08" "Database unique index should exist for duplicate referrals." $backendDbContext "HasIndex(x => x.ReferralSubmissionKey)"
        Assert-PatternPresent "DF-08" "Repository duplicate check should exist." $backendReferralRepo "ExistsDuplicateSubmission"
        Assert-PatternPresent "DF-08" "Referral creation should block duplicates." $backendCreateReferral "already been submitted by you"
        Assert-PatternPresent "DF-08" "Migration should add submission key unique index." $backendMigration "IX_Referral_ReferralSubmissionKey"

        Assert-PatternPresent "DF-09" "Referral creation should call Fabric validation." $backendCreateReferral "_fabricService.ReferredValidation"
        Assert-PatternPresent "DF-09" "Referral creation should fail closed on invalid Fabric validation." $backendCreateReferral "does not meet the referral program requirements"
        $warnings.Add("DF-09 is only partially verifiable statically. The scanner confirms app-side fail-closed validation, not upstream Fabric correctness.")

        Assert-PatternPresent "DF-10" "Referral flow should sanitize user input." $backendCreateReferral "InputSanitizer.SanitizePlainText"
        Assert-PatternPresent "DF-10/DF-14" "Sanitizer should exist." $backendInputSanitizer "SanitizeHtmlFragment"
        Assert-PatternPresent "DF-14" "CMS content should sanitize rich HTML." $backendContentController "SanitizeHtmlFragment"

        Assert-PatternPresent "DF-13" "File upload validator should exist." $backendFileValidator "ValidateImage"
        Assert-PatternPresent "DF-13" "Admin content upload should use file validator." $backendContentController "FileUploadValidator.ValidateImage"
        Assert-PatternPresent "DF-13" "Upload validator should restrict extensions." $backendFileValidator ".webp"

        Assert-PatternPresent "DF-04/F-13" "API should emit X-Content-Type-Options." $backendProgram "X-Content-Type-Options"
        Assert-PatternPresent "DF-04/F-13" "API should emit X-Frame-Options." $backendProgram "X-Frame-Options"
        Assert-PatternPresent "DF-04/F-13" "API should emit Content-Security-Policy." $backendProgram "Content-Security-Policy"
        Assert-PatternPresent "DF-04/F-13" "API should emit Cross-Origin-Opener-Policy." $backendProgram "Cross-Origin-Opener-Policy"
        Assert-PatternPresent "F-08" "Swagger should only be enabled in development." $backendProgram "if (app.Environment.IsDevelopment())"
        Assert-PatternPresent "DF-03" "Swagger auth metadata should be applied per authorized endpoint." $backendSwaggerFilter "AuthorizeCheckOperationFilter"

        $reportPath = Join-Path $reportDir "pentest-regression.txt"
        @(
            "Pentest regression scan: $(Get-Date -Format s)"
            ""
            "This check is static and code-focused. DF-09 remains partially external."
        ) | Set-Content -Path $reportPath -Encoding UTF8
        $summary | ForEach-Object { $_ | Out-File -FilePath $reportPath -Append -Encoding UTF8 }
        if ($warnings.Count -gt 0) {
            "" | Out-File -FilePath $reportPath -Append -Encoding UTF8
            "Warnings:" | Out-File -FilePath $reportPath -Append -Encoding UTF8
            $warnings | ForEach-Object { $_ | Out-File -FilePath $reportPath -Append -Encoding UTF8 }
        }
        if ($errors.Count -gt 0) {
            "" | Out-File -FilePath $reportPath -Append -Encoding UTF8
            "Errors:" | Out-File -FilePath $reportPath -Append -Encoding UTF8
            $errors | ForEach-Object { $_ | Out-File -FilePath $reportPath -Append -Encoding UTF8 }
        }
    }
}

if (-not $SkipDynamicPentest) {
    Run-Step "Dynamic pentest regression ($Environment)" {
        Run-DynamicPentestRegression -EnvironmentName $Environment -FrontendUrl $FrontendTargetUrl -ApiUrl $ApiTargetUrl -OutputDirectory $reportDir -BurstCount $RateLimitProbeCount
    }
}

if ($RunZapBaseline) {
    $zapRunner = Get-ZapRunner
    if ($null -eq $zapRunner) {
        $warnings.Add("ZAP is not installed. Install OWASP ZAP to run the baseline scan.")
    } else {
        Run-Step "ZAP baseline" {
            $outFile = Join-Path $reportDir "zap-baseline.txt"
            & $zapRunner -cmd -quickurl $ZapTargetUrl -quickout $outFile
        }
    }
}

$summaryFile = Join-Path $reportDir "summary.txt"
"Scan finished: $(Get-Date -Format s)" | Out-File -FilePath $summaryFile -Encoding UTF8
"Root: $root" | Out-File -FilePath $summaryFile -Append -Encoding UTF8
"Environment: $Environment" | Out-File -FilePath $summaryFile -Append -Encoding UTF8
"Frontend target: $FrontendTargetUrl" | Out-File -FilePath $summaryFile -Append -Encoding UTF8
"API target: $ApiTargetUrl" | Out-File -FilePath $summaryFile -Append -Encoding UTF8
"" | Out-File -FilePath $summaryFile -Append -Encoding UTF8
"Completed steps:" | Out-File -FilePath $summaryFile -Append -Encoding UTF8
if ($summary.Count -eq 0) {
    " - none" | Out-File -FilePath $summaryFile -Append -Encoding UTF8
} else {
    $summary | ForEach-Object { " - $_" | Out-File -FilePath $summaryFile -Append -Encoding UTF8 }
}
"" | Out-File -FilePath $summaryFile -Append -Encoding UTF8
"Warnings:" | Out-File -FilePath $summaryFile -Append -Encoding UTF8
if ($warnings.Count -eq 0) {
    " - none" | Out-File -FilePath $summaryFile -Append -Encoding UTF8
} else {
    $warnings | ForEach-Object { " - $_" | Out-File -FilePath $summaryFile -Append -Encoding UTF8 }
}
"" | Out-File -FilePath $summaryFile -Append -Encoding UTF8
"Errors / missing tools:" | Out-File -FilePath $summaryFile -Append -Encoding UTF8
if ($errors.Count -eq 0) {
    " - none" | Out-File -FilePath $summaryFile -Append -Encoding UTF8
} else {
    $errors | ForEach-Object { " - $_" | Out-File -FilePath $summaryFile -Append -Encoding UTF8 }
}

Write-Host ""
Write-Host "Reports directory: $reportDir" -ForegroundColor Yellow
Write-Host "Summary file: $summaryFile" -ForegroundColor Yellow

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "Some checks failed or tools are missing:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
    exit 1
}

exit 0
