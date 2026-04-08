Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
$imageRoot = Join-Path $repoRoot "SharedStorage\\images"
$attributionPath = Join-Path $imageRoot "ATTRIBUTION.md"

New-Item -ItemType Directory -Force -Path $imageRoot | Out-Null

$imageSpecs = @(
    @{
        Destination = "poi-vinh-khanh-food-street.png"
        PageUrl = "https://commons.wikimedia.org/wiki/File%3APh%C3%A1_l%E1%BA%A5u_as_served_in_Vietnam.jpeg"
        DownloadUrl = "https://commons.wikimedia.org/wiki/Special:Redirect/file/Ph%C3%A1%20l%E1%BA%A5u%20as%20served%20in%20Vietnam.jpeg"
        Author = "Policyandinternet"
        License = "CC BY-SA 4.0"
        TargetWidth = 1600
        TargetHeight = 900
        CropFocus = "center"
    },
    @{
        Destination = "poi-vinh-khanh-seafood-cluster.png"
        PageUrl = "https://commons.wikimedia.org/wiki/File%3ASaigon_Street_Corner_Dining_%28at_noon%29.jpg"
        DownloadUrl = "https://commons.wikimedia.org/wiki/Special:Redirect/file/Saigon%20Street%20Corner%20Dining%20%28at%20noon%29.jpg"
        Author = "Alan Turkus"
        License = "CC BY 2.0"
        TargetWidth = 1600
        TargetHeight = 900
        CropFocus = "center"
    },
    @{
        Destination = "poi-khanh-hoi-bus-stop.png"
        PageUrl = "https://commons.wikimedia.org/wiki/File%3APedestrian_on_Khanh_Hoi_Bridge.jpg"
        DownloadUrl = "https://commons.wikimedia.org/wiki/Special:Redirect/file/Pedestrian%20on%20Khanh%20Hoi%20Bridge.jpg"
        Author = "Syced"
        License = "CC0 1.0"
        TargetWidth = 1600
        TargetHeight = 900
        CropFocus = "center"
    },
    @{
        Destination = "poi-vinh-hoi-bus-stop.png"
        PageUrl = "https://commons.wikimedia.org/wiki/File%3A%C4%90%C3%ACnh_V%C4%A9nh_H%E1%BB%99i%2C_b%E1%BA%BFn_v%C3%A2n_%C4%91%E1%BB%93n_q4_tphcm_vn_-_panoramio.jpg"
        DownloadUrl = "https://commons.wikimedia.org/wiki/Special:Redirect/file/%C4%90%C3%ACnh%20V%C4%A9nh%20H%E1%BB%99i%2C%20b%E1%BA%BFn%20v%C3%A2n%20%C4%91%E1%BB%93n%20q4%20tphcm%20vn%20-%20panoramio.jpg"
        Author = "trungydang"
        License = "CC BY 3.0"
        TargetWidth = 1600
        TargetHeight = 900
        CropFocus = "center"
    },
    @{
        Destination = "poi-xuan-chieu-bus-stop.png"
        PageUrl = "https://commons.wikimedia.org/wiki/File%3ANh%C3%A0_th%E1%BB%9D_X%C3%B3m_Chi%E1%BA%BFu.jpg"
        DownloadUrl = "https://commons.wikimedia.org/wiki/Special:Redirect/file/Nh%C3%A0%20th%E1%BB%9D%20X%C3%B3m%20Chi%E1%BA%BFu.jpg"
        Author = "Akira2112"
        License = "CC BY 4.0"
        TargetWidth = 1600
        TargetHeight = 900
        CropFocus = "top"
    },
    @{
        Destination = "poi-vinh-khanh-street-life.png"
        PageUrl = "https://commons.wikimedia.org/wiki/File%3AA_woman_rides_a_bicycle_in_District_4%2C_Ho_Chi_Minh_City%2C_Vietnam.jpg"
        DownloadUrl = "https://commons.wikimedia.org/wiki/Special:Redirect/file/A%20woman%20rides%20a%20bicycle%20in%20District%204%2C%20Ho%20Chi%20Minh%20City%2C%20Vietnam.jpg"
        Author = "Kwozyn"
        License = "CC BY-SA 4.0"
        TargetWidth = 1600
        TargetHeight = 900
        CropFocus = "center"
    }
)

function Get-CropRectangle([System.Drawing.Image]$image, [int]$targetWidth, [int]$targetHeight, [string]$cropFocus)
{
    $sourceWidth = [double]$image.Width
    $sourceHeight = [double]$image.Height
    $targetRatio = [double]$targetWidth / [double]$targetHeight
    $sourceRatio = $sourceWidth / $sourceHeight

    if ($sourceRatio -gt $targetRatio)
    {
        $cropHeight = $image.Height
        $cropWidth = [int][Math]::Round($cropHeight * $targetRatio)
        $x = [int][Math]::Floor(($image.Width - $cropWidth) / 2)
        $y = 0
    }
    else
    {
        $cropWidth = $image.Width
        $cropHeight = [int][Math]::Round($cropWidth / $targetRatio)
        $x = 0
        switch ($cropFocus)
        {
            "top" { $y = 0 }
            "bottom" { $y = [Math]::Max($image.Height - $cropHeight, 0) }
            default { $y = [int][Math]::Floor(($image.Height - $cropHeight) / 2) }
        }
    }

    return New-Object System.Drawing.Rectangle $x, $y, $cropWidth, $cropHeight
}

function Save-CroppedImage([string]$sourcePath, [string]$destinationPath, [int]$targetWidth, [int]$targetHeight, [string]$cropFocus)
{
    $sourceImage = [System.Drawing.Image]::FromFile($sourcePath)
    try
    {
        $cropRect = Get-CropRectangle -image $sourceImage -targetWidth $targetWidth -targetHeight $targetHeight -cropFocus $cropFocus
        $bitmap = New-Object System.Drawing.Bitmap $targetWidth, $targetHeight
        try
        {
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try
            {
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.DrawImage(
                    $sourceImage,
                    (New-Object System.Drawing.Rectangle 0, 0, $targetWidth, $targetHeight),
                    $cropRect,
                    [System.Drawing.GraphicsUnit]::Pixel)
            }
            finally
            {
                $graphics.Dispose()
            }

            $bitmap.Save($destinationPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally
        {
            $bitmap.Dispose()
        }
    }
    finally
    {
        $sourceImage.Dispose()
    }
}

function Invoke-DownloadWithRetry([string]$uri, [string]$destinationPath)
{
    $headers = @{
        "User-Agent" = "AudioGuideSystem-Demo/1.0 (educational project)"
    }

    for ($attempt = 1; $attempt -le 5; $attempt++)
    {
        try
        {
            Invoke-WebRequest -Uri $uri -Headers $headers -OutFile $destinationPath
            return
        }
        catch
        {
            if ($attempt -eq 5)
            {
                throw
            }

            Start-Sleep -Seconds (5 * $attempt)
        }
    }
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("audio-tour-images-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

try
{
    foreach ($spec in $imageSpecs)
    {
        $downloadTarget = Join-Path $tempRoot ([System.IO.Path]::GetFileName($spec.Destination) + ".download")
        Invoke-DownloadWithRetry -uri $spec.DownloadUrl -destinationPath $downloadTarget

        $destinationPath = Join-Path $imageRoot $spec.Destination
        Save-CroppedImage -sourcePath $downloadTarget -destinationPath $destinationPath -targetWidth $spec.TargetWidth -targetHeight $spec.TargetHeight -cropFocus $spec.CropFocus
        Write-Host "Saved $($spec.Destination)"
    }

    $lines = @(
        "# Wikimedia Commons image attributions",
        "",
        "These local image files were downloaded from Wikimedia Commons and cropped to 1600x900 for the demo app.",
        "",
        "| Local file | Source page | Author | License |",
        "| --- | --- | --- | --- |"
    )

    foreach ($spec in $imageSpecs)
    {
        $pageName = [System.IO.Path]::GetFileName($spec.PageUrl)
        $lines += "| $($spec.Destination) | <$($spec.PageUrl)> | $($spec.Author) | $($spec.License) |"
    }

    Set-Content -LiteralPath $attributionPath -Value $lines -Encoding UTF8
}
finally
{
    if (Test-Path $tempRoot)
    {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

Write-Host "POI images downloaded successfully."
