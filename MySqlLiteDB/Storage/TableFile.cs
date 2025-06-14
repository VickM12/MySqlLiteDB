using MySqlLiteDB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlLiteDB.Storage
{
    public class TableFile
    {
        private readonly string path;
        public TableFile(string tableName)
        {
            path = $"Data/{tableName}.tbl";
            Directory.CreateDirectory("Data");
            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
            }
        }

        public void Insert(Row row)
        {
            File.AppendAllText(path, row.Serialize() + "\n");
        }

        public List<Row> SelectAll()
        {
            var lines = File.ReadAllLines(path);
            var result = new List<Row>();
            foreach (var line in lines)
            {
                if(!string.IsNullOrWhiteSpace(line))
                {
                    result.Add(Row.Deserialize(line));
                }
            }
            return result;
        }
    }
}
