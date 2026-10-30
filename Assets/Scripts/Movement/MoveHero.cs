using System.Collections.Generic;
using UnityEngine;

public class MoveHero
{
    private Object heroPrefab;
    private List<GameObject> heroObjectList = new List<GameObject>();
    private float groundLevel = 0;

    public MoveHero(Object _heroPrefab)
    {
        heroPrefab = _heroPrefab;
    }

    public void Init(List<Hero> heroList)
    {
        heroObjectList.Clear();
        foreach (Hero hero in heroList)
        {
            GameObject heroObject = Object.Instantiate(heroPrefab, new Vector3(hero.position.x, groundLevel, hero.position.y), Quaternion.identity) as GameObject;
            heroObjectList.Add(heroObject);
        }
    }

    public void UpdatePosition(List<Hero> heroList)
    {
        for (int i = 0; i < heroList.Count && i < heroObjectList.Count; i++)
        {
            Hero hero = heroList[i];
            GameObject heroObject = heroObjectList[i];
            if (heroObject == null) continue;

            heroObject.transform.position = new Vector3(hero.position.x, groundLevel, hero.position.y);
            heroObject.SetActive(!hero.isDead);   // un heros mort quitte le plateau
        }
    }
}
