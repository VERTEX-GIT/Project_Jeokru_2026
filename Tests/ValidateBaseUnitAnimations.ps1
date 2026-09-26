$root = Split-Path -Parent $PSScriptRoot
$expected = @{ Austin = @(4, 0, 4); Ernist = @(3, 4, 4); SimYoyeon = @(3, 4, 5); Victor = @(4, 0, 4); Zorba = @(3, 4, 4) }
$spriteMetas = Get-ChildItem (Join-Path $root 'Assets/Images/Animations/Base Units') -Filter '*.png.meta' -Recurse
$spriteRefs = @{}
foreach ($meta in $spriteMetas) {
    $source = Get-Content $meta.FullName -Raw
    $guid = [regex]::Match($source, '(?m)^guid: (\w+)').Groups[1].Value
    foreach ($id in [regex]::Matches($source, '(?m)^\s+213: (-?\d+)')) {
        $spriteRefs["$($id.Groups[1].Value):$guid"] = $true
    }
}
foreach ($name in $expected.Keys) {
    $asset = Get-Content (Join-Path $root "Assets/Animations/Base Units/${name}Animations.asset") -Raw
    $counts = @('walk', 'attack', 'down') | ForEach-Object {
        $section = [regex]::Match($asset, "(?ms)^  $_`:.*?(?=^  (?:walk|attack|down|framesPerSecond):|\z)").Value
        $refs = [regex]::Matches($section, 'fileID: (-?\d+), guid: (\w+)')
        foreach ($ref in $refs) {
            if (-not $spriteRefs.ContainsKey("$($ref.Groups[1].Value):$($ref.Groups[2].Value)")) { throw "$name has a missing sprite" }
        }
        $refs.Count
    }
    if ("$counts" -ne "$($expected[$name])") { throw "$name frame counts: $counts" }
}
foreach ($kind in @('Melee', 'Ranged')) {
    $prefab = Get-Content (Join-Path $root "Assets/Prefab/Unit/Base Unit/BaseUnit_${kind}.prefab") -Raw
    if ($prefab -notmatch 'Assembly-CSharp::UnitSpriteAnimator') { throw "Missing animator on $kind prefab" }
}
foreach ($name in @('Ernist', 'SimYoyeon', 'Zorba')) {
    $data = Get-Content (Join-Path $root "Assets/GameData/Units/${name}Data.asset") -Raw
    $animationMeta = Get-Content (Join-Path $root "Assets/Animations/Base Units/${name}Animations.asset.meta") -Raw
    $animationGuid = [regex]::Match($animationMeta, '(?m)^guid: (\w+)').Groups[1].Value
    $kind = if ($name -eq 'Zorba') { 'Ranged' } elseif ($name -eq 'SimYoyeon') { 'SimYoyeon' } else { 'Melee' }
    $prefabMeta = Get-Content (Join-Path $root "Assets/Prefab/Unit/Base Unit/BaseUnit_${kind}.prefab.meta") -Raw
    $prefabGuid = [regex]::Match($prefabMeta, '(?m)^guid: (\w+)').Groups[1].Value
    if ($data -notmatch "animationSet: .*guid: $animationGuid" -or $data -notmatch "unitPrefab: .*guid: $prefabGuid") {
        throw "$name data is not connected to its animation and prefab"
    }
}
$simPrefab = Get-Content (Join-Path $root 'Assets/Prefab/Unit/Base Unit/BaseUnit_SimYoyeon.prefab') -Raw
$simDataGuid = [regex]::Match((Get-Content (Join-Path $root 'Assets/GameData/Units/SimYoyeonData.asset.meta') -Raw), '(?m)^guid: (\w+)').Groups[1].Value
if ($simPrefab -notmatch "<Data>k__BackingField: .*guid: $simDataGuid" -or
    $simPrefab -notmatch 'm_Sprite: \{fileID: -3468114202860011654, guid: 996b08d726dbf4648aa6d0d097cfbf9a') {
    throw 'Sim Yoyeon prefab still uses Ernist visuals or data'
}
Write-Output 'PASS: five animation sets and both base unit prefabs'
