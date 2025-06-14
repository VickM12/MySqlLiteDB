using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlLiteDB.Engine.Lexer
{
    public enum TokenKind
    {
        //Structural
        EndOfFile,

        //Single-character symbols
        Asterisk, Comma, Semicolon,
        LeftParen, RightParen, Equal,

        //Literals and Identifiers
        Identifier,
        Number,
        String,

       //Keywords
       Select, From, Where,
       Insert, Into, Values,
       Create, Table, Drop,
       Update
    }
    public sealed record Token(TokenKind Kind, string Lexeme, int Position);
}


