#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CharacterStatWindowBuilder
{
    private const string PanelPath = "Assets/UI/character_stat_main_panel_empty.png";
    private const string KnightPath = "Assets/_Project/Art/UI/Knight.png";
    private const string ButtonPath = "Assets/_Project/Art/UI/ChatGPT Image 2026년 4월 29일 오후 02_14_39.png";
    private const string BarPath = "Assets/_Project/Art/UI/Bar.png";
    private const string FontPath = "Assets/_Project/Art/Font/CookieRun Bold SDF.asset";

    [MenuItem("Tools/New Game/Rebuild Character Stat Window")]
    public static void Rebuild()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[CharacterStatWindowBuilder] Canvas not found.");
            return;
        }

        Transform statRoot = canvas.transform.Find("Stat UI");
        if (statRoot == null)
        {
            GameObject statRootObject = new GameObject("Stat UI", typeof(RectTransform));
            SetRect(statRootObject, canvas.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            statRoot = statRootObject.transform;
        }

        Transform oldWindow = statRoot.Find("Character Stat Window");
        if (oldWindow != null)
        {
            Object.DestroyImmediate(oldWindow.gameObject);
        }

        Sprite panelSprite = LoadSprite(PanelPath);
        Sprite knightSprite = LoadSprite(KnightPath);
        Sprite buttonSprite = LoadSprite(ButtonPath);
        Sprite barSprite = LoadSprite(BarPath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        GameObject window = CreateImage("Character Stat Window", statRoot, panelSprite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(720f, 1080f), Color.white, true);
        Image background = window.GetComponent<Image>();
        background.type = Image.Type.Sliced;
        CharacterStatWindowUI ui = window.AddComponent<CharacterStatWindowUI>();

        TMP_Text title = CreateText("Title Text", window.transform, font, "캐릭터 스탯", 42, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -71f), new Vector2(360f, 70f), Color.white);
        GameObject portrait = CreateImage("Character Portrait", window.transform, knightSprite, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(150f, -205f), new Vector2(120f, 140f), Color.white, false);

        Color gold = new Color(1f, 0.78f, 0.13f, 1f);
        Color cream = new Color(0.96f, 0.88f, 0.72f, 1f);
        TMP_Text level = CreateText("Level Text", window.transform, font, "LV : 00", 30, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(250f, -155f), new Vector2(180f, 50f), gold);
        TMP_Text classText = CreateText("Class Text", window.transform, font, "기사", 30, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(250f, -205f), new Vector2(160f, 50f), Color.white);

        Image hpFill = CreateBar("Health", window.transform, barSprite, new Vector2(250f, -260f), new Color(0.67f, 0.05f, 0.04f, 1f));
        Image mpFill = CreateBar("Mana", window.transform, barSprite, new Vector2(250f, -300f), new Color(0.03f, 0.18f, 0.5f, 1f));
        TMP_Text hpText = CreateText("Health Value", window.transform, font, "0 / 0", 16, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(360f, -260f), new Vector2(170f, 26f), Color.white);
        TMP_Text mpText = CreateText("Mana Value", window.transform, font, "0 / 0", 16, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(360f, -300f), new Vector2(170f, 26f), Color.white);

        GameObject combatBox = CreateImage("Combat Power Box", window.transform, null, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-80f, -250f), new Vector2(245f, 74f), new Color(0.18f, 0.16f, 0.11f, 0.78f), false);
        Outline outline = combatBox.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.62f, 0.28f, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);
        TMP_Text combatText = CreateText("Combat Power Text", combatBox.transform, font, "전투력 : 00000", 28, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
        Stretch(combatText.rectTransform);

        string[] names = { "공격력", "공격속도", "체력", "치명타 확률", "치명타 데미지", "골드 획득량", "경험치 획득량" };
        StatType[] types = { StatType.Attack, StatType.AttackSpeed, StatType.Health, StatType.CritChance, StatType.CritDamage, StatType.GoldBonus, StatType.ExpBonus };
        TMP_Text[] nameTexts = new TMP_Text[names.Length];
        TMP_Text[] valueTexts = new TMP_Text[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            float y = -385f - i * 48f;
            nameTexts[i] = CreateText(names[i] + " Name", window.transform, font, names[i], 24, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(245f, y), new Vector2(220f, 42f), cream);
            valueTexts[i] = CreateText(names[i] + " Value", window.transform, font, "000", 24, TextAlignmentOptions.Right, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, 0.5f), new Vector2(545f, y), new Vector2(140f, 42f), Color.white);
        }

        GameObject confirm = CreateImage("Confirm Button", window.transform, buttonSprite, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(220f, 92f), Color.white, true);
        Button confirmButton = confirm.AddComponent<Button>();
        TMP_Text confirmText = CreateText("Confirm Text", confirm.transform, font, "확인", 36, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
        Stretch(confirmText.rectTransform);

        SerializedObject serializedUi = new SerializedObject(ui);
        serializedUi.FindProperty("windowRoot").objectReferenceValue = window;
        serializedUi.FindProperty("closeButton").objectReferenceValue = confirmButton;
        serializedUi.FindProperty("titleText").objectReferenceValue = title;
        serializedUi.FindProperty("levelText").objectReferenceValue = level;
        serializedUi.FindProperty("classText").objectReferenceValue = classText;
        serializedUi.FindProperty("portraitImage").objectReferenceValue = portrait.GetComponent<Image>();
        serializedUi.FindProperty("portraitSprite").objectReferenceValue = knightSprite;
        serializedUi.FindProperty("healthText").objectReferenceValue = hpText;
        serializedUi.FindProperty("manaText").objectReferenceValue = mpText;
        serializedUi.FindProperty("healthFillImage").objectReferenceValue = hpFill;
        serializedUi.FindProperty("manaFillImage").objectReferenceValue = mpFill;
        serializedUi.FindProperty("combatPowerText").objectReferenceValue = combatText;

        SerializedProperty statLines = serializedUi.FindProperty("statLines");
        statLines.arraySize = types.Length;
        for (int i = 0; i < types.Length; i++)
        {
            SerializedProperty line = statLines.GetArrayElementAtIndex(i);
            line.FindPropertyRelative("statType").enumValueIndex = (int)types[i];
            line.FindPropertyRelative("nameText").objectReferenceValue = nameTexts[i];
            line.FindPropertyRelative("valueText").objectReferenceValue = valueTexts[i];
        }

        serializedUi.ApplyModifiedPropertiesWithoutUndo();
        WireButtons(statRoot, window);

        window.SetActive(false);
        EditorUtility.SetDirty(window);
        EditorUtility.SetDirty(statRoot.gameObject);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[CharacterStatWindowBuilder] Rebuilt Character Stat Window.");
    }

    private static void WireButtons(Transform statRoot, GameObject window)
    {
        Transform statButton = statRoot.Find("Stat Button");
        if (statButton != null)
        {
            TogglePanelButton statToggle = statButton.GetComponent<TogglePanelButton>();
            Transform statScrollView = statButton.Find("Scroll View");
            if (statToggle != null && statScrollView != null)
            {
                SerializedObject serializedStatToggle = new SerializedObject(statToggle);
                serializedStatToggle.FindProperty("targetPanel").objectReferenceValue = statScrollView.gameObject;
                serializedStatToggle.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(statToggle);
            }
        }

        Transform playerButton = GameObject.Find("Player Button")?.transform;
        if (playerButton == null)
        {
            return;
        }

        TogglePanelButton playerToggle = playerButton.GetComponent<TogglePanelButton>();
        if (playerToggle == null)
        {
            playerToggle = playerButton.gameObject.AddComponent<TogglePanelButton>();
        }

        SerializedObject serializedPlayerToggle = new SerializedObject(playerToggle);
        serializedPlayerToggle.FindProperty("targetPanel").objectReferenceValue = window;
        serializedPlayerToggle.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(playerToggle);
    }

    private static Sprite LoadSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject CreateImage(string name, Transform parent, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size, Color color, bool raycast)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        SetRect(go, parent, anchorMin, anchorMax, pivot, position, size);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycast;
        image.preserveAspect = sprite != null;
        return go;
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string text, int size, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 rectSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        SetRect(go, parent, anchorMin, anchorMax, pivot, position, rectSize);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        if (font != null)
        {
            tmp.font = font;
        }

        return tmp;
    }

    private static Image CreateBar(string name, Transform parent, Sprite frameSprite, Vector2 position, Color fillColor)
    {
        GameObject frame = CreateImage(name + " Frame", parent, frameSprite, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), position, new Vector2(220f, 28f), Color.white, false);
        GameObject fill = CreateImage(name + " Fill", frame.transform, null, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(164f, 14f), fillColor, false);
        Image image = fill.GetComponent<Image>();
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
        return image;
    }

    private static RectTransform SetRect(GameObject go, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        go.transform.SetParent(parent, false);
        RectTransform rectTransform = go.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        return rectTransform;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
#endif
