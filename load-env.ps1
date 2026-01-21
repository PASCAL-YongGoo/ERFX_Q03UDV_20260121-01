# .env 파일 경로 (사용자 홈 디렉토리)
$envPath = "$env:USERPROFILE\.env"

# .env 파일 로드
if (Test-Path $envPath) {
    Get-Content $envPath | ForEach-Object {
        if ($_ -match '^([^=]+)=(.*)$' -and $_ -notmatch '^#') {
            Set-Item "env:$($matches[1])" $matches[2]
            Write-Host "Loaded: $($matches[1])" -ForegroundColor Green
        }
    }
} else {
    Write-Host "Warning: .env not found at $envPath" -ForegroundColor Yellow
}

# Claude Code 실행
claude $args