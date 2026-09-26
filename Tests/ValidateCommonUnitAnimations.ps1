$root = Split-Path -Parent $PSScriptRoot
$imageDir = Join-Path $root 'Assets/Images/Animations/Common Units'
$animationDir = Join-Path $root 'Assets/Animations/Common Units'
$dataDir = Join-Path $root 'Assets/GameData/Units'

foreach ($kind in @('Melee', 'Ranged')) {
    $prefix = if ($kind -eq 'Melee') { 'ShortDis' } else { 'LongDis' }
    $asset = Get-Content (Join-Path $animationDir "Common${kind}Animations.asset") -Raw
    if ($asset -notmatch 'guid: 39ca6dd4eca7431465561b1aff859383') { throw "$kind animation script missing" }
    foreach ($pair in @(@('idle', 'idle'), @('walk', 'walk'), @('attack', 'attack'), @('down', 'dead'))) {
        $section = $pair[0]
        $sheet = $pair[1]
        $meta = Get-Content (Join-Path $imageDir "commonUnit_${prefix}_${sheet}.png.meta") -Raw
        $guid = [regex]::Match($meta, '(?m)^guid: (\w+)').Groups[1].Value
        if ($section -eq 'idle') {
            if ($asset -notmatch "idle: \{fileID: 21300000, guid: $guid") { throw "$kind idle missing" }
            continue
        }
        $block = [regex]::Match($asset, "(?ms)^  $section`:\r?\n.*?(?=^  (?:walk|attack|down|framesPerSecond):|\z)").Value
        $ids = [regex]::Matches($block, 'fileID: (-?\d+), guid: (\w+)')
        if ($ids.Count -ne 3) { throw "$kind $section needs 3 frames" }
        foreach ($id in $ids) {
            if ($id.Groups[2].Value -ne $guid -or $meta -notmatch "internalID: $($id.Groups[1].Value)(?:\r?\n|$)") {
                throw "$kind $section references a missing sprite"
            }
        }
    }
    $data = Get-Content (Join-Path $dataDir "Ally${kind}Data.asset") -Raw
    $animationGuid = [regex]::Match((Get-Content (Join-Path $animationDir "Common${kind}Animations.asset.meta") -Raw), '(?m)^guid: (\w+)').Groups[1].Value
    if ($data -notmatch "animationSet: .*guid: $animationGuid" -or $data -notmatch 'unitPrefab: .*guid: db1652fdef115264384fcccbf5ec8f1b') {
        throw "$kind data is not connected"
    }
    if ($data -notmatch '(?m)^  isBasicUnit: 0$') { throw "$kind generic unit must be removed after death" }
}

foreach ($name in @('Ernist', 'SimYoyeon', 'Zorba')) {
    $data = Get-Content (Join-Path $dataDir "${name}Data.asset") -Raw
    if ($data -notmatch '(?m)^  isBasicUnit: 1$') { throw "$name base unit must remain after death" }
}

$prefab = Get-Content (Join-Path $root 'Assets/Prefab/Unit/Unit_Ally.prefab') -Raw
$scene = Get-Content (Join-Path $root 'Assets/Scenes/InGame.unity') -Raw
if ($prefab -notmatch 'Assembly-CSharp::UnitSpriteAnimator') { throw 'Generic ally prefab has no animator' }
if ([regex]::Matches($scene, '(?m)^  m_SourcePrefab: .*guid: db1652fdef115264384fcccbf5ec8f1b').Count -ne 3) { throw 'Expected 3 generic allied units in scene' }
if ([regex]::Matches($scene, '(?m)^      objectReference: \{fileID: 11400000, guid: a8bafde668674d98b21bcc703cc2238c').Count -ne 1) { throw 'Expected one ranged allied unit in scene' }
Write-Output 'PASS: common animations and 2 melee + 1 ranged allies'
