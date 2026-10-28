using UnityEngine;

public class EquipmentController
{
    private GameManager gameManager;
    public EquipmentController(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void StartEquipmentUse()
    {
        return;
    }

    public void ConfirmEquipmentUse(Equipment equipment)
    {
        equipment.effect.Execute(gameManager);

        if (equipment.singleUse)
        {
            gameManager.GetCurrentHero().inventory.Remove(equipment);
        }
    }
}