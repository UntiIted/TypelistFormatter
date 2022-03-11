using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypelistFormatter
{
    internal class NestedTypeIndex
    {
        public string File { get; set; }
        public string FieldName { get; set; }
        public int StartLineIndex { get; set; }
        public int EndLineIndex { get; set; }
        public int WhiteSpaceCount { get; set; }

        public static void FindNestedTypesInFile(List<NestedTypeIndex> nestedTypes, string fileName, string[] lines, ref int i, int whiteSpaceCount)
        {
            var field = "";
            var start = -1;

            while (i < lines.Length - 1)
            {
                if (!lines[i + 1].StartsWith(' ') && whiteSpaceCount == 0) // ignore outermost layer if it's not about to enter a nested type
                {
                    i++;
                    continue;
                }

                if (field == "" && whiteSpaceCount != 0) // outermost layer is ignored, it's only for the sake of going over the whole file
                {
                    start = i; // this is the first nested line
                    field = TypelistFormatter.GetRealTypeFromLine(lines[i - 1]); // if this is the first nested line, the previous line must contain the source field
                }

                int count = lines[i + 1].TakeWhile(Char.IsWhiteSpace).Count(); // typelist marks nests using whitespaces

                if (count < whiteSpaceCount) // if the whitespace count decreased, we moved out of a nested type
                {
                    if (!nestedTypes.Any(x => x.FieldName == field)) // nested types each have their page, no need to repeat generation for the same type
                    {
                        nestedTypes.Add(new NestedTypeIndex
                        {
                            File = fileName,
                            FieldName = field,
                            StartLineIndex = start,
                            EndLineIndex = i,
                            WhiteSpaceCount = whiteSpaceCount
                        });
                    }

                    return;
                }
                else if (count > whiteSpaceCount) // if the whitespace count increased, we moved into another nested type
                {
                    i++; // we're looking ahead - the next recursion needs to start from the first new nested line
                    FindNestedTypesInFile(nestedTypes, fileName, lines, ref i, count);
                }
                else
                    i++;
            }
        }
    }
}
