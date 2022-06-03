using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TypelistFormatter
{
    internal class Constants
    {
        internal const string DataDir = "Data";
        internal const string BlocksCategoriesDataFile = $"{DataDir}/Source/BlocksCategories.json";
        internal const string NewLine = "\r\n";

        internal const string MainCategory = "Main";
        internal const string RarelyEditedCategory = "Rarely Edited";
        internal const string UnusedCategory = "Unused";
        
        internal static readonly Regex GenericRegex = new Regex(@"\b(.+): (.+)"); // type: fieldName
        internal static readonly Regex EnumRegex = new Regex(@"\(enum\) (.+): (.+)"); // (enum) type: fieldName
        internal static readonly Regex IDReferenceRegex = new Regex(@"\((.+?DataBlock)\) (.+): (.+)"); // (datablock) type: fieldName
    }
}
