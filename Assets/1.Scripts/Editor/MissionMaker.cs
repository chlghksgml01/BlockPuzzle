using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class MissionMaker : EditorWindow
{
    private const int MinBoardSize = 8;
    private const int MaxBoardSize = 10;
    private const float MinCellPixelSize = 24f;
    private const float MaxCellPixelSize = 72f;
    private const float BoardPadding = 12f;
    private const string SpritePalettePrefsKey = "BlockPuzzle.MissionMaker.SpritePaletteGuid";
    private const string SelectedSpriteIndexPrefsKey = "BlockPuzzle.MissionMaker.SelectedSpriteIndex";
    private static readonly string[] CheckerboardPaths =
    {
        "Assets/Chess Studio/Block Puzzle GUI Pack/png/Game/Checkerboard8x8.png",
        "Assets/Chess Studio/Block Puzzle GUI Pack/png/Game/Checkerboard9x9.png",
        "Assets/Chess Studio/Block Puzzle GUI Pack/png/Game/Checkerboard10x10.png"
    };

    [SerializeField] private int _boardSize = 9;
    [SerializeField] private BlockSpritePalette _spritePalette;
    [SerializeField] private int _selectedSpriteIndex;
    [SerializeField] private MissionData _missionAsset;
    [SerializeField] private bool _eraseMode;
    [SerializeField] private bool _isHard;
    [SerializeField] private bool _isClear;
    [SerializeField] private MissionType _missionType = MissionType.ScoreGoal;
    [SerializeField] private int _targetScore;
    [SerializeField] private float _timeLimitSeconds;

    private readonly Dictionary<Vector2Int, string> _filledCells = new Dictionary<Vector2Int, string>();
    private Vector2 _scrollPosition;
    private Rect _boardRect;
    private float _cellSize;
    private bool _isPainting;
    private bool _isErasing;
    private Vector2Int _lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);

    private Sprite[] BlockSprites => _spritePalette != null ? _spritePalette.sprites : null;

    [MenuItem("BlockPuzzle/Mission Maker")]
    public static void Open()
    {
        MissionMaker window = GetWindow<MissionMaker>("Mission Maker");
        window.minSize = new Vector2(360f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        LoadEditorSettings();
    }

    private void OnDisable()
    {
        SaveEditorSettings();
    }

    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(6f);
        DrawSpritePalette();
        EditorGUILayout.Space(8f);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        DrawBoardArea();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6f);
        DrawFooter();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.LabelField("Mission Maker", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _boardSize = EditorGUILayout.IntSlider("Board Size", _boardSize, MinBoardSize, MaxBoardSize);
        if (EditorGUI.EndChangeCheck())
            RemoveOutOfBoundsCells();

        _missionAsset = (MissionData)EditorGUILayout.ObjectField(
            "Mission Asset",
            _missionAsset,
            typeof(MissionData),
            false);

        EditorGUILayout.BeginHorizontal();
        _isHard = EditorGUILayout.ToggleLeft("Is Hard", _isHard, GUILayout.Width(80f));
        _isClear = EditorGUILayout.ToggleLeft("Is Clear", _isClear, GUILayout.Width(80f));
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.EnumPopup("Mission Type (Auto)", _missionType);
        EditorGUI.EndDisabledGroup();

        if (_missionType == MissionType.ScoreGoal)
        {
            _targetScore = EditorGUILayout.IntField("Target Score", _targetScore);
            _timeLimitSeconds = EditorGUILayout.FloatField("Time Limit (sec)", _timeLimitSeconds);
        }

        EditorGUILayout.BeginHorizontal();
        _eraseMode = GUILayout.Toggle(_eraseMode, "Erase Mode", EditorStyles.miniButtonLeft);
        if (GUILayout.Button("Clear All", EditorStyles.miniButtonMid))
            ClearAllCells();

        if (GUILayout.Button("Load Asset", EditorStyles.miniButtonMid))
            LoadFromAsset();

        if (GUILayout.Button("Save Asset", EditorStyles.miniButtonRight))
            SaveToAsset();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSpritePalette()
    {
        EditorGUILayout.LabelField("Block Sprites", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _spritePalette = (BlockSpritePalette)EditorGUILayout.ObjectField(
            "Sprite Palette",
            _spritePalette,
            typeof(BlockSpritePalette),
            false);
        if (EditorGUI.EndChangeCheck())
            SaveEditorSettings();

        if (_spritePalette == null)
        {
            EditorGUILayout.HelpBox(
                "Block Sprite Palette 에셋을 지정하세요.\nCreate > BlockPuzzle > Block Sprite Palette",
                MessageType.Info);
            return;
        }

        SerializedObject paletteSerializedObject = new SerializedObject(_spritePalette);
        paletteSerializedObject.Update();
        SerializedProperty spritesProperty = paletteSerializedObject.FindProperty("sprites");
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(spritesProperty, true);
        if (EditorGUI.EndChangeCheck())
        {
            paletteSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(_spritePalette);
        }
        else
        {
            paletteSerializedObject.ApplyModifiedProperties();
        }

        Sprite[] blockSprites = BlockSprites;
        if (blockSprites == null || blockSprites.Length == 0)
        {
            EditorGUILayout.HelpBox("팔레트에 블록 스프라이트를 추가하세요. (인스펙터에서도 편집 가능)", MessageType.Info);
            return;
        }

        _selectedSpriteIndex = Mathf.Clamp(_selectedSpriteIndex, 0, blockSprites.Length - 1);

        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < blockSprites.Length; i++)
        {
            Sprite sprite = blockSprites[i];
            Rect buttonRect = GUILayoutUtility.GetRect(44f, 44f, GUILayout.Width(44f), GUILayout.Height(44f));

            if (_selectedSpriteIndex == i)
                EditorGUI.DrawRect(buttonRect, new Color(0.25f, 0.55f, 1f, 0.35f));

            if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
            {
                _selectedSpriteIndex = i;
                SaveEditorSettings();
            }

            Rect spriteRect = InsetRect(buttonRect, 4f);
            DrawSprite(spriteRect, sprite);

            if (sprite != null)
            {
                GUI.Label(
                    new Rect(buttonRect.x, buttonRect.yMax - 14f, buttonRect.width, 14f),
                    sprite.name,
                    EditorStyles.centeredGreyMiniLabel);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBoardArea()
    {
        _cellSize = CalculateCellSize();
        float boardPixelSize = _boardSize * _cellSize;
        Rect layoutRect = GUILayoutUtility.GetRect(boardPixelSize + BoardPadding * 2f, boardPixelSize + BoardPadding * 2f);
        _boardRect = new Rect(layoutRect.x + BoardPadding, layoutRect.y, boardPixelSize, boardPixelSize);

        EditorGUI.DrawRect(_boardRect, new Color(0.12f, 0.12f, 0.12f, 1f));
        DrawCheckerboardBackground(_boardRect);

        Handles.BeginGUI();
        for (int y = 0; y < _boardSize; y++)
        {
            for (int x = 0; x < _boardSize; x++)
            {
                Rect cellRect = GetCellRect(_boardRect, x, y, _cellSize);
                DrawCell(cellRect, x, y);
            }
        }
        Handles.EndGUI();

        HandleBoardInput();

        if (_filledCells.Count > 0)
            EditorGUILayout.LabelField($"Filled Cells: {_filledCells.Count}", EditorStyles.miniLabel);
    }

    private void DrawFooter()
    {
        EditorGUILayout.HelpBox(
            "좌클릭/드래그: 선택한 블록으로 칸 채우기\n우클릭/드래그: 칸 비우기\nErase Mode: 좌클릭으로 지우기",
            MessageType.None);

        if (GUILayout.Button("Copy JSON To Clipboard"))
            CopyJsonToClipboard();
    }

    private void DrawCell(Rect cellRect, int x, int y)
    {
        Vector2Int key = new Vector2Int(x, y);
        bool isFilled = _filledCells.TryGetValue(key, out string spriteName);

        EditorGUI.DrawRect(cellRect, new Color(1f, 1f, 1f, isFilled ? 0.04f : 0.08f));

        if (isFilled && TryGetSpriteByName(spriteName, out Sprite sprite))
            DrawSprite(InsetRect(cellRect, 2f), sprite);

        Handles.color = new Color(0f, 0f, 0f, 0.25f);
        Handles.DrawWireCube(cellRect.center, cellRect.size);
    }

    private void DrawCheckerboardBackground(Rect boardRect)
    {
        Sprite checkerboard = LoadCheckerboardSprite(_boardSize);
        if (checkerboard == null)
            return;

        DrawSprite(boardRect, checkerboard);
    }

    private void HandleBoardInput()
    {
        Event currentEvent = Event.current;
        if (currentEvent.type != EventType.MouseDown &&
            currentEvent.type != EventType.MouseDrag &&
            currentEvent.type != EventType.MouseUp)
            return;

        if (currentEvent.button != 0 && currentEvent.button != 1)
            return;

        if (currentEvent.type == EventType.MouseUp)
        {
            _isPainting = false;
            _isErasing = false;
            _lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);
            return;
        }

        if (!_boardRect.Contains(currentEvent.mousePosition))
            return;

        if (!TryGetCellFromMouse(_boardRect, _cellSize, currentEvent.mousePosition, out int x, out int y))
            return;

        bool shouldErase = currentEvent.button == 1 || (_eraseMode && currentEvent.button == 0);

        if (currentEvent.type == EventType.MouseDown)
        {
            _isPainting = !shouldErase;
            _isErasing = shouldErase;
            ApplyPaint(x, y, shouldErase);
            currentEvent.Use();
            Repaint();
            return;
        }

        if (currentEvent.type == EventType.MouseDrag && (_isPainting || _isErasing))
        {
            if (_lastPaintedCell.x == x && _lastPaintedCell.y == y)
                return;

            ApplyPaint(x, y, _isErasing);
            currentEvent.Use();
            Repaint();
        }
    }

    private void ApplyPaint(int x, int y, bool erase)
    {
        Vector2Int key = new Vector2Int(x, y);
        _lastPaintedCell = key;

        if (erase)
        {
            _filledCells.Remove(key);
            RefreshMissionType();
            return;
        }

        Sprite selectedSprite = GetSelectedSprite();
        if (selectedSprite == null)
            return;

        _filledCells[key] = selectedSprite.name;
        RefreshMissionType();
    }

    private void RefreshMissionType()
    {
        _missionType = ResolveMissionTypeFromFilledCells();
    }

    /// <summary>
    /// grass → Grass, ice → Ice, Pentagon/Square/Star → Gem,
    /// 비어 있거나 stone만 있으면 ScoreGoal.
    /// </summary>
    private MissionType ResolveMissionTypeFromFilledCells()
    {
        bool hasGrass = false;
        bool hasIce = false;
        bool hasGem = false;

        foreach (KeyValuePair<Vector2Int, string> pair in _filledCells)
        {
            string spriteName = pair.Value;
            if (BoardCell.IsGrassSpriteName(spriteName))
                hasGrass = true;
            else if (BoardCell.IsIceSpriteName(spriteName))
                hasIce = true;
            else if (BoardCell.IsGemSpriteName(spriteName))
                hasGem = true;
        }

        if (hasGrass)
            return MissionType.Grass;
        if (hasIce)
            return MissionType.Ice;
        if (hasGem)
            return MissionType.Gem;

        return MissionType.ScoreGoal;
    }

    private bool TryGetCellFromMouse(Rect boardRect, float cellSize, Vector2 mousePosition, out int x, out int y)
    {
        x = y = -1;
        if (!boardRect.Contains(mousePosition))
            return false;

        x = Mathf.Clamp(Mathf.FloorToInt((mousePosition.x - boardRect.xMin) / cellSize), 0, _boardSize - 1);
        y = Mathf.Clamp(Mathf.FloorToInt((mousePosition.y - boardRect.yMin) / cellSize), 0, _boardSize - 1);
        return true;
    }

    private static Rect GetCellRect(Rect boardRect, int x, int y, float cellSize)
    {
        return new Rect(
            boardRect.x + x * cellSize,
            boardRect.y + y * cellSize,
            cellSize,
            cellSize);
    }

    private float CalculateCellSize()
    {
        float availableWidth = position.width - BoardPadding * 2f - 16f;
        float cellSize = availableWidth / _boardSize;
        return Mathf.Clamp(cellSize, MinCellPixelSize, MaxCellPixelSize);
    }

    private Sprite GetSelectedSprite()
    {
        Sprite[] blockSprites = BlockSprites;
        if (blockSprites == null || blockSprites.Length == 0)
            return null;

        _selectedSpriteIndex = Mathf.Clamp(_selectedSpriteIndex, 0, blockSprites.Length - 1);
        return blockSprites[_selectedSpriteIndex];
    }

    private bool TryGetSpriteByName(string spriteName, out Sprite sprite)
    {
        sprite = null;
        Sprite[] blockSprites = BlockSprites;
        if (string.IsNullOrEmpty(spriteName) || blockSprites == null)
            return false;

        for (int i = 0; i < blockSprites.Length; i++)
        {
            Sprite candidate = blockSprites[i];
            if (candidate != null && candidate.name == spriteName)
            {
                sprite = candidate;
                return true;
            }
        }

        return false;
    }

    private void ClearAllCells()
    {
        _filledCells.Clear();
        RefreshMissionType();
        Repaint();
    }

    private void RemoveOutOfBoundsCells()
    {
        List<Vector2Int> removeKeys = new List<Vector2Int>();
        foreach (KeyValuePair<Vector2Int, string> pair in _filledCells)
        {
            if (pair.Key.x >= _boardSize || pair.Key.y >= _boardSize)
                removeKeys.Add(pair.Key);
        }

        for (int i = 0; i < removeKeys.Count; i++)
            _filledCells.Remove(removeKeys[i]);

        RefreshMissionType();
    }

    private void LoadFromAsset()
    {
        if (_missionAsset == null)
        {
            EditorUtility.DisplayDialog("Mission Maker", "Mission Asset를 먼저 지정해 주세요.", "OK");
            return;
        }

        _boardSize = Mathf.Clamp(_missionAsset.boardSize, MinBoardSize, MaxBoardSize);
        _isHard = _missionAsset.isHard;
        _isClear = _missionAsset.isClear;
        _targetScore = _missionAsset.targetScore;
        _timeLimitSeconds = _missionAsset.timeLimitSeconds;
        _filledCells.Clear();

        List<FilledCellData> cells = _missionAsset.filledCells;
        if (cells == null)
        {
            RefreshMissionType();
            Repaint();
            return;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            FilledCellData data = cells[i];
            if (data == null)
                continue;

            if (data.x < 0 || data.x >= _boardSize || data.y < 0 || data.y >= _boardSize)
                continue;

            _filledCells[new Vector2Int(data.x, data.y)] = data.spriteName;
        }

        RefreshMissionType();
        Repaint();
    }

    private void SaveToAsset()
    {
        if (_missionAsset == null)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Mission Data",
                "MissionData",
                "asset",
                "저장할 Mission Data 경로를 선택하세요.");

            if (string.IsNullOrEmpty(path))
                return;

            _missionAsset = CreateInstance<MissionData>();
            AssetDatabase.CreateAsset(_missionAsset, path);
        }

        _missionAsset.boardSize = _boardSize;
        _missionAsset.filledCells = ExportFilledCells();
        _missionAsset.isHard = _isHard;
        _missionAsset.isClear = _isClear;
        _missionAsset.missionType = _missionType;
        _missionAsset.targetScore = _targetScore;
        _missionAsset.timeLimitSeconds = _timeLimitSeconds;

        EditorUtility.SetDirty(_missionAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(_missionAsset);

        _missionAsset = null;
        Repaint();
    }

    private List<FilledCellData> ExportFilledCells()
    {
        List<FilledCellData> result = new List<FilledCellData>(_filledCells.Count);
        foreach (KeyValuePair<Vector2Int, string> pair in _filledCells)
        {
            result.Add(new FilledCellData
            {
                x = pair.Key.x,
                y = pair.Key.y,
                spriteName = pair.Value
            });
        }

        return result;
    }

    private void CopyJsonToClipboard()
    {
        MissionData temp = CreateInstance<MissionData>();
        temp.boardSize = _boardSize;
        temp.filledCells = ExportFilledCells();
        temp.isHard = _isHard;
        temp.isClear = _isClear;
        temp.missionType = _missionType;
        temp.targetScore = _targetScore;
        temp.timeLimitSeconds = _timeLimitSeconds;
        EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(temp, true);
        Debug.Log("Board mission JSON copied to clipboard.");
        DestroyImmediate(temp);
    }

    private void LoadEditorSettings()
    {
        string paletteGuid = EditorPrefs.GetString(SpritePalettePrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(paletteGuid))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(paletteGuid);
            if (!string.IsNullOrEmpty(assetPath))
                _spritePalette = AssetDatabase.LoadAssetAtPath<BlockSpritePalette>(assetPath);
        }

        _selectedSpriteIndex = EditorPrefs.GetInt(SelectedSpriteIndexPrefsKey, 0);
        Sprite[] blockSprites = BlockSprites;
        if (blockSprites != null && blockSprites.Length > 0)
            _selectedSpriteIndex = Mathf.Clamp(_selectedSpriteIndex, 0, blockSprites.Length - 1);
    }

    private void SaveEditorSettings()
    {
        if (_spritePalette != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(_spritePalette);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            EditorPrefs.SetString(SpritePalettePrefsKey, guid);
        }
        else
        {
            EditorPrefs.DeleteKey(SpritePalettePrefsKey);
        }

        EditorPrefs.SetInt(SelectedSpriteIndexPrefsKey, _selectedSpriteIndex);
    }

    private static Sprite LoadCheckerboardSprite(int boardSize)
    {
        int index = Mathf.Clamp(boardSize - MinBoardSize, 0, CheckerboardPaths.Length - 1);
        return AssetDatabase.LoadAssetAtPath<Sprite>(CheckerboardPaths[index]);
    }

    private static void DrawSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return;

        Texture2D texture = sprite.texture;
        Rect uv = new Rect(
            sprite.textureRect.x / texture.width,
            sprite.textureRect.y / texture.height,
            sprite.textureRect.width / texture.width,
            sprite.textureRect.height / texture.height);

        GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
    }

    private static Rect InsetRect(Rect rect, float inset)
    {
        return new Rect(rect.x + inset, rect.y + inset, rect.width - inset * 2f, rect.height - inset * 2f);
    }
}
