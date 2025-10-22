[System.Serializable]
public class ScenarioData
{
    public string CharacterID;
    public string Expression;
    public string Dialogue;
    public string BackgroundImage;
    public string EventType;
    public string EventValue; // For jumps, etc.

    // --- New fields for Affection System ---
    // e.g., "charA:+10,charB:-5"
    public string AffectionChange;
    // e.g., "charA:>=:50"
    public string BranchCondition;
}
