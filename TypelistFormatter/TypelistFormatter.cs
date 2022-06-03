using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TypelistFormatter
{
    public class TypelistFormatter
    {
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
            Summary summary = new Summary();

            var files = Directory.GetFiles(Constants.DataDir);
            files = files.OrderBy(f => f).ToArray();

            Directory.CreateDirectory("Results/nested-types");
            Directory.CreateDirectory($"Results/datablocks/{BlocksCategories.Data.GetCategoryDirectory(Constants.MainCategory)}");
            Directory.CreateDirectory($"Results/datablocks/{BlocksCategories.Data.GetCategoryDirectory(Constants.RarelyEditedCategory)}");
            Directory.CreateDirectory($"Results/datablocks/{BlocksCategories.Data.GetCategoryDirectory(Constants.UnusedCategory)}");

            summary.CreateEntry(Constants.MainCategory,
                $"reference/datablocks/{BlocksCategories.Data.GetCategoryDirectory(Constants.MainCategory)}");
            summary.CreateEntry(Constants.RarelyEditedCategory,
                $"reference/datablocks/{BlocksCategories.Data.GetCategoryDirectory(Constants.RarelyEditedCategory)}");
            summary.CreateEntry(Constants.UnusedCategory,
                $"reference/datablocks/{BlocksCategories.Data.GetCategoryDirectory(Constants.UnusedCategory)}");

            foreach (var file in files)
            {
                if (!file.EndsWith("DataBlock.txt"))
                {
                    continue;
                }

                currentDataBlock = new Regex(@"\.(.+DataBlock)\.").Match(file).Groups[1].Value;

                List<string> output = new();

                CreateHeader(output, currentDataBlock);
                CreateFields(output, file);
                output = output.Select((string s) => s.Replace('<', ' ').Replace(">", string.Empty)).ToList(); // md thinks <> is inline html

                var dbName = $"{currentDataBlock.Replace("DataBlock", string.Empty)}";
                var category = BlocksCategories.Data.GetCategory(currentDataBlock);
                var directory = BlocksCategories.Data.GetCategoryDirectory(category);

                summary.AddDataBlock(category, dbName);
                File.WriteAllLines($"Results/datablocks/{directory}/{dbName.ToLower()}.md", output);
            }

            nestedTypes = nestedTypes.OrderBy(x => x.HeaderText).ToList();

            foreach(var type in nestedTypes)
            {
                List<string> output = new();

                CreateHeader(output, type.HeaderText, false);
                CreateFields(output, type.File, type.StartLineIndex, type.EndLineIndex, type.WhiteSpaceCount, false);
                output = output.Select((string s) => s.Replace('<', ' ').Replace(">", string.Empty)).ToList(); // md thinks <> is inline html

                summary.AddNestedType(type);
                
                File.WriteAllLines($"Results/nested-types/{type.FieldName}.md", output);
            }

            summary.WriteSummary();
        }

        static void CreateHeader(List<string> output, string name, bool isDataBlock = true)
        {
            output.Add("---");
            if (isDataBlock)
                output.Add($"description: GameData_{name}_bin.json");
            else
                output.Add($"description: {name}");
            output.Add("---");

            if (isDataBlock)
                output.Add($"{Constants.NewLine}# {name.Replace("DataBlock", string.Empty)}{Constants.NewLine}");
            else
                output.Add($"{Constants.NewLine}# {name}{Constants.NewLine}");

            output.Add("No description provided." + Constants.NewLine);

            output.Add("***" + Constants.NewLine);

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
                    output.Add(Constants.NewLine + ProcessLine(lines, ref i, whiteSpaceCount, isDataBlock));

                    if (!CheckLastAddedTypeRedundant(output))
                        output.Add(Constants.NewLine + "No description provided.");
                }
            }

            int j = 0;

            if (isDataBlock)
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

        static string ProcessLine(string[] lines, ref int i, int whiteSpaceCount, bool fromDataBlock)
        {
            if (i < lines.Length - 1)
            {
                int count = lines[i + 1].TakeWhile(Char.IsWhiteSpace).Count();

                if (count > whiteSpaceCount)
                    return ProcessNestedType(lines, ref i, whiteSpaceCount, fromDataBlock);                
            }

            var line = lines[i].TrimStart();

            if (line.StartsWith("(enum)"))
            {
                return ProcessEnum(line, fromDataBlock);
            }
            else if (Constants.IDReferenceRegex.IsMatch(line))
            {
                return ProcessIDReference(line, fromDataBlock);
            }

            return ProcessGenericType(line);
        }

        static string ProcessNestedType(string[] lines, ref int i, int whiteSpaceCount, bool fromDataBlock)
        {
            var match = Constants.GenericRegex.Match(lines[i]);
            var type = match.Groups[1].Value;
            var name = match.Groups[2].Value;

            var res = $"### {name} - [{type}]({(fromDataBlock ? "../../nested-types" : ".")}/{GetTypeForLinkFromLine(lines[i])}.md) (nested type)";

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

        static string ProcessGenericType(string line)
        {
            var match = Constants.GenericRegex.Match(line);
            var type = match.Groups[1].Value;
            var name = match.Groups[2].Value;

            return $"### {name} - {type}";
        }

        static string ProcessEnum(string line, bool fromDataBlock)
        {
            var match = Constants.EnumRegex.Match(line);
            var type = match.Groups[1].Value;
            var name = match.Groups[2].Value;

            return $"### {name} - [{type}]({(fromDataBlock ? "../../" : "../")}enum-types.md#{GetTypeForLinkFromLine(line)}) (enum)";
        }

        static string ProcessIDReference(string line, bool fromDataBlock)
        {
            var match = Constants.IDReferenceRegex.Match(line);
            var dataBlock = match.Groups[1].Value;
            var type = match.Groups[2].Value;
            var name = match.Groups[3].Value;

            var link = BlocksCategories.Data.GetLink(dataBlock, fromDataBlock);

            return $"### {name} - {type} ([{dataBlock}]({link}))";
        }

        public static string GetTypeForLinkFromLine(string line)
        {
            return GetTypeFromLine(line).ToLower(); // because gitbook
        }

        public static string GetTypeFromLine(string line)
        {
            string type;

            if (line.IndexOf("enum") < 0)
            {
                var match = Constants.GenericRegex.Match(line);
                type = match.Groups[1].Value;
            }
            else if (Constants.IDReferenceRegex.IsMatch(line))
            {
                var match = Constants.IDReferenceRegex.Match(line);
                type = match.Groups[2].Value;
            }
            else
            {
                var match = Constants.EnumRegex.Match(line);
                type = match.Groups[1].Value;
            }

            if (type.IndexOf('<') > 0)
            {
                type = type.Substring(type.LastIndexOf('<') + 1);
                type = type.Substring(0, type.IndexOf('>'));
            }

            return type;
        }
    }
}
