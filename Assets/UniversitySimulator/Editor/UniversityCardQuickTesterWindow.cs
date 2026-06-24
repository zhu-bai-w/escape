using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UniversityCardQuickTesterWindow : EditorWindow
{
    const string WindowTitle = "Card Quick Tester";

    static readonly string[] CardPrefabFolders =
    {
        "Assets/UniversitySimulator/Prefabs/Cards",
        "Assets/Kings/cards"
    };

    readonly List<CardEntry> cards = new List<CardEntry>();
    readonly Dictionary<string, CardEntry> cardsByKey = new Dictionary<string, CardEntry>();

    CardStack cardStack;
    CardStack cachedCardStack;
    Vector2 scrollPosition;
    string searchText = "";

    [MenuItem("University Simulator/Cards/Card Quick Tester")]
    public static void Open()
    {
        UniversityCardQuickTesterWindow window = GetWindow<UniversityCardQuickTesterWindow>(WindowTitle);
        window.minSize = new Vector2(560f, 420f);
        window.RefreshCards();
    }

    void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        RefreshCards();
    }

    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    void OnFocus()
    {
        if (cardStack == null) {
            cardStack = FindCardStack();
        }

        if (cards.Count == 0 || cachedCardStack != cardStack) {
            RefreshCards();
        }
    }

    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        cardStack = FindCardStack();
        RefreshCards();
        Repaint();
    }

    void OnGUI()
    {
        DrawToolbar();

        if (cardStack == null) {
            EditorGUILayout.HelpBox("No CardStack was found in the active scene. Open the Game scene, or assign the Card Stack field manually.", MessageType.Warning);
        }

        if (EditorApplication.isPlaying == false) {
            EditorGUILayout.HelpBox("Enter Play Mode first. Then click Show on any card to replace the current card immediately.", MessageType.Info);
        }

        DrawCurrentCard();
        DrawCardList();
    }

    void DrawToolbar()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(WindowTitle, EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        cardStack = EditorGUILayout.ObjectField("Card Stack", cardStack, typeof(CardStack), true) as CardStack;
        if (EditorGUI.EndChangeCheck()) {
            RefreshCards();
        }

        EditorGUILayout.BeginHorizontal();
        searchText = EditorGUILayout.TextField("Search", searchText);
        if (GUILayout.Button("Clear", GUILayout.Width(60f))) {
            searchText = "";
            GUI.FocusControl(null);
        }
        if (GUILayout.Button("Refresh", GUILayout.Width(80f))) {
            RefreshCards();
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawCurrentCard()
    {
        if (cardStack == null) {
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.ObjectField("Current Card", cardStack.spawnedCard, typeof(GameObject), true);

        using (new EditorGUI.DisabledScope(cardStack.spawnedCard == null)) {
            if (GUILayout.Button("Select", GUILayout.Width(70f))) {
                Selection.activeGameObject = cardStack.spawnedCard;
                EditorGUIUtility.PingObject(cardStack.spawnedCard);
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawCardList()
    {
        int visibleCount = 0;
        foreach (CardEntry card in cards) {
            if (MatchesSearch(card)) {
                visibleCount++;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Cards: " + visibleCount + " / " + cards.Count, EditorStyles.miniBoldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (CardEntry card in cards) {
            if (MatchesSearch(card) == false) {
                continue;
            }

            DrawCardRow(card);
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawCardRow(CardEntry card)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.ObjectField(card.prefab, typeof(GameObject), false);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying == false || cardStack == null)) {
            if (GUILayout.Button("Show", GUILayout.Width(70f))) {
                ShowCard(card.prefab);
            }
        }

        if (GUILayout.Button("Ping", GUILayout.Width(55f))) {
            Selection.activeObject = card.prefab;
            EditorGUIUtility.PingObject(card.prefab);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Source", card.source, EditorStyles.miniLabel);

        if (string.IsNullOrEmpty(card.title) == false) {
            EditorGUILayout.LabelField("Title", Compact(card.title, 90));
        }

        if (string.IsNullOrEmpty(card.question) == false) {
            EditorGUILayout.LabelField("Question", Compact(card.question, 120));
        }

        if (string.IsNullOrEmpty(card.assetPath) == false) {
            EditorGUILayout.LabelField("Path", card.assetPath, EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    void ShowCard(GameObject cardPrefab)
    {
        if (EditorApplication.isPlaying == false) {
            EditorUtility.DisplayDialog(WindowTitle, "Enter Play Mode first, then click Show again.", "OK");
            return;
        }

        CardStack runtimeStack = FindCardStack();
        if (runtimeStack == null) {
            Debug.LogWarning("Card Quick Tester could not find a CardStack in the active scene.");
            return;
        }

        cardStack = runtimeStack;
        GameObject spawned = runtimeStack.DebugShowCard(cardPrefab);
        if (spawned == null) {
            return;
        }

        Selection.activeGameObject = spawned;
        EditorGUIUtility.PingObject(spawned);
        Debug.Log("Card Quick Tester showed card: " + cardPrefab.name);
    }

    void RefreshCards()
    {
        if (cardStack == null) {
            cardStack = FindCardStack();
        }

        cachedCardStack = cardStack;
        cards.Clear();
        cardsByKey.Clear();

        AddCardsFromStack(cardStack);
        AddCardsFromProject();
    }

    void AddCardsFromStack(CardStack stack)
    {
        if (stack == null) {
            return;
        }

        if (stack.fallBackCard != null) {
            AddCard(stack.fallBackCard, "CardStack fallback");
        }

        if (stack.allCards == null) {
            return;
        }

        for (int groupIndex = 0; groupIndex < stack.allCards.Length; groupIndex++) {
            CardStack.cardCategory group = stack.allCards[groupIndex];
            if (group == null || group.groupCards == null) {
                continue;
            }

            string groupName = string.IsNullOrEmpty(group.groupName) ? "Group " + groupIndex : group.groupName;
            for (int cardIndex = 0; cardIndex < group.groupCards.Length; cardIndex++) {
                AddCard(group.groupCards[cardIndex], "CardStack / " + groupName);
            }
        }
    }

    void AddCardsFromProject()
    {
        List<string> validFolders = new List<string>();
        foreach (string folder in CardPrefabFolders) {
            if (AssetDatabase.IsValidFolder(folder)) {
                validFolders.Add(folder);
            }
        }

        if (validFolders.Count == 0) {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", validFolders.ToArray());
        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponent<EventScript>() == null) {
                continue;
            }

            AddCard(prefab, "Project prefab");
        }
    }

    void AddCard(GameObject prefab, string source)
    {
        if (prefab == null) {
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(prefab);
        string key = string.IsNullOrEmpty(assetPath) ? prefab.GetInstanceID().ToString() : assetPath;

        CardEntry existing;
        if (cardsByKey.TryGetValue(key, out existing)) {
            if (existing.source.IndexOf(source, StringComparison.OrdinalIgnoreCase) < 0) {
                existing.source += ", " + source;
            }
            return;
        }

        EventScript eventScript = prefab.GetComponent<EventScript>();
        CardEntry card = new CardEntry {
            prefab = prefab,
            source = source,
            assetPath = assetPath,
            title = GetText(eventScript != null ? eventScript.textFields.titleText : null),
            question = GetText(eventScript != null ? eventScript.textFields.questionText : null)
        };

        cards.Add(card);
        cardsByKey.Add(key, card);
    }

    bool MatchesSearch(CardEntry card)
    {
        if (string.IsNullOrEmpty(searchText)) {
            return true;
        }

        string needle = searchText.Trim();
        if (needle.Length == 0) {
            return true;
        }

        return Contains(card.prefab != null ? card.prefab.name : "", needle)
            || Contains(card.source, needle)
            || Contains(card.title, needle)
            || Contains(card.question, needle)
            || Contains(card.assetPath, needle);
    }

    static CardStack FindCardStack()
    {
        if (EditorApplication.isPlaying && CardStack.instance != null) {
            return CardStack.instance;
        }

        return FindObjectOfType<CardStack>();
    }

    static bool Contains(string haystack, string needle)
    {
        return haystack != null && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static string GetText(EventScript.eventText text)
    {
        return text != null ? text.textContent : "";
    }

    static string Compact(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) {
            return "";
        }

        string compact = text.Replace("\r", " ").Replace("\n", " ").Trim();
        if (compact.Length <= maxLength) {
            return compact;
        }

        return compact.Substring(0, maxLength - 3) + "...";
    }

    class CardEntry
    {
        public GameObject prefab;
        public string source;
        public string assetPath;
        public string title;
        public string question;
    }
}
