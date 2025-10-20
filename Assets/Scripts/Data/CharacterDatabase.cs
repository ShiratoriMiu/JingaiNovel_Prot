using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "NovelGame/CharacterDatabase")]
public class CharacterDatabase : ScriptableObject
{
    public List<CharacterDataEntry> characters = new List<CharacterDataEntry>();

    [System.Serializable]
    public class CharacterDataEntry
    {
        public string characterId;
        public CharacterData characterData;
    }

    public CharacterData GetCharacterData(string characterId)
    {
        return characters.Find(c => c.characterId == characterId)?.characterData;
    }
}
