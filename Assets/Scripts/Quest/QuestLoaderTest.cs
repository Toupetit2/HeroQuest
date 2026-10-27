using System.Collections.Generic;
using UnityEngine;

// outil de test : charge une quete et affiche le resultat dans la Console, sans rien poser dans la scene
public class QuestLoaderTest : MonoBehaviour
{
    public TextAsset boardFile;
    public TextAsset questFile;
    public List<MonsterData> monsterDatas;

    [ContextMenu("Tester la quete")]
    public void Test()
    {
        GameState gameState = new GameState();
        gameState.map = new Map(boardFile.text);

        QuestLoader loader = new QuestLoader(gameState, monsterDatas);
        loader.Load(questFile.text);

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

        Debug.Log("Victoire : " + gameState.victory.mode + ", " + gameState.victory.objectives.Count
            + " objectifs, monstre errant : " + gameState.wanderingMonster);
    }
}
