using System.Collections.Generic;
using UnityEngine;

public class MoveHero
{
    private Object heroPrefab;
    private BoardBuilder board;     // pour convertir une case du plateau en position dans la scene
    private List<GameObject> heroObjectList = new List<GameObject>();

    public MoveHero(Object _heroPrefab, BoardBuilder _board)
    {
        heroPrefab = _heroPrefab;
        board = _board;
    }

    public void Init(List<Hero> heroList)
    {
        heroObjectList.Clear();
        foreach (Hero hero in heroList)
        {
            GameObject heroObject = Object.Instantiate(heroPrefab, CellPosition(hero.position), Quaternion.identity) as GameObject;
            if (heroObject != null)
            {
                heroObject.name = "Hero_" + hero.data.heroClass;   // reconnaissable dans la Hierarchy
                Tint(heroObject, HeroColors.Of(hero.data.heroClass));
            }
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

            heroObject.transform.position = CellPosition(hero.position);
            heroObject.SetActive(!hero.isDead);   // un heros mort quitte le plateau
        }
    }

    // teinte le heros a sa couleur : tous les heros partagent le meme prefab, la couleur les distingue
    // renderer.material cree une copie du materiau pour ce heros seulement (le prefab n'est pas modifie)
    private void Tint(GameObject heroObject, Color color)
    {
        foreach (Renderer r in heroObject.GetComponentsInChildren<Renderer>())
            r.material.color = color;
    }

    // centre de la case au niveau du sol (une case = cellSize unites, le y du plateau va vers -Z)
    private Vector3 CellPosition(Vector2Int cell)
    {
        if (board == null) return new Vector3(cell.x, 0f, cell.y);
        return board.CellToWorld(cell.x, cell.y);
    }
}
