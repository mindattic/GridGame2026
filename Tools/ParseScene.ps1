param([string]$ScenePath)
# Comprehensive Unity scene parser that extracts full hierarchy with resolved component names

# Build GUID -> script name map from .meta files
$guidMap = @{}
Get-ChildItem "D:\Projects\Unity\GridGame2026\Assets" -Filter "*.cs.meta" -Recurse | ForEach-Object {
    $c = [System.IO.File]::ReadAllText($_.FullName)
    if ($c -match 'guid:\s*([a-f0-9]{32})') { $guidMap[$matches[1].Substring(0,8)] = ($_.Name -replace '\.cs\.meta$','') }
}
# Unity built-in script GUIDs (consistent across projects)
$guidMap['fe87c0e1'] = 'Image'
$guidMap['1344c3c8'] = 'RawImage'
$guidMap['0cd44c10'] = 'CanvasScaler'
$guidMap['dc42784c'] = 'GraphicRaycaster'
$guidMap['76c392e4'] = 'EventSystem'
$guidMap['4f231c4f'] = 'StandaloneInputModule'
$guidMap['948f4100'] = 'UniversalAdditionalCameraData'
$guidMap['a79441f3'] = 'UniversalAdditionalCameraData'
$guidMap['cd0c81fe'] = 'Image'
$guidMap['2a4db7a1'] = 'Image'
$guidMap['1367256a'] = 'TextMeshProUGUI'
$guidMap['f4688fdb'] = 'TextMeshProUGUI'
$guidMap['d199490a'] = 'TextMeshPro'
$guidMap['a076d1e2'] = 'Mask'
$guidMap['31a19414'] = 'ScrollRect'
$guidMap['2a4db7a1'] = 'Image'
$guidMap['4e29b1a8'] = 'Button'
$guidMap['14e6513a'] = 'Scrollbar'
$guidMap['59f8146e'] = 'VerticalLayoutGroup'
$guidMap['3245ec92'] = 'HorizontalLayoutGroup'
$guidMap['306cc8c2'] = 'GridLayoutGroup'
$guidMap['1679637b'] = 'ContentSizeFitter'
$guidMap['2fafe2c0'] = 'LayoutElement'
$guidMap['d5bd5763'] = 'Toggle'
$guidMap['67db9e01'] = 'Slider'
$guidMap['e948e6e1'] = 'Dropdown'
$guidMap['b53ea835'] = 'TMP_Dropdown'
$guidMap['87decbba'] = 'InputField'
$guidMap['2da0c512'] = 'TMP_InputField'

$scene = Get-Content $ScenePath -Raw
$blocks = $scene -split '(?=--- !u!)'

# Unity type ID -> name for built-in components
$builtIn = @{
    '4'='Transform';'20'='Camera';'23'='MeshRenderer';'33'='MeshFilter';'54'='Rigidbody';
    '61'='BoxCollider';'64'='MeshCollider';'65'='CapsuleCollider';'81'='AudioListener';
    '82'='AudioSource';'95'='Animator';'108'='Light';'111'='Animation';'114'='MonoBehaviour';
    '120'='LineRenderer';'135'='SphereCollider';'136'='CircleCollider2D';'137'='BoxCollider2D';
    '143'='CharacterController';'145'='SpringJoint2D';'210'='SortingGroup';'212'='SpriteRenderer';
    '222'='CanvasRenderer';'223'='Canvas';'224'='RectTransform';'225'='CanvasGroup';'226'='CanvasScaler'
}

$goMap = @{}; $rtMap = @{}; $trMap = @{}; $compMap = @{}

foreach ($b in $blocks) {
    if ($b -match '--- !u!1 &(\d+)') {
        $id = $matches[1]
        $n = if ($b -match 'm_Name:\s+(.+)') { $matches[1].Trim() } else { '?' }
        $ly = if ($b -match 'm_Layer:\s+(\d+)') { [int]$matches[1] } else { 0 }
        $ac = if ($b -match 'm_IsActive:\s+(\d+)') { [int]$matches[1] } else { 1 }
        $cs = @(); [regex]::Matches($b, 'component:\s*\{fileID:\s*(\d+)\}') | ForEach-Object { $cs += $_.Groups[1].Value }
        $goMap[$id] = @{N=$n;L=$ly;A=$ac;C=$cs}
        continue
    }
    if ($b -match '--- !u!224 &(\d+)') {
        $id = $matches[1]
        $gi = if ($b -match 'm_GameObject:\s*\{fileID:\s*(\d+)') { $matches[1] } else { '' }
        $fa = if ($b -match 'm_Father:\s*\{fileID:\s*(\d+)') { $matches[1] } else { '0' }
        $amin = if ($b -match 'm_AnchorMin:\s*\{x:\s*([\d.e+-]+),\s*y:\s*([\d.e+-]+)') { "$($matches[1]),$($matches[2])" } else { '0,0' }
        $amax = if ($b -match 'm_AnchorMax:\s*\{x:\s*([\d.e+-]+),\s*y:\s*([\d.e+-]+)') { "$($matches[1]),$($matches[2])" } else { '1,1' }
        $pv = if ($b -match 'm_Pivot:\s*\{x:\s*([\d.e+-]+),\s*y:\s*([\d.e+-]+)') { "$($matches[1]),$($matches[2])" } else { '0.5,0.5' }
        $sd = if ($b -match 'm_SizeDelta:\s*\{x:\s*([\d.e+-]+),\s*y:\s*([\d.e+-]+)') { "$($matches[1]),$($matches[2])" } else { '0,0' }
        $ap = if ($b -match 'm_AnchoredPosition:\s*\{x:\s*([\d.e+-]+),\s*y:\s*([\d.e+-]+)') { "$($matches[1]),$($matches[2])" } else { '0,0' }
        $rtMap[$id] = @{GO=$gi;F=$fa;Amin=$amin;Amax=$amax;Pv=$pv;Sd=$sd;Ap=$ap}
        continue
    }
    if ($b -match '--- !u!4 &(\d+)') {
        $id = $matches[1]
        $gi = if ($b -match 'm_GameObject:\s*\{fileID:\s*(\d+)') { $matches[1] } else { '' }
        $fa = if ($b -match 'm_Father:\s*\{fileID:\s*(\d+)') { $matches[1] } else { '0' }
        $trMap[$id] = @{GO=$gi;F=$fa}
        continue
    }
    if ($b -match '--- !u!(\d+) &(\d+)') {
        $tid = $matches[1]; $cid = $matches[2]
        if ($tid -notin @('1','4','224','3','2','104','157','196')) {
            $gi = if ($b -match 'm_GameObject:\s*\{fileID:\s*(\d+)') { $matches[1] } else { '' }
            $tn = ''
            if ($tid -eq '114') {
                $guid8 = if ($b -match 'm_Script:\s*\{[^}]*guid:\s*([a-f0-9]{8})') { $matches[1] } else { '' }
                if ($guid8 -and $guidMap.ContainsKey($guid8)) { $tn = $guidMap[$guid8] }
                elseif ($guid8) { $tn = "MonoBehaviour($guid8)" }
                else { $tn = 'MonoBehaviour' }
            } elseif ($builtIn.ContainsKey($tid)) { $tn = $builtIn[$tid] }
            else { $tn = "Component($tid)" }
            # Extract key properties
            $props = @()
            switch ($tid) {
                '20' { # Camera
                    if ($b -match 'orthographic:\s*(\d)') { $props += "ortho=$($matches[1])" }
                    if ($b -match 'orthographic size:\s*([\d.]+)') { $props += "size=$($matches[1])" }
                    if ($b -match 'm_Depth:\s*([\d.-]+)') { $props += "depth=$($matches[1])" }
                }
                '223' { # Canvas
                    if ($b -match 'm_RenderMode:\s*(\d)') { $rm = $matches[1]; $rmn = @{'0'='Overlay';'1'='Camera';'2'='WorldSpace'}[$rm]; $props += "mode=$rmn" }
                    if ($b -match 'm_SortingOrder:\s*(-?[\d]+)') { $props += "sort=$($matches[1])" }
                }
                '226' { # CanvasScaler
                    if ($b -match 'm_UiScaleMode:\s*(\d)') { $sm = @{'0'='ConstantPixel';'1'='ScaleWithScreenSize';'2'='ConstantPhysical'}[[string]$matches[1]]; $props += "mode=$sm" }
                    if ($b -match 'm_ReferenceResolution:\s*\{x:\s*([\d.]+),\s*y:\s*([\d.]+)') { $props += "ref=$($matches[1])x$($matches[2])" }
                    if ($b -match 'm_MatchWidthOrHeight:\s*([\d.]+)') { $props += "match=$($matches[1])" }
                }
            }
            $pstr = if ($props.Count -gt 0) { "($($props -join ', '))" } else { '' }
            $compMap[$cid] = @{T=$tid;GO=$gi;N="$tn$pstr"}
        }
    }
}

$allT = @{}
foreach ($k in $rtMap.Keys) { $allT[$k] = @{GO=$rtMap[$k].GO;F=$rtMap[$k].F;RT=$true} }
foreach ($k in $trMap.Keys) { if (-not $allT.ContainsKey($k)) { $allT[$k] = @{GO=$trMap[$k].GO;F=$trMap[$k].F;RT=$false} } }

function Show($tId, $ind) {
    $goId = $allT[$tId].GO
    if (-not $goMap.ContainsKey($goId)) { return }
    $go = $goMap[$goId]
    $parts = @($go.N)
    if ($go.L -ne 0) { $parts += "[L=$($go.L)]" }
    if ($go.A -eq 0) { $parts += "[OFF]" }
    # RT details
    if ($rtMap.ContainsKey($tId)) {
        $r = $rtMap[$tId]
        $stretch = ($r.Amin -eq '0,0' -and $r.Amax -eq '1,1' -and $r.Ap -eq '0,0' -and $r.Sd -eq '0,0')
        if ($stretch) { $parts += '{stretch}' }
        else {
            $rtd = "a=($($r.Amin)...$($r.Amax))"
            if ($r.Sd -ne '0,0') { $rtd += " sz=($($r.Sd))" }
            if ($r.Ap -ne '0,0') { $rtd += " pos=($($r.Ap))" }
            if ($r.Pv -ne '0.5,0.5') { $rtd += " pv=($($r.Pv))" }
            $parts += "{$rtd}"
        }
    }
    # Components
    $cnames = @()
    foreach ($cId in $go.C) { if ($compMap.ContainsKey($cId)) { $cnames += $compMap[$cId].N } }
    if ($cnames.Count -gt 0) { $parts += ":: $($cnames -join ' + ')" }
    Write-Host "$ind$($parts -join ' ')"
    $kids = @(); foreach ($k in $allT.Keys) { if ($allT[$k].F -eq $tId) { $kids += $k } }
    foreach ($kid in $kids) { Show $kid "$ind  " }
}

Write-Host "=== $(Split-Path $ScenePath -Leaf) ==="
$roots = @(); foreach ($k in $allT.Keys) { if ($allT[$k].F -eq '0') { $roots += $k } }
foreach ($r in $roots) { Show $r '' }
