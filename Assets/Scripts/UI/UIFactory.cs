using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// petits elements d'interface (uGUI) crees par code, dans le style sombre et dore du jeu
// utilises par le menu pause et les fiches des heros : aucun prefab a creer dans la scene
public static class UIFactory
{
    public static readonly Color DarkBrown = new Color(0.14f, 0.08f, 0.05f, 0.97f);
    public static readonly Color ButtonBrown = new Color(0.30f, 0.17f, 0.09f);
    public static readonly Color ButtonHover = new Color(0.45f, 0.26f, 0.11f);
    public static readonly Color Ivory = new Color(0.95f, 0.88f, 0.70f);
    public static readonly Color Gold = new Color(1.00f, 0.82f, 0.30f);

    static Font defaultFont;

    public static Font DefaultFont
    {
        get
        {
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return defaultFont;
        }
    }

    // objet d'interface vide, ancre au centre de son parent
    public static RectTransform Create(string name, Transform parent, Vector2 size, Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        return rt;
    }

    // etire l'objet pour qu'il couvre tout son parent
    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // panneau colore, avec un liseré dore si border = vrai
    public static Image Panel(string name, Transform parent, Vector2 size, Vector2 position, Color color, bool border)
    {
        RectTransform rt = Create(name, parent, size, position);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        if (border)
        {
            Outline outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(3f, -3f);
        }
        return image;
    }

    public static Text Label(string name, Transform parent, string text, int fontSize, Color color, TextAnchor anchor, Vector2 size, Vector2 position, Font font = null)
    {
        RectTransform rt = Create(name, parent, size, position);
        Text label = rt.gameObject.AddComponent<Text>();
        label.font = font != null ? font : DefaultFont;
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = anchor;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        return label;
    }

    public static Button Button(string name, Transform parent, string text, int fontSize, Vector2 size, Vector2 position, UnityAction onClick, Font font = null)
    {
        Image image = Panel(name, parent, size, position, Color.white, true);
        Button button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonBrown;
        colors.highlightedColor = ButtonHover;
        colors.selectedColor = ButtonBrown;
        colors.pressedColor = Gold;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        Text label = Label("Text", image.transform, text, fontSize, Ivory, TextAnchor.MiddleCenter, size, Vector2.zero, font);
        Stretch(label.rectTransform);
        return button;
    }

    // canvas plein ecran, au-dessus de l'interface du jeu
    public static Canvas OverlayCanvas(string name, Transform parent, int sortingOrder)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }
}
