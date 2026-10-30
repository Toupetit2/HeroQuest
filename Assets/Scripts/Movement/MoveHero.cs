using System.Collections.Generic;
using UnityEngine;

public class MoveHero
{
    private Object heroPrefab;
    private List<GameObject> heroObjectList; 
    private float groundLevel = 0;

    public MoveHero(Object _heroPrefab) 
    {
        heroPrefab = _heroPrefab;
    }

    public void Init(List<Hero> heroList)
    {
        foreach (Hero hero in heroList) 
        {
            Object.Instantiate(heroPrefab, new Vector3(hero.position.x, groundLevel, hero.position.y), Quaternion.identity);
        }
    }

    public void UpdatePosition(List<Hero> heroList) 
    {
        for (int i = 0; i < heroList.Count; i++) 
        {
            Hero hero = heroList[i];
            GameObject heroObject = heroObjectList[i];
            heroObject.transform.position = new Vector3(hero.position.x, groundLevel, hero.position.y);
        }
    }
}
