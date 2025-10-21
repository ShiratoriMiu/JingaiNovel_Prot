using System;

[System.Serializable]
public class GameData
{
    public string scenarioName;
    public int currentLineIndex;
    public string characterID;
    public string expression;
    public string backgroundImageName;
    public string saveTimestamp;

    // Default constructor for a new game state
    public GameData()
    {
        scenarioName = ""; // Let GameManager fill this in
        currentLineIndex = 0;
        characterID = "";
        expression = "";
        backgroundImageName = "";
        saveTimestamp = "";
    }
}
