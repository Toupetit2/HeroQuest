using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// affichage de ZoneStyle dans l'Inspector : le champ "zone" devient une liste deroulante
// remplie avec les zones lues dans le board.txt du BoardBuilder
[CustomPropertyDrawer(typeof(ZoneStyle))]
public class ZoneStyleDrawer : PropertyDrawer
{
    // 3 lignes : zone, prefab, materiau
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        float step = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        SerializedProperty zoneProp = property.FindPropertyRelative("zone");
        List<string> zones = GetZones(property);

        if (zones == null)
        {
            // pas de board.txt : on garde un champ texte
            EditorGUI.PropertyField(line, zoneProp);
        }
        else
        {
            // noms affiches : "." devient "Couloir"
            string[] names = new string[zones.Count];
            for (int i = 0; i < zones.Count; i++)
                names[i] = zones[i] == "." ? "Couloir" : zones[i];

            int index = zones.IndexOf(zoneProp.stringValue);
            int newIndex = EditorGUI.Popup(line, "Zone", index, names);
            if (newIndex >= 0) zoneProp.stringValue = zones[newIndex];
        }

        line.y += step;
        EditorGUI.PropertyField(line, property.FindPropertyRelative("floorPrefab"));
        line.y += step;
        EditorGUI.PropertyField(line, property.FindPropertyRelative("material"));
    }

    // zones du plateau, dans l'ordre du fichier, sans doublon
    List<string> GetZones(SerializedProperty property)
    {
        BoardBuilder builder = property.serializedObject.targetObject as BoardBuilder;
        if (builder == null || builder.boardFile == null) return null;

        Map map;
        try { map = new Map(builder.boardFile.text); }
        catch { return null; } // fichier mal forme : l'erreur s'affichera au Generate

        List<string> zones = new List<string>();
        for (int y = 0; y < map.height; y++)
        {
            for (int x = 0; x < map.width; x++)
            {
                string z = map.Get(x, y).zone;
                if (!zones.Contains(z)) zones.Add(z);
            }
        }
        return zones;
    }
}
