using System;
using System.Linq.Expressions;

namespace RISClet_Compiler
{
    /// <summary>
    /// Parses tokens into ASTs
    /// [ASSUMPTION] This is currently NOT a recursive descent parser, although that would be a good idea
    /// </summary>
    public class Parser
	{
		// 1. Iterate through tokens to produce an 'expression' (series of tokens ended by a semicolon)
        // 2. Parse expression
        // 3. Add to program node

        public ProgramNode Parse(Token[] tokens)
        {
            ProgramNode programNode = new();

            List<Token> current = new();

            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i].Type == TokenType.Semicolon)
                {
                    // Current expression is done; parse the current expression
                    if (current.Count == 0) ErrorReporter.CompilerError("Empty statement", (tokens[i].Line, tokens[i].Column));
                    programNode.Statements.Add(ParseStatement(current)); // EXCLUDES SEMICOLON
                    
                    current.Clear();
                    continue;
                }
                else
                {
                    current.Add(tokens[i]);
                }
            }

            return programNode;
        }

        // For phase 1, this only works for one-line simple statements (i.e. no code blocks (subroutines, if-else statements, etc.), no complex arithmetic operations (e.g. x = 3 * 4 - 5))
        // [ASSUMPTION] This assumes that there IS NOT a semicolon on the end of statements
        public ASTNode ParseStatement(List<Token> tokens)
        {
            if (tokens.Count == 0) ErrorReporter.CompilerError("Empty statement", (-1, -1)); // -1, -1 because there IS no token to extract line and column number from

            ASTNode? returnVal = null;

            // Case 1: Variable declaration ([identifier] [colon] [type] [assignment] [expression for value], or [identifier] [colon] [type])
            if (tokens.Count >= 3 && tokens[0].Type == TokenType.Identifier && tokens[1].Type == TokenType.Colon && tokens[2].Type == TokenType.Identifier)
            {
                // Case A: With assignment
                if (tokens.Count >= 4 && tokens[3].Type == TokenType.Assign)
                    returnVal = new VariableDeclarationNode(tokens[0].Lexeme, tokens[2].Lexeme, ParseExpression(tokens[4..(tokens.Count)]));

                // Case B: Without assignment
                else
                    returnVal = new VariableDeclarationNode(tokens[0].Lexeme, tokens[2].Lexeme);
            }

            // Case 2: Variable assignment ([identifier] [assignment] [expression for value])
            else if (tokens.Count >= 3 && tokens[0].Type == TokenType.Identifier && tokens[1].Type == TokenType.Assign)
            {
                returnVal = new AssignmentNode(tokens[0].Lexeme, ParseExpression(tokens[2..(tokens.Count)]));
            }

            // Case 3: Subroutine call (only Output(val) for Phase 1) ([identifier] [leftparenth] [...] [rightparenth])
            else if (tokens.Count >= 2 && tokens[0].Type == TokenType.Identifier && tokens[1].Type == TokenType.LeftParen && tokens[^1].Type == TokenType.RightParen)
            {
                // This doesn't work for nested commas or brackets (e.g. Procedure((3,4), 5); it's a tuple, which is both nested brackets AND nested commas)
                List<Token> currentParam = new();
                List<ASTNode> parameters = new();

                for (int i = 2; i < tokens.Count - 1; i++)
                {
                    if (tokens[i].Type == TokenType.Comma)
                    {
                        // Current expression is done; parse the current expression
                        if (currentParam.Count == 0) ErrorReporter.CompilerError("Empty argument", (tokens[i].Line, tokens[i].Column));
                        parameters.Add(ParseExpression(currentParam)); // EXCLUDES SEMICOLON

                        currentParam.Clear();
                        continue;
                    }
                    else
                    {
                        currentParam.Add(tokens[i]);
                    }
                }

                if (currentParam.Count > 0)
                    parameters.Add(ParseExpression(currentParam));

                returnVal = new SubroutineCallNode(tokens[0].Lexeme, parameters);
            }

            if (returnVal == null)
                ErrorReporter.CompilerError("Unrecognised statement structure!", (tokens[0].Line, tokens[0].Column));

            return returnVal;
        }

        // Typically used for binary expressions e.g. 3 + 4; only works for basic '<left> operator <right>' expressions for Phase 1
        public ASTNode ParseExpression(List<Token> tokens)
        {
            // Special case: Expression is only of a single item
            if (tokens.Count == 1)
            {
                return ParseAtomic(tokens[0]);
            }

            if (tokens.Count == 2)
            {
                ErrorReporter.CompilerError($"Malformed expression: ({tokens[0]}, {tokens[1]})", (tokens[0].Line, tokens[0].Column));
            }

            BinaryOpType? op = tokens[1].Type switch
            {
                TokenType.Add => BinaryOpType.Add,
                TokenType.Subtract => BinaryOpType.Subtract,
                TokenType.Multiply => BinaryOpType.Multiply,
                TokenType.Divide => BinaryOpType.Divide,
                _ => null
            };

            if (op == null) ErrorReporter.CompilerError("Invalid operator", (tokens[0].Line, tokens[0].Column));

            ASTNode left = ParseAtomic(tokens[0]);
            ASTNode right = ParseAtomic(tokens[2]);

            return new BinaryExpressionNode(op.Value, left, right);
        }

        private ASTNode ParseAtomic(Token token)
        {
            if (token.Type == TokenType.Identifier)
            {
                return new IdentifierNode(token.Lexeme);
            }
            else
            {
                return new IntegerLiteralNode(int.Parse(token.Lexeme));
            }
        }
    }

    #region Abstract Syntax Tree Classes

    public abstract class ASTNode
    {
        
    }


    // Program head
    public class ProgramNode : ASTNode
    {
        public List<ASTNode> Statements { get; set; } = new();
    }
    
    public class VariableDeclarationNode : ASTNode
    {
        public string Identifier { get; set; }
        public string Type { get; set; }
        public ASTNode? Initialiser { get; set; }

        public VariableDeclarationNode(string ident, string type)
        {
            Identifier = ident;
            Type = type;
        }

        public VariableDeclarationNode(string ident, string type, ASTNode initialiser)
        {
            Identifier = ident;
            Type = type;
            Initialiser = initialiser;
        }
    }

    public class AssignmentNode : ASTNode
    {
        public string Identifier { get; set; }
        public ASTNode Expression { get; set; }

        public AssignmentNode(string ident, ASTNode exp)
        {
            Identifier = ident;
            Expression = exp;
        }
    }

    // Arithmetic operations, bitwise operations, boolean logic operations, etc.
    public class BinaryExpressionNode : ASTNode
    {
        public BinaryOpType Operator { get; set; }  // "Add", "Subtract", etc.
        public ASTNode Left { get; set; }
        public ASTNode Right { get; set; }

        public BinaryExpressionNode(BinaryOpType op, ASTNode left, ASTNode right)
        {
            Operator = op;
            Left = left;
            Right = right;
        }
    }
    public enum BinaryOpType { Add, Subtract, Multiply, Divide }

    public class IntegerLiteralNode : ASTNode, IDataItem
    {
        public int Value { get; set; }

        public IntegerLiteralNode(int val)
        {
            Value = val;
        }
    }

    public class IdentifierNode : ASTNode, IDataItem
    {
        public string Name { get; set; }

        public IdentifierNode(string name)
        {
            Name = name;
        }
    }

    public class SubroutineCallNode : ASTNode
    {
        public string Name { get; set; }
        public List<ASTNode> Arguments { get; set; } = new();

        public SubroutineCallNode(string name)
        {
            Name = name;
        }

        public SubroutineCallNode(string name, List<ASTNode> arguments)
        {
            Name = name;
            Arguments = arguments;
        }
    }

    // Useful for generalising items of data used in expression e.g. binary operations
    public interface IDataItem { }

    #endregion
}

