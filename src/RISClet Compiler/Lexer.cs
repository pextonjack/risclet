using System;

namespace RISClet_Compiler
{
	/// <summary>
	/// Manages tokenisation
	/// </summary>
	public class Lexer
	{
		
	}

	public class Token
	{
		public TokenType Type { get; }
		public string Lexeme { get; }

		// Useful later for error handling & syntax highlighting
		public int Line { get; }
		public int Column { get; }

		public Token(TokenType type, string lexeme, int line, int column)
		{
			Type = type;
			Lexeme = lexeme;
			Line = line;
			Column = column;
		}
	}

	public enum TokenType
	{
		// Values and identifiers
		Identifier,
		IntLiteral,
		StringLiteral,

		// Operators
		Assign,
		Plus,
		Minus,
		Divide,
		Multiply,

		// Punctuation
		Colon,
		Semicolon,
		LeftParenth,
		RightParenth,

		// Keywords (mostly not useful in Phase 1)
		If, Elseif, Else, While, Output, Return,

		// Misc.
		EOF,
		Unknown
	}
}

