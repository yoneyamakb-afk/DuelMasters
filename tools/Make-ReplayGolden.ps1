\
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$InputTracePath,              # 実戦トレース (JSONL or JSON array)
        [Parameter(Mandatory=$true)]
        [string]$OutputGoldenPath            # 出力先 (tests/DMRules.Tests/Golden/*.jsonl)
    )
    Write-Host "[M25] Normalize & export replay trace golden" -ForegroundColor Cyan

    if (-not (Test-Path $InputTracePath)) {
        throw "Input not found: $InputTracePath"
    }
    $content = Get-Content -Raw -Encoding UTF8 $InputTracePath

    # 簡易：JSON 配列なら行分割に、JSONLならそのまま
    function Split-LinesFromJsonLike($raw) {
        $trim = $raw.Trim()
        if ($trim.StartsWith("[")) {
            try {
                $arr = $trim | ConvertFrom-Json
                return $arr | ForEach-Object { $_ | ConvertTo-Json -Compress }
            } catch {
                throw "Invalid JSON array input."
            }
        } else {
            return $raw -split "`r?`n"
        }
    }

    # Normalize（時刻/Guid/Hex/一般ID）
    function Normalize-Line($line) {
        if ([string]::IsNullOrWhiteSpace($line)) { return $line }
        $s = $line
        $s = [Regex]::Replace($s, "\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(?:\.\d+)?Z?", "<T>")
        $s = [Regex]::Replace($s, "\b[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}\b", "<GUID>")
        $s = [Regex]::Replace($s, "\b0x[0-9a-fA-F]+\b", "<HEX>")
        $s = [Regex]::Replace($s, "(?<=\`"(id|eventId|stackId)\`"\s*:\s*\`")[^\`"]+(?=\`")", "<ID>", "IgnoreCase")
        return $s
    }

    $lines = Split-LinesFromJsonLike $content | Where-Object { $_.Trim() -ne "" }
    $norm  = $lines | ForEach-Object { Normalize-Line $_ }

    New-Item -ItemType Directory -Force -Path (Split-Path $OutputGoldenPath) | Out-Null
    Set-Content -Path $OutputGoldenPath -Value ($norm -join "`n") -Encoding UTF8

    Write-Host "[M25] Golden written: $OutputGoldenPath" -ForegroundColor Green
