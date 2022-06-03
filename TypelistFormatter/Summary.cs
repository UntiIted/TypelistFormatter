using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypelistFormatter
{
    public class Summary
    {
        public List<SummaryDataBlocksEntry> DataBlocksEntries { get; set; }
        public List<NestedTypeIndex> NestedTypes { get; set; }

        public Summary()
        {
            DataBlocksEntries = new List<SummaryDataBlocksEntry>();
            NestedTypes = new List<NestedTypeIndex>();
        }

        public void CreateEntry(string header, string path)
        {
            DataBlocksEntries.Add(new SummaryDataBlocksEntry(header, path));
        }

        public void AddDataBlock(string header, string entry)
        {
            DataBlocksEntries.First(x => x.Header == header).DataBlocks.Add(entry);
        }

        public void AddNestedType(NestedTypeIndex type)
        {
            NestedTypes.Add(type);
        }

        public void WriteSummary()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("* [Datablocks](reference/datablocks/README.md)");
            foreach (var entry in DataBlocksEntries)
            {
                sb.AppendLine($"  * [{entry.Header}]({entry.Path}/README.md)");
                foreach (var dbEntry in entry.DataBlocks)
                {
                    sb.AppendLine($"    * [{dbEntry}]({entry.Path}/{dbEntry.ToLower()}.md)");
                }
            }

            sb.AppendLine("* [Nested Types](reference/nested-types/README.md)");
            foreach(var type in NestedTypes)
            {
                sb.AppendLine($"  * [{type.HeaderText}](reference/nested-types/{type.FieldName}.md)");
            }

            File.WriteAllText("Results/SUMMARY.md", sb.ToString());
        }

        public class SummaryDataBlocksEntry
        {
            public string Header { get; set; }
            public string Path { get; set; }
            public List<string> DataBlocks { get; set; }

            public SummaryDataBlocksEntry(string header, string path)
            {
                Header = header;
                Path = path;
                DataBlocks = new List<string>();
            }
        }
    }
}
