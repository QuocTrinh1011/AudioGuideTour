param(
    [switch]$IncludeEnglishAudio,
    [switch]$IncludeVietnameseAudio,
    [switch]$RegenerateDemoImages
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
$sharedRoot = Join-Path $repoRoot "SharedStorage"
$imageRoot = Join-Path $sharedRoot "images"
$audioRoot = Join-Path $sharedRoot "audio"

New-Item -ItemType Directory -Force -Path $imageRoot | Out-Null
New-Item -ItemType Directory -Force -Path $audioRoot | Out-Null

$imageSpecs = @(
    @{
        FileName = "poi-vinh-khanh-food-street.png"
        Title = "Pho am thuc Vinh Khanh"
        Subtitle = "Cua ngo am thuc Quan 4, phu hop de bat dau tour dem."
        Footer = "Street food | seafood | grilled dishes"
        StartColor = "#14324B"
        EndColor = "#E2A93B"
    },
    @{
        FileName = "poi-vinh-khanh-seafood-cluster.png"
        Title = "Cum quan oc Vinh Khanh"
        Subtitle = "Cum quan oc va mon nuong dong khach nhat tren tuyen pho."
        Footer = "Seafood cluster | late-night dining"
        StartColor = "#13293D"
        EndColor = "#C97732"
    },
    @{
        FileName = "poi-khanh-hoi-bus-stop.png"
        Title = "Tram xe buyt Khanh Hoi"
        Subtitle = "Diem QR vao tour nhanh cho visitor den bang xe buyt."
        Footer = "QR stop | Khanh Hoi"
        StartColor = "#16324B"
        EndColor = "#3DA5D9"
    },
    @{
        FileName = "poi-vinh-hoi-bus-stop.png"
        Title = "Tram xe buyt Vinh Hoi"
        Subtitle = "Diem trung chuyen hoac ket tour, quet QR de nghe ngay."
        Footer = "QR stop | Vinh Hoi"
        StartColor = "#1B3B5F"
        EndColor = "#56A3A6"
    },
    @{
        FileName = "poi-xuan-chieu-bus-stop.png"
        Title = "Tram xe buyt Xuan Chieu"
        Subtitle = "Diem vao tu huong Xuan Chieu - Xom Chieu bang QR."
        Footer = "QR stop | Xuan Chieu"
        StartColor = "#19324A"
        EndColor = "#7A9E7E"
    },
    @{
        FileName = "poi-vinh-khanh-street-life.png"
        Title = "Nhip song khu vuc Vinh Khanh"
        Subtitle = "Diem ke chuyen ve nhip song duong pho va doi song ve dem."
        Footer = "Culture | street rhythm | District 4"
        StartColor = "#162A3A"
        EndColor = "#2A9D8F"
    },
    @{
        FileName = "tour-dem-vinh-khanh-45-phut.png"
        Title = "Dem Vinh Khanh 45 phut"
        Subtitle = "Lo trinh di bo tu tram xe buyt den pho am thuc va nhip song dem."
        Footer = "Walking tour | 45 minutes | multilingual audio"
        StartColor = "#102A43"
        EndColor = "#F0B429"
    }
)

function New-Color([string]$hex)
{
    return [System.Drawing.ColorTranslator]::FromHtml($hex)
}

function New-DemoCard([hashtable]$spec, [string]$destinationPath)
{
    $width = 1600
    $height = 900
    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    $backgroundRect = New-Object System.Drawing.Rectangle 0, 0, $width, $height
    $gradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush $backgroundRect, (New-Color $spec.StartColor), (New-Color $spec.EndColor), 35
    $graphics.FillRectangle($gradient, $backgroundRect)

    $overlayBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(42, 12, 22, 34))
    $graphics.FillEllipse($overlayBrush, -120, 520, 540, 420)
    $graphics.FillEllipse($overlayBrush, 1040, -80, 460, 360)

    $routePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(110, 255, 255, 255), 10)
    $graphics.DrawBezier($routePen, 150, 700, 520, 460, 920, 840, 1380, 530)
    $graphics.FillEllipse([System.Drawing.Brushes]::White, 140, 690, 26, 26)
    $graphics.FillEllipse([System.Drawing.Brushes]::White, 1368, 518, 26, 26)

    $panelBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(115, 8, 16, 25))
    $panelRect = New-Object System.Drawing.RectangleF 90, 110, 920, 470
    $graphics.FillRectangle($panelBrush, $panelRect)

    $titleFont = New-Object System.Drawing.Font "Segoe UI Semibold", 42, ([System.Drawing.FontStyle]::Bold)
    $subtitleFont = New-Object System.Drawing.Font "Segoe UI", 21, ([System.Drawing.FontStyle]::Regular)
    $footerFont = New-Object System.Drawing.Font "Segoe UI Semibold", 18, ([System.Drawing.FontStyle]::Bold)
    $tagFont = New-Object System.Drawing.Font "Segoe UI", 16, ([System.Drawing.FontStyle]::Regular)

    $titleBrush = [System.Drawing.Brushes]::White
    $subtitleBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(232, 242, 245))
    $footerBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(245, 215, 120))
    $tagBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(215, 228, 235))

    $graphics.DrawString("VINH KHANH AUDIO TOUR", $footerFont, $footerBrush, 130, 150)
    $graphics.DrawString($spec.Title, $titleFont, $titleBrush, (New-Object System.Drawing.RectangleF 126, 206, 840, 120))
    $graphics.DrawString($spec.Subtitle, $subtitleFont, $subtitleBrush, (New-Object System.Drawing.RectangleF 130, 352, 810, 120))
    $graphics.DrawString($spec.Footer, $tagFont, $tagBrush, 130, 500)

    $poiLabelBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(220, 255, 255, 255))
    $poiLabelFont = New-Object System.Drawing.Font "Segoe UI", 20, ([System.Drawing.FontStyle]::Bold)
    $graphics.DrawString("POI DEMO ASSET", $poiLabelFont, $poiLabelBrush, 1180, 760)
    $graphics.DrawString("SharedStorage/images", $tagFont, $tagBrush, 1178, 802)

    $bitmap.Save($destinationPath, [System.Drawing.Imaging.ImageFormat]::Png)

    $poiLabelFont.Dispose()
    $tagBrush.Dispose()
    $footerBrush.Dispose()
    $subtitleBrush.Dispose()
    $titleFont.Dispose()
    $subtitleFont.Dispose()
    $footerFont.Dispose()
    $tagFont.Dispose()
    $poiLabelBrush.Dispose()
    $panelBrush.Dispose()
    $routePen.Dispose()
    $overlayBrush.Dispose()
    $gradient.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

if ($RegenerateDemoImages)
{
    foreach ($spec in $imageSpecs)
    {
        $destination = Join-Path $imageRoot $spec.FileName
        New-DemoCard -spec $spec -destinationPath $destination
    }
}

function New-DesktopVoice()
{
    $voice = New-Object -ComObject SAPI.SpVoice
    $voiceTokens = $voice.GetVoices()
    $selectedVoice = $null
    for ($i = 0; $i -lt $voiceTokens.Count; $i++)
    {
        $token = $voiceTokens.Item($i)
        if ($token.GetDescription() -like "*Zira*")
        {
            $selectedVoice = $token
            break
        }
    }

    if ($selectedVoice -eq $null -and $voiceTokens.Count -gt 0)
    {
        $selectedVoice = $voiceTokens.Item(0)
    }

    if ($selectedVoice -eq $null)
    {
        throw "Khong tim thay voice SAPI de tao audio tieng Anh."
    }

    $voice.Voice = $selectedVoice
    return $voice
}

function New-OneCoreVoice([string]$languageKeyword)
{
    $category = New-Object -ComObject SAPI.SpObjectTokenCategory
    $category.SetId("HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices", $false)
    $tokens = $category.EnumerateTokens()
    $selectedToken = $null

    for ($i = 0; $i -lt $tokens.Count; $i++)
    {
        $token = $tokens.Item($i)
        if ($token.GetDescription() -like "*$languageKeyword*")
        {
            $selectedToken = $token
            break
        }
    }

    if ($selectedToken -eq $null)
    {
        throw "Khong tim thay OneCore voice phu hop voi '$languageKeyword'."
    }

    $voice = New-Object -ComObject SAPI.SpVoice
    $voice.Voice = $selectedToken
    return $voice
}

function Write-AudioFiles($voice, [hashtable[]]$audioSpecs)
{
    foreach ($audioSpec in $audioSpecs)
    {
        $destination = Join-Path $audioRoot $audioSpec.FileName
        if (Test-Path $destination)
        {
            Remove-Item -LiteralPath $destination -Force
        }

        $stream = New-Object -ComObject SAPI.SpFileStream
        $stream.Format.Type = 22
        $stream.Open($destination, 3, $false)
        $voice.AudioOutputStream = $stream
        $null = $voice.Speak($audioSpec.Text, 0)
        $stream.Close()
    }
}

if ($IncludeEnglishAudio)
{
    $voice = New-DesktopVoice

    $audioSpecs = @(
        @{ FileName = "poi-vinh-khanh-food-street-en.wav"; Text = "You are at the entrance of Vinh Khanh Food Street. This is the ideal place to begin the night-food walking tour in District Four." },
        @{ FileName = "poi-vinh-khanh-seafood-cluster-en.wav"; Text = "This seafood cluster is the busiest late-night section of Vinh Khanh, known for snails, grilled dishes, and the dense sidewalk dining atmosphere." },
        @{ FileName = "poi-khanh-hoi-bus-stop-en.wav"; Text = "Khanh Hoi Bus Stop is a quick entry point to the tour. Visitors can scan the QR code here and listen immediately without waiting for GPS." },
        @{ FileName = "poi-vinh-hoi-bus-stop-en.wav"; Text = "Vinh Hoi Bus Stop works well as a transfer point or tour ending point. The QR content here lets visitors start or resume the experience quickly." },
        @{ FileName = "poi-xuan-chieu-bus-stop-en.wav"; Text = "This is the QR entry point from the Xuan Chieu direction, also known as Xom Chieu. It helps visitors access the content quickly." },
        @{ FileName = "poi-vinh-khanh-street-life-en.wav"; Text = "This stop explains the social rhythm of Vinh Khanh at night, where food, conversation, and sidewalk life all blend into one experience." }
    )

    Write-AudioFiles -voice $voice -audioSpecs $audioSpecs
}

if ($IncludeVietnameseAudio)
{
    $voice = New-OneCoreVoice -languageKeyword "Vietnamese"
    $audioSpecs = @(
        @{ FileName = "poi-vinh-khanh-food-street-vi.wav"; Text = "Bạn đang đứng tại cửa ngõ phố ẩm thực Vĩnh Khánh. Đây là điểm hợp lý để bắt đầu hành trình ẩm thực về đêm của Quận Bốn." },
        @{ FileName = "poi-vinh-khanh-seafood-cluster-vi.wav"; Text = "Đây là cụm quán ốc và món nướng nhộn nhịp nhất trên phố Vĩnh Khánh, nơi du khách có thể cảm nhận rõ mùi vị hải sản và không khí vỉa hè." },
        @{ FileName = "poi-khanh-hoi-bus-stop-vi.wav"; Text = "Trạm xe buýt Khánh Hội là điểm vào tour nhanh cho du khách đến bằng xe buýt. Bạn có thể quét mã QR và nghe ngay tại đây." },
        @{ FileName = "poi-vinh-hoi-bus-stop-vi.wav"; Text = "Trạm xe buýt Vĩnh Hội phù hợp làm điểm trung chuyển hoặc kết tour. Nội dung tại đây giúp du khách tiếp tục hành trình một cách nhanh gọn." },
        @{ FileName = "poi-xuan-chieu-bus-stop-vi.wav"; Text = "Đây là điểm vào từ hướng Xuân Chiếu, còn gọi là Xóm Chiếu. Mã QR tại đây giúp du khách vào nội dung mà không cần chờ geofence." },
        @{ FileName = "poi-vinh-khanh-street-life-vi.wav"; Text = "Điểm này kể chuyện về nhịp sống phố phường của Vĩnh Khánh, nơi ẩm thực, giao tiếp và sinh hoạt vỉa hè hòa thành một trải nghiệm đêm rất riêng của Quận Bốn." }
    )

    Write-AudioFiles -voice $voice -audioSpecs $audioSpecs
}

Write-Host "Demo media generated successfully."
