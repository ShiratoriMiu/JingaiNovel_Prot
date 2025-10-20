using UnityEditor;
using UnityEngine;
using System.IO;

public class DataAssetGenerator
{
    private const string DataPath = "Assets/Resources/Data";

    [MenuItem("Assets/Create/Novel Game/Sample Data Assets")]
    public static void CreateSampleAssets()
    {
        // Ensure the directory exists
        if (!Directory.Exists(DataPath))
        {
            Directory.CreateDirectory(DataPath);
        }

        // --- Create Character Database ---
        CharacterDatabase db = ScriptableObject.CreateInstance<CharacterDatabase>();

        // --- Create Sample Character Data ---
        CharacterData charA = CreateCharacterData("charA", "サンプルA");
        CharacterData charB = CreateCharacterData("charB", "サンプルB");
        CharacterData system = CreateCharacterData("system", "システム");

        // --- Add Characters to Database ---
        db.characters.Add(new CharacterDatabase.CharacterDataEntry { characterId = "charA", characterData = charA });
        db.characters.Add(new CharacterDatabase.CharacterDataEntry { characterId = "charB", characterData = charB });
        db.characters.Add(new CharacterDatabase.CharacterDataEntry { characterId = "system", characterData = system });

        // --- Save Assets to Project ---
        AssetDatabase.CreateAsset(charA, $"{DataPath}/CharA_Data.asset");
        AssetDatabase.CreateAsset(charB, $"{DataPath}/CharB_Data.asset");
        AssetDatabase.CreateAsset(system, $"{DataPath}/System_Data.asset");
        AssetDatabase.CreateAsset(db, $"{DataPath}/CharacterDatabase.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Sample data assets (CharacterDatabase, CharA, CharB, System) created in Assets/Resources/Data.");
    }

    private static CharacterData CreateCharacterData(string id, string characterName)
    {
        CharacterData data = ScriptableObject.CreateInstance<CharacterData>();
        data.characterName = characterName;
        // You could add default expressions here if you have placeholder sprites
        // For example:
        // data.expressions.Add(new CharacterData.Expression { name = "normal", sprite = null });
        return data;
    }
}
