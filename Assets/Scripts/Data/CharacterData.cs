using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CharacterData", menuName = "NovelGame/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string characterName;

    [System.Serializable]
    public class Expression
    {
        public string name;
        public Sprite sprite;
    }

    public List<Expression> expressions = new List<Expression>();
}
