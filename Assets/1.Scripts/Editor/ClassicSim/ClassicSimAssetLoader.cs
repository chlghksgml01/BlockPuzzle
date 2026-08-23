using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Classic 런타임과 같은 BlockShape / ScoreSystem / InGameManager 감쇠 값을 에디터에서 읽는다.
/// </summary>
public static class ClassicSimAssetLoader
{
    private const string DraggableBlockPrefabPath = "Assets/2.Prefabs/DraggableBlock.prefab";
    private const string InGameManagerPrefabPath = "Assets/2.Prefabs/Manager/InGameManager.prefab";
    private const string ScoreSystemPath = "Assets/3.ScriptableObjects/ScoreSystem.asset";

    public static string TryLoadInto(ClassicSimConfig config)
    {
        if (!TryLoadShapes(config, out string shapeError))
            return shapeError;

        LoadHighFillWeightSettings(config);
        LoadScoreSettings(config);
        return null;
    }

    private static bool TryLoadShapes(ClassicSimConfig config, out string error)
    {
        error = null;
        List<ClassicSimShapeDef> shapes = new List<ClassicSimShapeDef>();

        DraggableBlock draggable = AssetDatabase.LoadAssetAtPath<DraggableBlock>(DraggableBlockPrefabPath);
        if (draggable != null)
        {
            SerializedObject so = new SerializedObject(draggable);
            SerializedProperty array = so.FindProperty("_blockShapes");
            if (array != null && array.isArray)
            {
                for (int i = 0; i < array.arraySize; i++)
                {
                    BlockShape shape = array.GetArrayElementAtIndex(i).objectReferenceValue as BlockShape;
                    ClassicSimShapeDef def = ToDef(shape);
                    if (def != null)
                        shapes.Add(def);
                }
            }
        }

        if (shapes.Count == 0)
        {
            string[] guids = AssetDatabase.FindAssets("t:BlockShape");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                BlockShape shape = AssetDatabase.LoadAssetAtPath<BlockShape>(path);
                ClassicSimShapeDef def = ToDef(shape);
                if (def != null)
                    shapes.Add(def);
            }
        }

        if (shapes.Count == 0)
        {
            error = "BlockShape를 찾지 못했습니다. DraggableBlock 프리팹 또는 Blocks 에셋을 확인하세요.";
            return false;
        }

        config.Shapes = shapes.ToArray();
        return true;
    }

    private static void LoadHighFillWeightSettings(ClassicSimConfig config)
    {
        InGameManager inGameManager = AssetDatabase.LoadAssetAtPath<InGameManager>(InGameManagerPrefabPath);
        if (inGameManager == null)
            return;

        SerializedObject so = new SerializedObject(inGameManager);

        SerializedProperty startFillRatio = so.FindProperty("_highFillShapeWeightSettings._startFillRatio");
        if (startFillRatio != null)
            config.LargeShapeSpawnReduceStartFillRatio = startFillRatio.floatValue;

        SerializedProperty largeThreshold = so.FindProperty("_highFillShapeWeightSettings._largeShapeCellThreshold");
        if (largeThreshold != null)
            config.LargeShapeCellThreshold = Mathf.Max(1, largeThreshold.intValue);

        SerializedProperty smallThreshold = so.FindProperty("_highFillShapeWeightSettings._smallShapeCellThreshold");
        if (smallThreshold != null)
            config.SmallShapeCellThreshold = Mathf.Max(1, smallThreshold.intValue);

        SerializedProperty largeMultiplier = so.FindProperty("_highFillShapeWeightSettings._highFillLargeShapeWeightMultiplier");
        if (largeMultiplier != null)
            config.HighFillLargeShapeWeightMultiplier = largeMultiplier.floatValue;

        SerializedProperty smallMultiplier = so.FindProperty("_highFillShapeWeightSettings._highFillSmallShapeWeightMultiplier");
        if (smallMultiplier != null)
            config.HighFillSmallShapeWeightMultiplier = smallMultiplier.floatValue;
    }

    private static void LoadScoreSettings(ClassicSimConfig config)
    {
        ScoreSystem score = AssetDatabase.LoadAssetAtPath<ScoreSystem>(ScoreSystemPath);
        if (score == null)
            return;

        SerializedObject so = new SerializedObject(score);
        SerializedProperty lineMul = so.FindProperty("_lineScoreMultiplier");
        if (lineMul != null)
            config.LineScoreMultiplier = lineMul.floatValue;

        SerializedProperty lineBonus = so.FindProperty("_lineBonusMultiplier");
        if (lineBonus != null)
            config.LineBonusMultiplier = lineBonus.floatValue;

        SerializedProperty comboMul = so.FindProperty("_comboScoreMultiplier");
        if (comboMul != null)
            config.ComboScoreMultiplier = comboMul.floatValue;

        SerializedProperty comboRemain = so.FindProperty("_comboRemainCount");
        if (comboRemain != null)
            config.ComboRemainCount = comboRemain.intValue;
    }

    private static ClassicSimShapeDef ToDef(BlockShape shape)
    {
        if (shape == null || shape.CellOffsets == null || shape.CellOffsets.Length == 0)
            return null;

        Vector2Int[] offsets = new Vector2Int[shape.CellOffsets.Length];
        System.Array.Copy(shape.CellOffsets, offsets, shape.CellOffsets.Length);
        return new ClassicSimShapeDef
        {
            Name = shape.name,
            Offsets = offsets,
            Weight = shape.Weights,
            CellCount = offsets.Length
        };
    }
}
