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
        if (string.IsNullOrEmpty(line)) return values;

        int start = 0;
        while (start < line.Length)
        {
            values.Add(ParseNextField(line, ref start));
        }
        return values;
    }

    private static string ParseNextField(string line, ref int start)
    {
        var field = new StringBuilder();
        bool inQuotes = false;

        // Skip leading whitespace
        while (start < line.Length && char.IsWhiteSpace(line[start]))
        {
            start++;
        }

        if (start < line.Length && line[start] == '"')
        {
            inQuotes = true;
            start++; // Skip the opening quote
        }

        while (start < line.Length)
        {
            char c = line[start];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (start + 1 < line.Length && line[start + 1] == '"')
                    {
                        field.Append('"'); // Escaped quote
                        start += 2;
                        continue;
                    }
                    else
                    {
                        inQuotes = false;
                        start++; // Skip the closing quote
                        break; // End of field
                    }
                }
                else
                {
                    field.Append(c);
                    start++;
                }
            }
            else
            {
                if (c == ',')
                {
                    start++; // Skip the comma
                    break; // End of field
                }
                else
                {
                    field.Append(c);
                    start++;
                }
            }
        }

        // Skip trailing whitespace until the next comma
        while (start < line.Length && char.IsWhiteSpace(line[start]))
        {
            if (line[start] == ',') break;
            start++;
        }
        if (start < line.Length && line[start] == ',')
        {
             start++; // Consume comma for the next field
        }


        return field.ToString();
    }
}
