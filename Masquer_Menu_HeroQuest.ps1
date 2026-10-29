# Masque les commandes "HeroQuest" du menu Unity en conservant les scripts.
# Ne modifie que des attributs [MenuItem("HeroQuest/... ")] dans des dossiers Editor.
# Sauvegarde les fichiers d'origine hors du dossier Assets avant toute modification.
$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$assets = Join-Path $projectRoot "Assets"

if (-not (Test-Path -LiteralPath $assets -PathType Container)) {
    Write-Host "ERREUR : aucun dossier Assets a cote de ce script." -ForegroundColor Red
    Write-Host "Placez les fichiers BAT et PS1 a la racine du projet Unity puis relancez."
    exit 1
}

$backupFolder = "Sauvegarde_Menu_HeroQuest_" + (Get-Date -Format "yyyyMMdd_HHmmss")
$backupRoot = Join-Path $projectRoot $backupFolder
$pattern = [regex]'(?m)^[ \t]*\[MenuItem\s*\(\s*"HeroQuest(?:/[^"\r\n]*)?"[^\r\n]*\)\s*\][ \t]*(?:\r?\n|$)'
$changedFiles = 0
$hiddenEntries = 0

try {
    Get-ChildItem -LiteralPath $assets -Recurse -File -Filter '*.cs' | ForEach-Object {
        $file = $_
        # Les attributs du menu de l'Editeur doivent se trouver dans un dossier Editor.
        if ($file.FullName -notmatch '[\\/]Editor[\\/]') { return }
        $source = [System.IO.File]::ReadAllText($file.FullName)
        $matches = $pattern.Matches($source)
        if ($matches.Count -eq 0) { return }

        $relative = $file.FullName.Substring($projectRoot.Length).TrimStart([char[]]'\\/')
        $backupPath = Join-Path $backupRoot $relative
        $backupDir = Split-Path -Parent $backupPath
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $backupPath -Force

        $cleaned = $pattern.Replace($source, '')
        [System.IO.File]::WriteAllText($file.FullName, $cleaned,
            (New-Object System.Text.UTF8Encoding($false)))
        $script:changedFiles++
        $script:hiddenEntries += $matches.Count
        Write-Host ("Masque : " + $relative + " (" + $matches.Count + " entree(s))")
    }
    if ($changedFiles -eq 0) {
        Write-Host "Aucune entree de menu HeroQuest trouvee dans les scripts Editor."
        Write-Host "Le menu est peut-etre deja masque ou vient d'un package externe."
    } else {
        Write-Host ""
        Write-Host ("Termine : " + $hiddenEntries + " entree(s) de menu masquees dans " + $changedFiles + " script(s).") -ForegroundColor Green
        Write-Host ("Sauvegarde des originaux : " + $backupFolder)
        Write-Host "Rouvrez Unity ou attendez la recompilation des scripts."
        Write-Host "Aucun GameManager, prefab, scene ni asset 3D n'a ete modifie."
    }
} catch {
    Write-Host ("ERREUR : " + $_.Exception.Message) -ForegroundColor Red
    Write-Host "Les originaux deja copies se trouvent dans : $backupFolder"
    exit 1
}
