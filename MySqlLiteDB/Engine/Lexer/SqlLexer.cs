using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MySqlLiteDB.Engine.Lexer
{
    public sealed class SqlLexer
    {
        private readonly string _text;
        private int _pos;

        public SqlLexer(string text) => _text = text;

        public Token NextToken()
        {
            SkipWhitespace();
            if (IsEnd)
            {
                return Emit(TokenKind.EndOfFile, string.Empty);
            }
            char c = Peek();

            switch (c)
            {
                case '*': return EmitAndAdvance(TokenKind.Asterisk);
                case ',': return EmitAndAdvance(TokenKind.Comma);
                case ';': return EmitAndAdvance(TokenKind.Semicolon);
                case '(': return EmitAndAdvance(TokenKind.LeftParen);
                case ')': return EmitAndAdvance(TokenKind.RightParen);
                case '=': return EmitAndAdvance(TokenKind.Equal);
            }
            // string literal
            if (c is '\'' or '"')
                return ReadQuotedString();

            // number literal
            if (char.IsDigit(c))
                return ReadNumber();

            // identifier / keyword
            if (char.IsLetter(c) || c == '_')
                return ReadIdentifierOrKeyword();

            // unknown rune
            throw new InvalidOperationException(
                $"Unexpected character '{c}' at position {_pos}.");
        }
        bool IsEnd => _pos >= _text.Length;
        char Peek(int lookahead = 0) => _pos + lookahead < _text.Length ? _text[_pos + lookahead] : '\0';
        char Next() => IsEnd ? '\0' : _text[_pos++];
        void SkipWhitespace() { while (!IsEnd && char.IsWhiteSpace(Peek())) _pos++; }

        Token Emit(TokenKind kind, string lexeme) => new(kind, lexeme, _pos);
        Token EmitAndAdvance(TokenKind kind)
        {
            char c = Next();
            return new Token(kind, c.ToString(), _pos - 1);
        }

        Token ReadQuotedString()
        {
            char quote = Next(); // consume opening quote
            var sb = new StringBuilder();
            while (!IsEnd && Peek() != quote)
            {
                // rudimentary escape handling: doubled quotes inside strings
                if (Peek() == '\\' && Peek(1) == quote)
                {
                    _pos += 2;
                    sb.Append(quote);
                    continue;
                }
                sb.Append(Next());
            }
            if (IsEnd) throw new InvalidOperationException("Unterminated string literal.");
            Next(); // consume closing quote
            return new Token(TokenKind.String, sb.ToString(), _pos - sb.Length - 2);
        }

        Token ReadNumber()
        {
            int start = _pos;
            while (char.IsDigit(Peek())) _pos++;
            return new Token(TokenKind.Number, _text[start.._pos], start);
        }

        Token ReadIdentifierOrKeyword()
        {
            int start = _pos;
            while (char.IsLetterOrDigit(Peek()) || Peek() == '_') _pos++;
            string lexeme = _text[start.._pos];
            return new Token(MapKeyword(lexeme), lexeme, start);
        }

        static TokenKind MapKeyword(string lexeme) => lexeme.ToLowerInvariant() switch
        {
            "select" => TokenKind.Select,
            "from" => TokenKind.From,
            "where" => TokenKind.Where,
            "insert" => TokenKind.Insert,
            "into" => TokenKind.Into,
            "values" => TokenKind.Values,
            "create" => TokenKind.Create,
            "table" => TokenKind.Table,
            "drop" => TokenKind.Drop,
            "update" => TokenKind.Update,
            "delete" => TokenKind.Delete,
            "join" => TokenKind.Join,
            _ => TokenKind.Identifier
        };
    };
}

    
