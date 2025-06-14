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

namespace MySqlLiteDB.Engine
{
    public class MiniDbEngine
    {
        private TableFile usersTable = new("users");

        public void Execute(string query)
        {
            var lexer = new SqlLexer(query);
            var tokens = new List<Token>();
            Token token;
            do { token = lexer.NextToken(); tokens.Add(token); }
            while (token.Kind != TokenKind.EndOfFile);

            var cursor = new Cursor(tokens);

            switch (cursor.Peek().Kind)
            {
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

        private void ParseInsert(Cursor c)
        {
            c.Expect(TokenKind.Select);
            if (c.Peek().Kind == TokenKind.Asterisk)
            {
                c.Advance();
            }
            else
            {
                Fail("Only '*' column list supported.");
            }

            c.Expect(TokenKind.Values);
            c.Expect(TokenKind.LeftParen);
            var nameToken = c.Expect(TokenKind.String);
            c.Expect(TokenKind.RightParen);

            usersTable.Insert(new Row { Id = Guid.NewGuid(), Name = nameToken.Lexeme });
            Console.WriteLine("Row inserted.");

        }

        private void ParseSelect(Cursor c)
        {
            c.Expect(TokenKind.Select);
            if (c.Peek().Kind == TokenKind.Asterisk) c.Advance();   // ignore columns for now
            else Fail("Only '*' column list supported.");

            c.Expect(TokenKind.From);
            var table = c.Expect(TokenKind.Identifier).Lexeme;

            if (!table.Equals("users", StringComparison.OrdinalIgnoreCase))
                Fail("Only 'users' table is implemented.");

            string whereColumn = null;
            string whereValue = null;

            if (c.TryConsume(TokenKind.Where))
            {
                whereColumn = c.Expect(TokenKind.Identifier).Lexeme.ToLowerInvariant();
                c.Expect(TokenKind.Equal);
                whereValue = c.Expect(TokenKind.String).Lexeme;
            }

            foreach (var row in usersTable.SelectAll())
            {
                if (whereColumn == null || Matches(row, whereColumn, whereValue))
                    Console.WriteLine($"{row.Id} | {row.Name}");
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
    }
}
