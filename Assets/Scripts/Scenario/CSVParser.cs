using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class CSVParser
{
    public static List<ScenarioData> Parse(TextAsset csvFile)
    {
        var scenarioDataList = new List<ScenarioData>();
        var lines = csvFile.text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            var values = SplitCsvLine(line.Trim());

            var data = new ScenarioData
            {
                CharacterID = values.Count > 0 ? values[0] : string.Empty,
                Expression = values.Count > 1 ? values[1] : string.Empty,
                Dialogue = values.Count > 2 ? values[2].Replace("\\\"", "\"") : string.Empty,
                BackgroundImage = values.Count > 3 ? values[3] : string.Empty,
                AffectionChange = values.Count > 5 ? values[5] : string.Empty,
                BranchCondition = values.Count > 6 ? values[6] : string.Empty,
                AnimationDuring = values.Count > 7 ? values[7] : string.Empty,
                AnimationAfter = values.Count > 8 ? values[8] : string.Empty,
            };

            if (values.Count > 4 && !string.IsNullOrEmpty(values[4]))
            {
                string rawEvent = values[4];
                int colonIndex = rawEvent.IndexOf(':');
                if (colonIndex != -1)
                {
                    data.EventType = rawEvent.Substring(0, colonIndex);
                    data.EventValue = rawEvent.Substring(colonIndex + 1);
                }
                else
                {
                    data.EventType = rawEvent;
                    data.EventValue = string.Empty;
                }
            }
            else
            {
                // EventTypeがない場合は、CharacterIDを見てdialogueかoptionかを推測する
                if(data.CharacterID == "option" || data.CharacterID == "timeout")
                {
                    // option と timeout は EventType を持たない想定
                    data.EventType = data.CharacterID;
                }
                else
                {
                     data.EventType = "dialogue";
                }
                data.EventValue = string.Empty;
            }

            scenarioDataList.Add(data);
        }

        return scenarioDataList;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    values.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }
        values.Add(currentField.ToString());
        return values;
    }
}
