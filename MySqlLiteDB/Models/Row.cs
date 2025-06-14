using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlLiteDB.Models
{
    public class Row
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public string Serialize() => $"{Id} | {Name}";
        public static Row Deserialize(string line)
        {
            var parts = line.Split('|');
            return new Row { Id = Guid.Parse(parts[0]), Name = parts[1] };
        }
    }
}
