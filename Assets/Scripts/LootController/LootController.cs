using System.Collections.Generic;
using System;
using UnityEngine;

public class LootController
{
    private GameManager gameManager;
    private List<Tuple<Equipment, Vector2Int>> droppedEquipment = new List<Tuple<Equipment, Vector2Int>>();

    public LootController(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void DropLoot(Equipment equipment, Vector2Int position)
    {
        droppedEquipment.Add(new Tuple<Equipment, Vector2Int>(equipment, position));
    }

    public void PickUpLoot(Hero hero, Vector2Int position)
    {
        // a l'envers : on retire du sol ce qui est ramasse
        for (int i = droppedEquipment.Count - 1; i >= 0; i--)
        {
            Tuple<Equipment, Vector2Int> equipment = droppedEquipment[i];
            if (equipment.Item2 == position)
            {
                hero.AddEquipment(equipment.Item1);
                droppedEquipment.RemoveAt(i);
            }
        }
    }
}