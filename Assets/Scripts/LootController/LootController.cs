using System.Collections.Generic;
using System;
using UnityEngine;

public class LootController
{
    private GameManager gameManager;
    private List<Tuple<Equipment, Vector2Int>> droppedEquipment;

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
        foreach(Tuple<Equipment, Vector2Int> equipment in droppedEquipment) 
        {
            if (equipment.Item2 == position) 
            {
                hero.AddEquipment(equipment.Item1);
            }
        }
    }
}