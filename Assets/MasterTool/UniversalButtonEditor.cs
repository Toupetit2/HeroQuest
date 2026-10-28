#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CanEditMultipleObjects]
[CustomEditor(typeof(Object), true)]
public class UniversalButtonEditor : Editor
{
    private readonly Dictionary<string, object[]> methodParams = new();
    static readonly Type[] unityTypesToIgnore = new Type[]
    {
        typeof(Transform), typeof(RectTransform), typeof(Camera), typeof(Light), typeof(MeshFilter),
        typeof(MeshRenderer), typeof(SkinnedMeshRenderer), typeof(Canvas), typeof(CanvasRenderer),
        typeof(SpriteRenderer), typeof(Animator), typeof(AudioSource), typeof(AudioListener)
        // Ajouter ici d'autres types Unity de base à ignorer si besoin
    };

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var targetObject = target;
        var type = targetObject.GetType();

        // Ignore les types Unity de base pour eviter de polluer l'UI
        foreach (var t in unityTypesToIgnore)
            if (t == type) return;

        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);


        foreach (var method in methods)
        {
            var attrs = method.GetCustomAttributes(typeof(ButtonAttribute), true);
            var parameters = method.GetParameters();
            if (attrs.Length > 0)
            {
                string methodKey = $"{method.DeclaringType.FullName}.{method.Name}(" +
                   string.Join(",", method.GetParameters()
                       .Select(p => p.ParameterType.FullName)) + ")";
                // Initialisation du tableau de valeurs si besoin
                if (!methodParams.ContainsKey(methodKey))
                    methodParams[methodKey] = new object[parameters.Length];

                // Affiche les champs pour chaque parametre
                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    object value = methodParams[methodKey][i];

                    // Affiche un champ selon le type du parametre
                    if (param.ParameterType == typeof(int))
                        value = EditorGUILayout.IntField(param.Name, value != null ? (int)value : 0);
                    else if (param.ParameterType == typeof(float))
                        value = EditorGUILayout.FloatField(param.Name, value != null ? (float)value : 0f);
                    else if (param.ParameterType == typeof(string))
                        value = EditorGUILayout.TextField(param.Name, value != null ? (string)value : "");
                    else if (param.ParameterType == typeof(bool))
                        value = EditorGUILayout.Toggle(param.Name, value != null ? (bool)value : false);
                    else if (typeof(UnityEngine.Object).IsAssignableFrom(param.ParameterType))
                        value = EditorGUILayout.ObjectField(param.Name, value as UnityEngine.Object, param.ParameterType, true);
                    else
                        EditorGUILayout.LabelField(param.Name, $"Type non supporte: {param.ParameterType.Name}");

                    methodParams[methodKey][i] = value;
                }

                // Affiche le bouton
                if (GUILayout.Button(method.Name))
                {
                    try
                    {
                        method.Invoke(targetObject, methodParams[methodKey]);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Erreur lors de l'appel de {method.Name}: {e}");
                    }
                }
            }
        }

    }
}
#endif