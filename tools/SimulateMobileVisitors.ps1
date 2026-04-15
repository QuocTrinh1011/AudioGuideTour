param(
    [string]$ApiBaseUrl = "http://127.0.0.1:5297",
    [int]$VisitorCount = 8,
    [int]$TrackingPointsPerVisitor = 16,
    [int]$DelayMilliseconds = 0,
    [double]$CenterLatitude = 10.76095,
    [double]$CenterLongitude = 106.70412
)

$ErrorActionPreference = "Stop"

function Invoke-JsonPost {
    param(
        [string]$Url,
        [object]$Body
    )

    $json = $Body | ConvertTo-Json -Depth 8
    Invoke-RestMethod -Method Post -Uri $Url -ContentType "application/json" -Body $json | Out-Null
}

function Get-LanguageForIndex {
    param([int]$Index)

    switch ($Index % 3) {
        0 { return "vi-VN" }
        1 { return "en-US" }
        default { return "zh-CN" }
    }
}

function Get-RandomOffset {
    param([double]$Min, [double]$Max)

    return ($Min + ((Get-Random -Minimum 0 -Maximum 1000000) / 1000000.0) * ($Max - $Min))
}

$baseUrl = $ApiBaseUrl.TrimEnd("/")
$runStamp = Get-Date -Format "yyyyMMddHHmmss"
$nowUtc = [DateTime]::UtcNow

Write-Host "Simulating $VisitorCount visitor(s) against $baseUrl ..."

for ($visitorIndex = 1; $visitorIndex -le $VisitorCount; $visitorIndex++) {
    $language = Get-LanguageForIndex -Index $visitorIndex
    $userId = "sim-user-$runStamp-$('{0:D2}' -f $visitorIndex)"
    $deviceId = "sim-device-$runStamp-$('{0:D2}' -f $visitorIndex)"
    $displayName = "Khách test $('{0:D2}' -f $visitorIndex)"

    Invoke-JsonPost -Url "$baseUrl/api/user" -Body @{
        id = $userId
        deviceId = $deviceId
        displayName = $displayName
        language = $language
        allowBackgroundTracking = $true
        allowAutoPlay = $true
    }

    $latBase = $CenterLatitude + ($visitorIndex * 0.00003)
    $lngBase = $CenterLongitude + (($visitorIndex % 4) * 0.00004)
    $recordedAt = $nowUtc.AddMinutes(-1 * ($VisitorCount - $visitorIndex))

    for ($pointIndex = 0; $pointIndex -lt $TrackingPointsPerVisitor; $pointIndex++) {
        $lat = $latBase + ($pointIndex * 0.00001) + (Get-RandomOffset -Min -0.000015 -Max 0.000015)
        $lng = $lngBase + ($pointIndex * 0.000008) + (Get-RandomOffset -Min -0.000015 -Max 0.000015)
        $pointTime = $recordedAt.AddSeconds($pointIndex * 8)

        Invoke-JsonPost -Url "$baseUrl/api/tracking" -Body @{
            userId = $userId
            deviceId = $deviceId
            language = $language
            latitude = [math]::Round($lat, 6)
            longitude = [math]::Round($lng, 6)
            accuracy = [math]::Round((6 + (Get-RandomOffset -Min 0 -Max 18)), 1)
            speedMetersPerSecond = [math]::Round((Get-RandomOffset -Min 0.2 -Max 1.6), 2)
            bearing = [math]::Round((Get-RandomOffset -Min 0 -Max 359), 1)
            isForeground = ($pointIndex % 2 -eq 0)
            recordedAt = $pointTime.ToString("o")
        }

        if ($DelayMilliseconds -gt 0) {
            Start-Sleep -Milliseconds $DelayMilliseconds
        }
    }

    Write-Host "Created $displayName ($language) with $TrackingPointsPerVisitor tracking points."
}

Write-Host "Done. Open admin /Visitor and /Analytics to verify active visitors and heatmap."
