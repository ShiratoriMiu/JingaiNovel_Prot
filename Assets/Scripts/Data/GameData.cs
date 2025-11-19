using System;
using System.Collections.Generic;
using UnityEngine; // Required for ISerializationCallbackReceiver

[Serializable]
public class GameData : ISerializationCallbackReceiver
{
    public string scenarioName;
    public int currentLineIndex;
    public string characterID;
    public string expression;
    public string backgroundImageName;
    public string saveTimestamp;
    public string playerName;

    // The dictionary is used for easy access during gameplay, but won't be serialized directly.
    [NonSerialized]
    public Dictionary<string, int> characterAffections = new Dictionary<string, int>();

    // These two lists will be serialized by JsonUtility, acting as a surrogate for the dictionary.
    [SerializeField] private List<string> affectionKeys = new List<string>();
    [SerializeField] private List<int> affectionValues = new List<int>();

    // Called by Unity before serialization
    public void OnBeforeSerialize()
    {
        affectionKeys.Clear();
        affectionValues.Clear();

        foreach (var pair in characterAffections)
        {
            affectionKeys.Add(pair.Key);
            affectionValues.Add(pair.Value);
        }
    }

    // Called by Unity after deserialization
    public void OnAfterDeserialize()
    {
        characterAffections = new Dictionary<string, int>();

        for (int i = 0; i < affectionKeys.Count; i++)
        {
            // Check for count mismatch to prevent errors
            if (i < affectionValues.Count) {
                characterAffections[affectionKeys[i]] = affectionValues[i];
            }
        }
    }

    // Default constructor for a new game state
    public GameData()
    {
        scenarioName = "";
        currentLineIndex = 0;
        characterID = "";
        expression = "";
        backgroundImageName = "";
        saveTimestamp = "";
        playerName = "主人公";
        characterAffections = new Dictionary<string, int>();
    }
}
