using MySqlLiteDB.Engine.Lexer;
using MySqlLiteDB.Models;
using MySqlLiteDB.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Quic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MySqlLiteDB.Engine
{
    public class MiniDbEngine
    {
        private readonly Dictionary<string, TableFile> tables =
             new(StringComparer.OrdinalIgnoreCase);

        public MiniDbEngine()
        {
            // 1. Ensure the data folder exists
            Directory.CreateDirectory("Data");

            // 2. Scan every *.tbl file and register it
            foreach (var file in Directory.EnumerateFiles("Data", "*.tbl"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var header = File.ReadLines(file).FirstOrDefault() ?? "";

                if (!header.Contains(':'))
                {
                    var fallbackSchema = new[]
                    {
                        new Column("id",   ColType.Guid),
                        new Column("name", ColType.Text)
                    };

                    tables[name] = new TableFile(name, fallbackSchema); // TableFile will
                                                                        // prepend the header
                    continue;
                }

                // each header looks like: "id:Guid|name:Text|age:Int"
                var columns = header.Split('|').Select(part =>
                {
                    var bits = part.Split(':');
                    return new Column(bits[0],
                        Enum.Parse<ColType>(bits[1], ignoreCase: true));
                });

                tables[name] = new TableFile(name, columns);
            }

            // 3. Guarantee a default users table if none exists
            if (!tables.ContainsKey("users"))
            {
                tables["users"] = new TableFile("users",
                    new[]
                    {
                        new Column("id",   ColType.Guid),
                        new Column("name", ColType.Text)
                    });
            }
        }

        public void Execute(string query)
        {
            try
            {
                var lexer = new SqlLexer(query);
                var tokens = new List<Token>();
                Token token;
                do { token = lexer.NextToken(); tokens.Add(token); }
                while (token.Kind != TokenKind.EndOfFile);

                var cursor = new Cursor(tokens);

                switch (cursor.Peek().Kind)
                {
                    case TokenKind.Create:
                        ParseCreateTable(cursor);
                        break;

                    case TokenKind.Delete:
                        ParseDelete(cursor);
                        break;

                    case TokenKind.Insert:
                        ParseInsert(cursor);
                        break;

                    case TokenKind.Select:
                        ParseSelect(cursor);
                        break;

                    default:
                        Fail($"Unknown command: {cursor.Peek().Lexeme}");
                        break;
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Syntax Error: {ex.Message}");
            }
        }

        private void ParseCreateTable(Cursor cursor)
        {
            cursor.Expect(TokenKind.Create);
            cursor.Expect(TokenKind.Table);
            var name = cursor.Expect(TokenKind.Identifier).Lexeme;

            cursor.Expect(TokenKind.LeftParen);
            var columns = new List<Column>();

           while(true)
            {
                var colName = cursor.Expect(TokenKind.Identifier).Lexeme;
                var typeTok = cursor.Expect(TokenKind.Identifier).Lexeme.ToLowerInvariant();
                var colType = typeTok switch
                {
                    "guid" => ColType.Guid,
                    "text" => ColType.Text,
                    "int" => ColType.Int,
                    _ => throw new InvalidOperationException($"Unknown type {typeTok}")
                };
                columns.Add(new Column(colName, colType));

                if (cursor.TryConsume(TokenKind.Comma)) continue;
                cursor.Expect(TokenKind.RightParen); break;
            }

            tables[name] = new TableFile(name, columns);
            Console.WriteLine($"Table '{name}' created with {columns.Count} columns.");
        }

        private void ParseInsert(Cursor c)
        {
            c.Expect(TokenKind.Insert);
            c.Expect(TokenKind.Into);
            var name = c.Expect(TokenKind.Identifier).Lexeme;

            if (!tables.TryGetValue(name, out var table))
                Fail($"Table '{name}' not found.");

            List<string> colOrder;
            if (c.TryConsume(TokenKind.LeftParen))
            { 
                colOrder = new();
                while (true)
                {
                    colOrder.Add(c.Expect(TokenKind.Identifier).Lexeme);
                    if (c.TryConsume(TokenKind.Comma))
                    {
                        continue;
                    }
                    c.Expect(TokenKind.RightParen);
                    break;
                }
            }
            else
            {
                colOrder = table.Schema.Select(col  => col.Name).ToList();
            }

            c.Expect(TokenKind.Values);
            c.Expect(TokenKind.LeftParen);
            var row = new DynamicRow();
            int colIdx = 0;

            while (true)
            {
                if (colIdx >= colOrder.Count)
                    Fail("Too many values supplied.");


                var colName = colOrder[colIdx];
                var colInfo = table.Schema.First(x => x.Name.Equals(colName, StringComparison.OrdinalIgnoreCase));
                object val = colInfo.Type switch
                {
                    ColType.Guid => Guid.Parse(c.Expect(TokenKind.String).Lexeme),
                    ColType.Int => int.Parse(c.ExpectLiteralNumber().Lexeme),
                    _ => c.Expect(TokenKind.String).Lexeme
                };
                row.Cells[colName] = val;

                colIdx++;
                if (c.TryConsume(TokenKind.Comma)) continue;
                c.Expect(TokenKind.RightParen); break;
            }

            if (colIdx != colOrder.Count)
                Fail("Column/Value count mismatch.");
            foreach (var col in table.Schema)
                if (!row.Cells.ContainsKey(col.Name))
                    row.Cells[col.Name] = col.Type switch
                    {
                        ColType.Guid => Guid.NewGuid(),
                        ColType.Int => 0,
                        _ => string.Empty
                    };

            table.Insert(row);
            Console.WriteLine("Row inserted into " + name);
        }

        private void ParseSelect(Cursor c)
        {
            c.Expect(TokenKind.Select);
            c.Expect(TokenKind.Asterisk);         // only * for now
            c.Expect(TokenKind.From);
            var name = c.Expect(TokenKind.Identifier).Lexeme;

            if (!tables.TryGetValue(name, out var table))
                Fail($"No such table {name}");

            foreach (var row in table.SelectAll())
                Console.WriteLine(string.Join(" | ",
                    table.Schema.Select(col => row.Cells[col.Name])));
        }

        private void ParseDelete(Cursor c)
        {
            c.Expect(TokenKind.Delete);
            c.Expect(TokenKind.From);
            var tableName = c.Expect(TokenKind.Identifier).Lexeme;

            if (!tables.TryGetValue(tableName, out var table))
                Fail($"No such table {tableName}");

            Func<DynamicRow, bool> predicate = _ => true;
            if(c.TryConsume(TokenKind.Where))
            {
                var colName = c.Expect(TokenKind.Identifier).Lexeme;
                c.Expect(TokenKind.Equal);

                var colInfo = table.Schema.First(s => s.Name.Equals(colName, StringComparison.OrdinalIgnoreCase));

                object wVal = colInfo.Type switch
                {
                    ColType.Guid => Guid.Parse(c.Expect(TokenKind.String).Lexeme),
                    ColType.Int => int.Parse(c.ExpectLiteralNumber().Lexeme),
                    _ => c.Expect(TokenKind.String).Lexeme
                };

                predicate = row => row.Cells[colName].Equals(wVal);
            }

            int removed = table.DeleteRows(predicate);
            Console.WriteLine($"{removed} row(s) deleted from {tableName}");
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
        private static void Fail(string msg) => throw new InvalidOperationException(msg);

    }
    internal sealed class Cursor
    {
        private readonly IList<Token> _t; private int _i;
        public Cursor(IList<Token> t) => _t = t;
        public Token Peek(int look = 0) => _i + look < _t.Count ? _t[_i + look] : _t[^1];
        public Token Advance() => _t[_i++];
        public Token Expect(TokenKind k)
        {
            if (Peek().Kind != k) throw new InvalidOperationException($"Expected {k}, got {Peek().Kind}");
            return Advance();
        }
        public bool TryConsume(TokenKind k)
        {
            if (Peek().Kind != k) return false;
            Advance(); return true;
        }

        public Token ExpectLiteralNumber()
        {
            if (Peek().Kind != TokenKind.Number)
                throw new InvalidOperationException($"Expected number, got {Peek().Kind}");
            return Advance();
        }
    }
}
