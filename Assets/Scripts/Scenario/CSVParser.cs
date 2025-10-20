using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class CSVParser
{
    public static List<ScenarioData> Parse(TextAsset csvFile)
    {
        var scenarioDataList = new List<ScenarioData>();
        var lines = csvFile.text.Split('\n');

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                continue;
            }

            var values = SplitCsvLine(line.Trim());

            if (values.Count < 5) continue;

            var data = new ScenarioData
            {
                CharacterID = values[0],
                Expression = values[1],
                Dialogue = values[2].Replace("\\\"", "\""), // Unescape quotes
                BackgroundImage = values[3],
            };

            // Parse EventType and EventValue (e.g., "jump:scenario_02.csv")
            var eventParts = values[4].Split(':');
            data.EventType = eventParts[0];
            if (eventParts.Length > 1)
            {
                data.EventValue = eventParts[1];
            } else {
                data.EventValue = string.Empty;
            }

            scenarioDataList.Add(data);
        }

        return scenarioDataList;
    }

    private static List<string> SplitCsvLine(string line)
    {
        // This is a simple parser. For more complex CSVs, a library might be better.
        // It handles comma-separated values, including those quoted.
        var values = new List<string>();
        var regex = new Regex("(\\\"(?:\\\\\"|.)*?\\\")|([^,]+)");
        var matches = regex.Matches(line);

        foreach (Match match in matches)
        {
            var value = match.Value.Trim();
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
            }
            values.Add(value);
        }
        return values;
    }
}
