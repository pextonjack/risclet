using System;
using System.Data.Common;

namespace RISClet_Compiler
{
	/// <summary>
	/// Manages tokenisation
	/// </summary>
	public class Lexer
	{
        #region Identifier and token checks
        static bool IsIdentifierStart(char c)
        {
            return (char.IsLetter(c) || c == '_');
        }
        static bool IsIdentifierPart(char c)
        {
            return (char.IsLetterOrDigit(c) || c == '_');
        }
        // Lookup types of tokens e.g. if, else, Output, etc.
        static TokenType LookupIdentifier(string identifier)
        {
            return identifier switch
            {
                "if" => TokenType.If,
                "elseif" => TokenType.Elseif,
                "else" => TokenType.Else,
                _ => TokenType.Identifier
            };
        }
        #endregion

        public static Token[] Tokenise(string code)
        {
            List<Token> tokens = new();
            int line = 1;
            int column = 1;

            code = code.Replace("\r\n", "\n"); // Normalize line endings

            for (int i = 0; i < code.Length;)
            {
                char current = code[i];

                // Case 1: Newline
                if (current == '\n')
                {
                    line++;
                    column = 1;
                    i++;
                    continue;
                }

                // Case 2: Whitespace (i.e. space)
                if (char.IsWhiteSpace(current))
                {
                    column++;
                    i++;
                    continue;
                }

                // Case 3: Identifier or keyword
                if (IsIdentifierStart(current))
                {
                    int start = i;
                    int startColumn = column;

                    // Iterate through the code until it reaches the end of the identifier
                    while (i < code.Length && IsIdentifierPart(code[i]))
                    {
                        i++;
                        column++;
                    }

                    string ident = code.Substring(start, i - start);
                    TokenType type = LookupIdentifier(ident); // Keyword or Identifier

                    tokens.Add(new Token(type, ident, line, startColumn));
                    continue;
                }

                // Case 4: Check if it's a number (i.e. int)
                if (char.IsDigit(current))
                {
                    int start = i;
                    int startColumn = column;

                    // Iterate through the code until it reaches the end of the int literal
                    while (i < code.Length && char.IsDigit(code[i]))
                    {
                        i++;
                        column++;
                    }

                    string number = code.Substring(start, i - start); // Store as string; parse into int later
                    tokens.Add(new Token(TokenType.IntLiteral, number, line, startColumn));
                    continue;
                }



                // Case to be implemented in later phases: String literal



                // Case 5: Check if it's punctuation/operators/assignment
                TokenType? singleCharType = current switch
                {
                    ':' => TokenType.Colon,
                    ';' => TokenType.Semicolon,
                    '=' => TokenType.Assign,
                    '+' => TokenType.Add,
                    '-' => TokenType.Subtract,
                    '*' => TokenType.Multiply,
                    '/' => TokenType.Divide,
                    '(' => TokenType.LeftParen,
                    ')' => TokenType.RightParen,
                    _ => null
                };
                if (singleCharType != null)
                {
                    tokens.Add(new Token(singleCharType.Value, current.ToString(), line, column));
                    i++;
                    column++;
                    continue;
                }

                // Case 6: Unknown character type (e.g. random Unicode characters)
                tokens.Add(new Token(TokenType.Unknown, current.ToString(), line, column));
                i++;
                column++;
            }

            // Add final EOF token to signify end of file (EOF); helps out parser with knowing when to stop
            tokens.Add(new Token(TokenType.EOF, "", line, column));
            return tokens.ToArray();
        }

    }

    public class Token
	{
		public TokenType Type { get; }
		public string Lexeme { get; }

		// Useful later for error handling & syntax highlighting
		public int Line { get; } // Line number the token starts from
		public int Column { get; } // Column number the token starts from

		public Token(TokenType type, string lexeme, int line, int column)
		{
			Type = type;
			Lexeme = lexeme;
			Line = line;
			Column = column;
		}

        #region Debugging
        public override string ToString()
        {
            return $"[{Type} \"{Lexeme}\" (Line {Line}, Col {Column})]";
        }
        #endregion
    }

    public enum TokenType
	{
		// Values and identifiers
		Identifier,
		IntLiteral,
		StringLiteral,

		// Operators
		Assign,
		Add,
		Subtract,
		Divide,
		Multiply,

		// Punctuation
		Colon,
		Semicolon,
		LeftParen,
		RightParen,

		// Keywords (mostly not useful in Phase 1)
		If, Elseif, Else, While, Output, Return,

		// Misc.
		EOF,
		Unknown
	}
}

