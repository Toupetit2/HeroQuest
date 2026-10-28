using System.Collections.Generic;
using UnityEngine;

// outil de test : genere le plateau, charge une quete et l'affiche
// sans BoardBuilder ni QuestView, affiche seulement le resultat dans la Console
public class QuestLoaderTest : MonoBehaviour
{
    public BoardBuilder board;      // optionnel : genere le plateau dans la scene
    public QuestView view;          // optionnel : affiche la quete dans la scene
    public TextAsset boardFile;     // utilise seulement sans BoardBuilder
    public TextAsset questFile;
    public List<MonsterData> monsterDatas;

    [ContextMenu("Tester la quete")]
    public void Test()
    {
        GameState gameState = new GameState();
        if (board != null)
        {
            board.Generate();
            gameState.map = board.map;
        }
        else gameState.map = new Map(boardFile.text);

        QuestLoader loader = new QuestLoader(gameState, monsterDatas);
        loader.Load(questFile.text);

        if (view != null) view.Show(gameState);

        // plateau en texte : # roche, X bloquee, M meuble, * visible (salle de depart), . cachee
        Map map = gameState.map;
        string text = "";
        for (int y = 0; y < map.height; y++)
        {
            for (int x = 0; x < map.width; x++)
            {
                Tile t = map.Get(x, y);
                if (t.rock) text += "#";
                else if (t.blocked) text += "X";
                else if (t.furniture != null) text += "M";
                else if (t.revealed) text += "*";
                else text += ".";
            }
            text += "\n";
        }
        Debug.Log(text);

        string wandering = gameState.hasWanderingMonster ? gameState.wanderingMonster.ToString() : "aucun";
        Debug.Log("Victoire : " + gameState.victory.mode + ", " + gameState.victory.objectives.Count
            + " objectifs, monstre errant : " + wandering);

        foreach (Treasure t in gameState.treasureList)
            Debug.Log("Tresor " + t.room + " (" + t.x + ", " + t.y + ") : " + t.gold + " or, objet : "
                + (string.IsNullOrEmpty(t.item) ? "aucun" : t.item) + (t.isTrapped ? ", piege -" + t.trapDamage : ""));
    }
}
