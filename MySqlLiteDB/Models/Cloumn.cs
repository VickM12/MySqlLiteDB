using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlLiteDB.Models
{
    public enum ColType { Guid, Text, Int}
    public sealed record Column(string Name, ColType Type)
    {
    }

    public sealed class DynamicRow
    {
        public Dictionary<string, object> Cells { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string Serialize(IEnumerable<Column> cols)
            => string.Join('|', cols.Select(c => Cells[c.Name]));

        public static DynamicRow Deserialize(string line, IEnumerable<Column> cols)
        {
            var parts = line.Split('|');
            var row = new DynamicRow();
            int i = 0;
            foreach(var col in cols)
            {
                object val = col.Type switch
                {
                    ColType.Guid => Guid.Parse(parts[i]),
                    ColType.Int => int.Parse(parts[i]),
                    _ => parts[i]
                };
                row.Cells[col.Name] = val;
                i++;
            }
            return row;
        }
    }
}
