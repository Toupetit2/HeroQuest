# Installer sans passer par les menus de l'editeur Unity.
# A executer une seule fois a la racine du projet de la branche main.
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$gameManager = Join-Path $root "Assets\Scripts\Game\GameManager.cs"
if (!(Test-Path $gameManager)) {
    Write-Host "ERREUR : GameManager.cs introuvable dans le projet main." -ForegroundColor Red
    exit 1
}
$source = [System.IO.File]::ReadAllText($gameManager)
$backup = $gameManager + ".avant_runtime.bak"
if (!(Test-Path $backup)) { [System.IO.File]::Copy($gameManager, $backup) }
# L'import UnityEditor.Search dans main est inutile et interdit dans un player.
$source = [regex]::Replace($source, '(?m)^\s*using UnityEditor\.Search;\s*\r?\n', '')
$call = 'HeroQuestRuntimeTerrain.Configure(boardBuilder, questView);'
if (!$source.Contains($call)) {
    $needle = 'boardBuilder.Generate();'
    if (!$source.Contains($needle)) {
        Write-Host "ERREUR : structure GameManager inconnue. Aucun changement sauvegarde." -ForegroundColor Red
        exit 2
    }
    $insert = @'
if (boardBuilder == null) boardBuilder = FindFirstObjectByType<BoardBuilder>();
        if (questView == null) questView = FindFirstObjectByType<QuestView>();
        HeroQuestRuntimeTerrain.Configure(boardBuilder, questView);
        boardBuilder.Generate();
'@
    $source = $source.Replace($needle, $insert)
}
$utf8 = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($gameManager, $source, $utf8)
Write-Host "OK : GameManager de main adapte au terrain runtime." -ForegroundColor Green
Write-Host "Aucun menu Unity d'installation n'est necessaire." -ForegroundColor Green
Write-Host "Pour jouer sans Unity, compiler le projet une fois dans Build Profiles."
