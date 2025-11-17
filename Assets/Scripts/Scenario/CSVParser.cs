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

        int pos = 0;
        while (pos < line.Length)
        {
            var field = new StringBuilder();
            bool inQuotes = false;

            // Skip leading whitespace of the field
            while (pos < line.Length && char.IsWhiteSpace(line[pos]))
            {
                pos++;
            }

            // Check for quoted field
            if (pos < line.Length && line[pos] == '"')
            {
                inQuotes = true;
                pos++; // consume quote
            }

            // Read field content
            while (pos < line.Length)
            {
                if (inQuotes)
                {
                    if (line[pos] == '"')
                    {
                        if (pos + 1 < line.Length && line[pos + 1] == '"') // escaped quote
                        {
                            field.Append('"');
                            pos += 2;
                        }
                        else // end of quoted field
                        {
                            inQuotes = false;
                            pos++; // consume quote
                            break;
                        }
                    }
                    else
                    {
                        field.Append(line[pos]);
                        pos++;
                    }
                }
                else // not in quotes
                {
                    if (line[pos] == ',')
                    {
                        // end of unquoted field
                        break;
                    }
                    else
                    {
                        field.Append(line[pos]);
                        pos++;
                    }
                }
            }

            // After the field content is read, find the next comma
            while (pos < line.Length && line[pos] != ',')
            {
                // Skip trailing characters (and whitespace) until a comma or end of line
                pos++;
            }

            // Move past the comma for the next iteration
            if (pos < line.Length && line[pos] == ',')
            {
                pos++;
            }

            values.Add(field.ToString());
        }

        // Add a final empty field if the line ends with a comma
        if (line.EndsWith(","))
        {
            values.Add(string.Empty);
        }

        return values;
    }
}
