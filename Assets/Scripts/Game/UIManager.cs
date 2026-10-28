using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject heroChoice;

    void Awake()
    {
        heroChoice.SetActive(false);
    }

    public void ShowHeroChoice(bool state)
    {
        heroChoice.SetActive(state);
    }

    public void ChooseBarbarian()
    {
        heroChoice.SetActive(false);
        // Set Hero to Barbarian
    }

    public void ChooseDwarf()
    {
        heroChoice.SetActive(false);
        // Set Hero to Dwarf
    }

    public void ChooseElf()
    {
        heroChoice.SetActive(false);
        // Set Hero to Elf
    }

    public void ChooseWizard()
    {
        heroChoice.SetActive(false);
        // Set Hero to Wizard
    }
}