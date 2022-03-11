using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TypelistFormatter
{
    internal class TypelistFormatter
    {
        static string dataDir = "Data";

        static string newLine = "\r\n";
        static Regex typeRegex = new Regex(@"\b(.+): (.+)"); // type: value
        static Regex enumRegex = new Regex(@"\(enum\) (.+): (.+)"); // (enum) type: value

        static List<string> RedundantTypeLines = new List<string>(new string[]
        {
            "### name - String",
            "### internalEnabled - Boolean",
            "### internalForceRebuild - Boolean",
            "### internalBlockListPrefix - String",
            "### internalBlockListSuffix - String",
            "### internalBlockListColor - Color",
            "### generalBlockInfo - String",
            "### persistentID - UInt32"
        });

        static List<NestedTypeIndex> nestedTypes = new();

        static string currentDataBlock;

        public static void Run()
        {
            var files = Directory.GetFiles(dataDir);

            Directory.CreateDirectory("Results/nested-types");
            Directory.CreateDirectory("Results/datablocks");

            foreach (var file in files)
            {
                Console.WriteLine(file);
                if (!file.EndsWith("DataBlock.txt"))
                {
                    continue;
                }

                currentDataBlock = new Regex(@"\.(.+DataBlock)\.").Match(file).Groups[1].Value;

                List<string> output = new();

                CreateHeader(output, currentDataBlock);
                CreateFields(output, file);
                output = output.Select((string s) => s.Replace('<', ' ').Replace(">", string.Empty)).ToList(); // md thinks <> is inline html

                File.WriteAllLines($"Results/datablocks/{currentDataBlock}.md", output);
            }

            foreach(var type in nestedTypes)
            {
                List<string> output = new();

                CreateHeader(output, type.FieldName, false);
                CreateFields(output, type.File, type.StartLineIndex, type.EndLineIndex, type.WhiteSpaceCount, false);
                output = output.Select((string s) => s.Replace('<', ' ').Replace(">", string.Empty)).ToList(); // md thinks <> is inline html

                File.WriteAllLines($"Results/nested-types/{type.FieldName}.md", output);
            }
        }

        static void CreateHeader(List<string> output, string name, bool isDataBlock = true)
        {
            output.Add("---");
            if (isDataBlock)
                output.Add($"description: GameData_{name}_bin.json");
            else
                output.Add($"description: {name}");
            output.Add("---");

            output.Add($"{newLine}# {name}{newLine}");

            output.Add("No description provided." + newLine);

            output.Add("***" + newLine);

            output.Add("## Fields");
        }

        static void CreateFields(List<string> output, string file, int startLine = 0, int endLine = -1, int whiteSpaceCount = 0, bool isDataBlock = true)
        {
            var lines = File.ReadAllLines(file);

            var endIndex = lines.Length - 1;
            if (endLine > -1)
                endIndex = endLine;

            for (int i = startLine; i <= endIndex; i++)
            {
                int count = lines[i].TakeWhile(Char.IsWhiteSpace).Count();

                if (count <= whiteSpaceCount)
                {
                    output.Add(newLine + ProcessLine(lines, ref i, whiteSpaceCount, isDataBlock));

                    if (!CheckLastAddedTypeRedundant(output))
                        output.Add(newLine + "No description provided.");
                }
            }

            int j = 0;
            NestedTypeIndex.FindNestedTypesInFile(nestedTypes, file, lines, ref j, 0);
        }

        static bool CheckLastAddedTypeRedundant(List<string> output)
        {
            bool isRedundant = false;

            if (RedundantTypeLines.Contains(output.Last<string>().Replace("\r\n", "")))
            {
                output.RemoveAt(output.Count - 1);
                isRedundant = true;
            }

            return isRedundant;
        }

        static string ProcessLine(string[] lines, ref int i, int whiteSpaceCount, bool isDataBlock)
        {
            if (i < lines.Length - 1)
            {
                int count = lines[i + 1].TakeWhile(Char.IsWhiteSpace).Count();

                if (count > whiteSpaceCount)
                    return ProcessNestedType(lines, ref i, whiteSpaceCount, isDataBlock);                
            }

            var line = lines[i].TrimStart();

            if (line.StartsWith("(enum)"))
            {
                return ProcessEnum(line);
            }

            return ProcessPlainType(line);
        }

        static string ProcessNestedType(string[] lines, ref int i, int whiteSpaceCount, bool isDataBlock)
        {
            var match = typeRegex.Match(lines[i]);
            var type = match.Groups[1].Value;
            var name = match.Groups[2].Value;

            var res = $"### {name} - [{type}]({(isDataBlock ? "../nested-types" : ".")}/{GetRealTypeFromLine(lines[i])}.md) (nested type)"; // ProcessPlainType(lines[i]);

            int j = i + 1;
            while (j < lines.Length)
            {
                int count = lines[i + 1].TakeWhile(Char.IsWhiteSpace).Count();

                if (count > whiteSpaceCount)
                    break;

                j++;
            }
            i = j - 1; // iterator will move ahead by 1

            return res;
        }

        static string ProcessEnum(string line)
        {
            var match = enumRegex.Match(line);
            var type = match.Groups[1].Value;
            var name = match.Groups[2].Value;

            return $"### {name} - [{type}](../enum-types.md#{GetRealTypeFromLine(line)}) (enum)";
        }

        static string ProcessPlainType(string line)
        {
            var match = typeRegex.Match(line);
            var type = match.Groups[1].Value;
            var name = match.Groups[2].Value;

            return $"### {name} - {type}";
        }

        public static string GetRealTypeFromLine(string line)
        {
            string type = "";

            if (line.IndexOf("enum") < 0)
            {
                var match = typeRegex.Match(line);
                type = match.Groups[1].Value;
            }
            else
            {
                var match = enumRegex.Match(line);
                type = match.Groups[1].Value;
            }

            if (type.IndexOf('<') > 0)
            {
                type = type.Substring(type.LastIndexOf('<') + 1);
                type = type.Substring(0, type.IndexOf('>'));
            }

            return type;
        }

        #region GetCountsForNestedMatches

        //class NestedMatch
        //{
        //    public string DataBlock { get; set; }
        //    public string Field { get; set; }
        //    public int MatchCount { get; set; }

        //    public override bool Equals(object obj)
        //    {
        //        return obj is NestedMatch match &&
        //               DataBlock == match.DataBlock &&
        //               Field == match.Field;
        //    }

        //    public override int GetHashCode()
        //    {
        //        return HashCode.Combine(DataBlock, Field);
        //    }

        //    public static implicit operator string(NestedMatch m) => $"{m.DataBlock} - {m.Field}: {m.MatchCount}";
        //}

        // static List<NestedMatch> nestedMatches = new();

        //static string ProcessNestedType(string[] lines, ref int i) // return one line, register to list of classes (start, end indexes and type) to reiterate on and create new section at the bottom
        //{
        //    AddNestedMatch(lines[i]);
        //    var res = ProcessPlainType(lines[i]);

        //    int j = i + 1; // for when processing is added
        //    while (j < lines.Length)
        //    {
        //        if (!lines[j].StartsWith(' '))
        //            break;

        //        j++;
        //    }
        //    i = j - 1; // iterator will move ahead by 1

        //    return res;
        //}

        //public static void Run()
        //{
        //    var files = Directory.GetFiles(dataDir);

        //    Directory.CreateDirectory("Results");

        //    foreach (var file in files)
        //    {
        //        Console.WriteLine(file);
        //        if (!file.EndsWith("DataBlock.txt"))
        //            continue;

        //        var lines = File.ReadAllLines(file);

        //        currentDataBlock = new Regex(@"\.(.+DataBlock)\.").Match(file).Groups[1].Value;


        //        List<string> output = new();
        //        CreateHeader(output, currentDataBlock);

        //        for (int i = 0; i < lines.Length; i++)
        //        {
        //            output.Add(NewLine + ProcessLine(lines, ref i));

        //            if (!CheckRedundantType(output))
        //                output.Add(NewLine + "No description provided.");
        //        }

        //        output = output.Select((string s) => s.Replace('<', ' ').Replace(">", string.Empty)).ToList(); // md thinks <> is inline html
        //        // output.RemoveAt(output.Count - 1); // last 2 lines are always empty, only one is needed

        //        File.WriteAllLines($"Results/{currentDataBlock}.md", output);
        //        break; // Just testing the first file for now
        //    }

        //    File.WriteAllLines($"Results/NestedMatchCounts.txt", nestedMatches.Select(x => (string)x));

        //    Dictionary<string, int> aggregatedMatches = new();

        //    foreach (var m in nestedMatches)
        //    {
        //        if (!aggregatedMatches.ContainsKey(m.Field))
        //            aggregatedMatches.Add(m.Field, 0);

        //        aggregatedMatches[m.Field]++;
        //    }

        //    var am = aggregatedMatches.OrderByDescending(x => x.Value);

        //    File.WriteAllLines($"Results/AggregatedNestedMatchCounts.txt", am.Select(x => $"{x.Key}: {x.Value}"));
        //}

        //static void AddNestedMatch(string line)
        //{
        //    NestedMatch match = new()
        //    {
        //        DataBlock = currentDataBlock,
        //        Field = GetRealTypeFromLine(line),
        //        MatchCount = 1
        //    };

        //    var existingMatch = nestedMatches.FirstOrDefault(x => x.Equals(match));

        //    if (existingMatch != null)
        //    {
        //        existingMatch.MatchCount++;
        //        return;
        //    }

        //    nestedMatches.Add(match);
        //}
        #endregion
    }
}
