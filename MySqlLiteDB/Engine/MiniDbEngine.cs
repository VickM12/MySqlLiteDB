using MySqlLiteDB.Models;
using MySqlLiteDB.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic;
using System.Text;
using System.Threading.Tasks;

namespace MySqlLiteDB.Engine
{
    public class MiniDbEngine
    {
        private TableFile usersTable = new("users");

        public void Execute(string query)
        {
            query = query.Trim().TrimEnd(';');

            if (query.StartsWith("insert", StringComparison.OrdinalIgnoreCase))
            {
                var parts = query.Split('\'');
                string name = parts[1];
                Guid id = Guid.NewGuid();
                usersTable.Insert(new Models.Row { Id = id, Name = name });

                Console.WriteLine("Row Inserted");
            }
            else if (query.StartsWith("select", StringComparison.OrdinalIgnoreCase))
            {
                var whereIndex = query.IndexOf("where", StringComparison.OrdinalIgnoreCase);

                string column = null;
                string value = null;
                if (whereIndex != -1)
                {
                    var whereClause = query[(whereIndex + 5)..].Trim(); //Skip "where"
                    var parts = whereClause.Split('=', 2, StringSplitOptions.TrimEntries);
                    column = parts[0].Trim().ToLowerInvariant();
                    value = parts[1].Trim(' ', '\'', '"', ';');
                }

                var rows = usersTable.SelectAll();
                foreach (var row in rows)
                {
                    if (column == null || Matches(row, column, value))
                    { 
                        Console.WriteLine($"{row.Id} | {row.Name}");
                    }
                }
            }
            else
            {
                Console.WriteLine("Command Unknown");
            }
        }

        private bool Matches(Row row, string column, string value)
        {
            return column switch
            {
                "id" => row.Id.ToString().Equals(value, StringComparison.OrdinalIgnoreCase),
                "name" => row.Name.Trim().Equals(value, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}
