using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class CSVParser
{
    public static List<ScenarioData> Parse(TextAsset csvFile)
    {
        var scenarioDataList = new List<ScenarioData>();
        // Windows (\r\n) と Mac/Linux (\n) の両方の改行コードに対応
        var lines = csvFile.text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                continue;
            }

            var values = SplitCsvLine(line.Trim());

            // 列が不足している場合に備えて、インデックスの範囲をチェック
            var data = new ScenarioData
            {
                CharacterID = values.Count > 0 ? values[0] : string.Empty,
                Expression = values.Count > 1 ? values[1] : string.Empty,
                Dialogue = values.Count > 2 ? values[2].Replace("\\\"", "\"") : string.Empty,
                BackgroundImage = values.Count > 3 ? values[3] : string.Empty,
            };

            if (values.Count > 4)
            {
                 // EventType と EventValue を解析 (例: "jump:scenario_02.csv")
                var eventParts = values[4].Split(':');
                data.EventType = eventParts[0];
                if (eventParts.Length > 1)
                {
                    data.EventValue = eventParts[1];
                }
                else
                {
                    data.EventValue = string.Empty;
                }
            } else {
                // EventType がない場合はデフォルトで "dialogue" を設定
                data.EventType = "dialogue";
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
                // クォート内の処理
                if (c == '"')
                {
                    // 次の文字がクォートであれば、それはエスケープされたクォートとして扱う
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++; // 次のクォートをスキップ
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
                // クォート外の処理
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

        values.Add(currentField.ToString()); // 最後のフィールドを追加

        return values;
    }
}
