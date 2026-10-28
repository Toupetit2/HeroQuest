#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Edition visuelle des objets d'une quete (les portes restent dans le JSON).
public class HQCampaignMapEditor : EditorWindow
{
    private readonly string[] names = {"Escalier", "Coffre", "Tombe", "Bibliotheque", "Table", "Table sorcier", "Etabli", "Trone", "Cheminee", "Placard", "Rack", "Rack armes", "Bloc 1", "Bloc 2", "Zombie", "Momie", "Gargouille", "Squelette", "Gobelin", "Orc", "Abomination", "Guerrier effroi", "Sorcier effroi", "Piege bloc", "Piege fosse", "Piege lance"};
    private readonly string[] groups = {"stairs", "furniture", "furniture", "furniture", "furniture", "furniture", "furniture", "furniture", "furniture", "furniture", "furniture", "furniture", "obstacle", "obstacle", "monster", "monster", "monster", "monster", "monster", "monster", "monster", "monster", "monster", "trap", "trap", "trap"};
    private readonly int[] ids = {5,13,14,16,17,19,20,21,22,23,24,25,15,18,5,6,7,8,9,10,11,12,27,7,8,9};
    private readonly Vector2Int[] dimensions = {
        new Vector2Int(2,2),new Vector2Int(1,1),new Vector2Int(3,2),new Vector2Int(3,1),new Vector2Int(3,2),new Vector2Int(3,2),new Vector2Int(3,2),new Vector2Int(1,1),new Vector2Int(3,1),new Vector2Int(3,1),new Vector2Int(3,2),new Vector2Int(3,1),new Vector2Int(1,1),new Vector2Int(2,1),
        new Vector2Int(1,1),new Vector2Int(1,1),new Vector2Int(1,1),new Vector2Int(1,1),new Vector2Int(1,1),new Vector2Int(1,1),new Vector2Int(1,1),new Vector2Int(1,1),new Vector2Int(1,1),new Vector2Int(1,1),new Vector2Int(1,1),new Vector2Int(1,1)
    };
    private int quest = 1, selection = 1, width = 1, height = 1, angle = 0;
    private HQCampaignQuestData data;
    private Texture2D map;
    private Vector2 scroll;
    private bool changed;

    [MenuItem("HeroQuest/Campagne/Editeur des placements TXT")]
    public static void Open() { var w=GetWindow<HQCampaignMapEditor>("Campagne HeroQuest"); w.minSize=new Vector2(630,610); w.Load(); }

    private void Load()
    {
        string p=Path.Combine(Application.dataPath,"Resources","Campaign",$"quest_{quest:00}.txt");
        data=File.Exists(p) ? JsonUtility.FromJson<HQCampaignQuestData>(File.ReadAllText(p)) : null;
        if (data != null && data.objects == null) data.objects=new HQCampaignObjectData[0];
        map=Resources.Load<Texture2D>($"CampaignMaps/quest_{quest:00}"); changed=false; Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Clic gauche sur la carte : ajouter un objet. Clic droit : enlever un objet. Sauvegarder en quittant. Les positions sont provisoires jusqu'a verification.", MessageType.Info);
        int q=EditorGUILayout.IntSlider("Quete",quest,1,14);
        if (q!=quest) { if (changed && !EditorUtility.DisplayDialog("Modifications","Abandonner les changements non sauvegardes ?","Oui","Non")) return; quest=q;Load(); }
        if (data==null) { EditorGUILayout.HelpBox("Fichier quest_XX.txt introuvable",MessageType.Error);return; }
        EditorGUILayout.LabelField(data.title + " : " + data.objects.Length + " objets dans le TXT");
        int next=EditorGUILayout.Popup("Ajouter",selection,names);
        if (next!=selection) {selection=next;width=dimensions[next].x;height=dimensions[next].y;}
        width=EditorGUILayout.IntSlider("Largeur (cases)",width,1,5);
        height=EditorGUILayout.IntSlider("Hauteur (cases)",height,1,5);
        angle=EditorGUILayout.IntPopup("Rotation",angle,new[]{"0", "90", "180", "270"},new[]{0,90,180,270});
        GUILayout.BeginHorizontal();
        if(GUILayout.Button("Sauvegarder TXT")) Save();
        if(GUILayout.Button("Recharger")) Load();
        GUILayout.EndHorizontal();
        scroll=EditorGUILayout.BeginScrollView(scroll);
        if(map!=null)
        {
            Rect r=GUILayoutUtility.GetRect(566f,416f,GUILayout.ExpandWidth(false));
            GUI.DrawTexture(r,map,ScaleMode.StretchToFill);
            foreach(var obj in data.objects)
            {
                if(obj==null)continue;
                Rect box=new Rect(r.x+5+obj.x*21.48f,r.y+4+obj.y*21.45f,obj.width*21.48f,obj.height*21.45f);
                Color color=obj.group=="monster" ? new Color(0.9f,.1f,.1f,.27f) : obj.group=="stairs" ? new Color(.1f,.9f,.1f,.3f) : new Color(1,.7f,.05f,.28f);
                EditorGUI.DrawRect(box,color);
                GUI.Label(new Rect(box.x,box.y,24,18),obj.id.ToString(),EditorStyles.miniLabel);
            }
            Event e=Event.current;
            if(e.type==EventType.MouseDown && r.Contains(e.mousePosition))
            {
                int x=Mathf.FloorToInt((e.mousePosition.x-r.x-5)/21.48f);
                int y=Mathf.FloorToInt((e.mousePosition.y-r.y-4)/21.45f);
                if(x>=0&&y>=0&&x<26&&y<19)
                {
                    var items=new List<HQCampaignObjectData>(data.objects);
                    if(e.button==1)
                    {
                        for(int i=items.Count-1;i>=0;i--)
                        {var o=items[i];if(x>=o.x&&x<o.x+o.width&&y>=o.y&&y<o.y+o.height){items.RemoveAt(i);break;}}
                    }
                    else if(e.button==0&&x+width<=26&&y+height<=19)
                        items.Add(new HQCampaignObjectData{group=groups[selection],id=ids[selection],x=x,y=y,width=width,height=height,angle=angle});
                    data.objects=items.ToArray();changed=true;Repaint();e.Use();
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void Save()
    {
        if(data==null)return;
        data.transcriptionVerified=false;
        string p=Path.Combine(Application.dataPath,"Resources","Campaign",$"quest_{quest:00}.txt");
        File.WriteAllText(p,JsonUtility.ToJson(data,true));
        AssetDatabase.Refresh(); changed=false;
        Debug.Log("Quete "+quest+" sauvegardee dans "+p+" (a verifier dans le Quest Book)");
    }
}
#endif
