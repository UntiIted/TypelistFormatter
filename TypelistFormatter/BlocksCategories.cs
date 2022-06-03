using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace TypelistFormatter
{
    public class BlocksCategories
    {
        private static BlocksCategories currentData;

        public static BlocksCategories Data
        {
            get
            {
                if (currentData == null)
                    currentData = Load();

                return currentData;
            }
        }

        public List<string> Main { get; set; }
        public List<string> RarelyEdited { get; set; }
        public List<string> Unused { get; set; }

        public static BlocksCategories Load()
        {
            string content = File.ReadAllText(Constants.BlocksCategoriesDataFile);
            return JsonSerializer.Deserialize<BlocksCategories>(content);
        }

        public string GetCategory(string dataBlock)
        {
            if (Unused.Contains(dataBlock))
            {
                return Constants.UnusedCategory;
            }
            else if (RarelyEdited.Contains(dataBlock))
            {
                return Constants.RarelyEditedCategory;
            }
            else if (!Main.Contains(dataBlock))
            {
                Console.WriteLine($"WARNING: no categories matched for {dataBlock}");
            }

            return Constants.MainCategory;
        }

        public string GetCategoryDirectory(string category)
        {
            switch (category)
            {
                case Constants.UnusedCategory:
                    return "unused";
                case Constants.RarelyEditedCategory:
                    return "rarely-edited";
                case Constants.MainCategory:
                    return "main";
                default:
                    Console.WriteLine($"WARNING: no directory matched for {category}");
                    return "main";
            }
        }

        public string GetLink(string dataBlock, bool fromDataBlock = true)
        {
            StringBuilder link = new StringBuilder();

            link.Append("../");

            if (!fromDataBlock)
            {
                link.Append("datablocks/");
            }

            link.Append(GetCategoryDirectory(GetCategory(dataBlock)));

            link.Append($"/{dataBlock.Replace("DataBlock", string.Empty).ToLower()}.md");

            return link.ToString();
        }
    }
}
