using MySqlLiteDB.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlLiteDB.Storage
{
    public class TableFile
    {
        private readonly string path;
        private readonly bool hasHeader;

        public List<Column> Schema { get; }

        public TableFile(string name, IEnumerable<Column> schema)
        {
            Schema = schema.ToList();
            Directory.CreateDirectory("Data");
            path = Path.Combine("Data", $"{name}.tbl");

            if (!File.Exists(path))
            {
                WriteHeader();
                hasHeader = true;
            }
            else
            {
                // peek first line
                var first = File.ReadLines(path).FirstOrDefault() ?? "";
                hasHeader = first.Contains(':');

                // if missing, prepend header & keep data
                if (!hasHeader)
                {
                    var all = File.ReadAllText(path);
                    WriteHeader();
                    File.AppendAllText(path, all);   // restore rows
                    hasHeader = true;
                }

            }
        }

        public void Insert(DynamicRow row)
        {
            File.AppendAllText(path, row.Serialize(Schema) + "\n");
        }

        public IEnumerable<DynamicRow> SelectAll()
            => File.ReadLines(path).Skip(1)   // skip header
              .Where(l => l.Length > 0)
              .Select(l => DynamicRow.Deserialize(l, Schema));

        public int DeleteRows(Func<DynamicRow, bool> predicate)
        {
            var all = SelectAll().ToList();
            var keep = all.Where(r => !predicate(r)).ToList();
            int nGone = all.Count - keep.Count;

            if (nGone == 0)
            {
                return 0;
            }

            var sb = new StringBuilder(
                string.Join('|', Schema.Select(c => $"{c.Name}:{c.Type}")) + Environment.NewLine);

            foreach (var r in keep)
            {
                sb.AppendLine(r.Serialize(Schema));
            }
            File.WriteAllText(path, sb.ToString());
            return nGone;
        }

        private void WriteHeader()
        {
            var header = string.Join('|', Schema.Select(c => $"{c.Name}:{c.Type}"));
            File.WriteAllText(path, header + Environment.NewLine);
        }
    }
}
