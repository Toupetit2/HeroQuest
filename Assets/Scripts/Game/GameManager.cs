using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class GameManager : MonoBehaviour
{
    [Header("Quête")]
    public int questNumber = 1;

    [Header("État de la partie")]
    public int currentTurn = 1;
    public bool isGameStarted = false;
    public GameState gameState;

    public int currentPlayerIndex = 0;

    [Header("État du tour")]    
    public bool hasMoveAction = true;
    public bool hasAction = true;

    private Map map;

    [Button]
    public void Test() 
    {
        return;
    }

    private void Start()
    {
        // Generate Map
        map = new Map(
                ". . . .\n" +
                ". . . .\n" +
                ". . . ."
            );

    // Show Intro UI -> Hero choice UI -> Spell choice UI

}

    private void Update()
    {
        if (!isGameStarted) 
        {
            // Choix position escalier

            isGameStarted = true;
        }

        if (gameState.gameStatus != GameStatus.Playing) 
        {
            return;
        }
    }

    public void StartTurn() 
    {
        hasMoveAction = true;
        hasAction = true;
    }

    public void EndTurn()
    {
        currentPlayerIndex++;

        if (currentPlayerIndex >= gameState.heroList.Count)
        {
            currentPlayerIndex = 0;
            currentTurn++;
        }
        StartTurn();
    }

}

