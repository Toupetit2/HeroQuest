using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Armor", menuName = "Scriptable Objects/Armor")]
public class Armor : Equipment
{
    public int defenseDice;
    public List<Armor> stacksWith;
}
