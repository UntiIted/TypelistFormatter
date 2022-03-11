using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TypelistFormatter
{
    internal class EnumFormatter
    {
        static string dataDir = @"Data\Enums";
        static string newLine = "\r\n";

        public static void Run()
        {
            var files = Directory.GetFiles(dataDir);

            Directory.CreateDirectory("Results");

            List<string> output = new();
            CreateHeader(output);

            foreach (var file in files)
            {
                Console.WriteLine(file);

                var name = new Regex(@"\\(\w+)\.txt").Match(file).Groups[1].Value;

                ProcessFile(output, name, File.ReadAllLines(file));
            }

            File.WriteAllLines($"Results/enum-types.md", output);
        }

        static void CreateHeader(List<string> output)
        {
            output.Add("---");
                output.Add($"description: A list of every enum type related to the game's Datablocks");
            output.Add("---");

            output.Add($"{newLine}# Enum Types{newLine}");

            output.Add("No description provided." + newLine);

            output.Add("***" + newLine);

            output.Add("## Enums");
        }

        static void ProcessFile(List<string> output, string enumName, string[] lines)
        {
            output.Add($"{newLine}### {enumName}{newLine}");

            foreach(var line in lines)
            {
                output.Add($"* {line}");
            }
        }
    }
}
