using UnityEngine;

public class EquipmentController
{
    private GameManager gameManager;
    private EquipmentControllerStates controllerState;
    public EquipmentController(GameManager gameManager)
    {
        this.gameManager = gameManager;
        controllerState = EquipmentControllerStates.None;
    }

    public void StartEquipmentUse() 
    {
        controllerState = EquipmentControllerStates.ChoosingTarget;
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
