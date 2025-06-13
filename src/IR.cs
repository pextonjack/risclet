using System;
namespace RISClet_Compiler
{
	/// <summary>
	/// Generates IR from ASTs
	/// </summary>
	public class IR
	{
		public IR()
		{
			variables = new();
            //intermediateVariables = new();
        }

		private Dictionary<string, DataType> variables;
		//private Dictionary<int, DataType> intermediateVariables;

		/*
		 * If given an expression like x: Int32 = 3 + 4; you cannot simply write this as a single IR instruction
		 * You must instead rewrite it as something like:
		 * t1 = 3 + 4;
		 * x: Int32 = t1
		 * Where t1 functions like an intermediate register for storing temporary values
		 * BUT, you cannot ASSUME that all incoming variable assignment and declaration statements are as such...
		 * ...SO, you must determine whether adding a temporary variable instruction is necessary for every declaration statement
		 */
		public (List<IRInstruction>, Dictionary<string, DataType>/*, Dictionary<int, DataType>*/) GenerateTupleIR(ProgramNode programNode)
		{
			List<IRInstruction> instructions = new();

            // Iterate through all statements in the program, and convert to Tuple IR
            for (int i = 0; i < programNode.Statements.Count; i++)
			{
				ASTNode currentNode = programNode.Statements[i];

				if (currentNode is VariableDeclarationNode varDeclare || currentNode is AssignmentNode varAssign)
				{
					instructions.AddRange(ProcessComplexInstruction(currentNode));
                }
				else if (currentNode is SubroutineCallNode subCall)
				{
					// Currently assumes only a single parameter, in the case of the Output(x) function
					instructions.Add(new IRSubroutineCallInstruction(subCall.Name, new DataItem[] { ConvertNodeToDataItem(subCall.Arguments[0]) }));
				}
				else
				{
					ErrorReporter.CompilerError("Unexpected expression in program; only variable declaration, assignment, and subroutine calling is allowed here", (-1,-1)); // TODO: Real error location reporting
				}
            }

			return (instructions, variables);//, intermediateVariables);
		}

		public List<IRInstruction> ProcessComplexInstruction(ASTNode node)
		{
			List<IRInstruction> instructions = new();

			// Case 1: VariableDeclaration
			if (node is VariableDeclarationNode varDeclare)
			{
                // Case A: variable declaration of no item (e.g. x: Int32;)
                if (varDeclare.Initialiser == null)
				{
                    instructions.Add(new IRVariableDeclarationInstruction(varDeclare.Identifier, GetDataType(varDeclare.Type)));
                }

                // Case B: variable declaration of a single "item" (e.g. x: Int32 = 3;)
                if (varDeclare.Initialiser is IDataItem dataItem)
				{
					instructions.Add(new IRVariableDeclarationInstruction(varDeclare.Identifier, GetDataType(varDeclare.Type), ConvertNodeToDataItem(varDeclare.Initialiser)));
				}

                // Case C: variable declaration of an expression with 2 "items" (e.g. x: Int32 = y + 3;)
                if (varDeclare.Initialiser is BinaryExpressionNode binExpression)
                {
					// This assumes only a maximum of a+b (2 operands)
					// This reserves the intermdiate variable value t1

					// TODO: Deal with temporary variables?

                    instructions.Add(new IRVariableDeclarationInstruction(varDeclare.Identifier, GetDataType(varDeclare.Type), new DataItem("t1", true)));
                }
            }
            // Case 2: Variable Assignment
            else if (node is AssignmentNode varAssign)
            {

            }

            return instructions;
		}

		public static DataItem ConvertNodeToDataItem(ASTNode node)
		{
			if (node is IntegerLiteralNode integer)
			{
				return new DataItem(integer.Value);
			}
            else if (node is IdentifierNode ident)
            {
                return new DataItem(ident.Name, isIdent: true);
            }
			return new DataItem(0);
        }

		public static DataType GetDataType(string type)
		{
			return type switch
			{
				"Int32" => DataType.Int32,
				_ => DataType.Int32
			};
		}
    }

	public abstract class IRInstruction { }

	public class IRVariableDeclarationInstruction : IRInstruction
	{
		public string VarIdent;
		public DataType Type;
		public DataItem? Value;

		public IRVariableDeclarationInstruction(string varIdent, DataType type)
		{
			VarIdent = varIdent;
			Type = type;
			Value = null;
		}
        public IRVariableDeclarationInstruction(string varIdent, DataType type, DataItem value)
        {
            VarIdent = varIdent;
            Type = type;
			Value = value;
        }
    }

    public class IRVariableAssignmentInstruction : IRInstruction
    {
		public string VarIdent;
        public DataItem Value;

		public IRVariableAssignmentInstruction(string ident, DataItem val)
		{
			VarIdent = ident;
			Value = val;
		}
    }

    public class IRSubroutineCallInstruction : IRInstruction
	{
		public string SubroutineIdent;
		public DataItem[] Parameters;

		public IRSubroutineCallInstruction(string ident, DataItem[] parameters)
		{
			SubroutineIdent = ident;
			Parameters = parameters;
		}
	}

	public class IRBinaryOperationInstruction : IRInstruction
	{
        public BinaryOpType OpType;
		public DataItem Left;
		public DataItem Right;

		public IRBinaryOperationInstruction(BinaryOpType opType, DataItem left, DataItem right)
		{
			OpType = opType;
			Left = left;
			Right = right;
		}
	}

	public class DataItem
	{
		public DataItemType Type;
		public string? Identifier;
		public int? IntLiteral;
		public string? StringLiteral;

		public DataItem(int intLiteral)
		{
			Type = DataItemType.IntLiteral;
			IntLiteral = intLiteral;
		}

        public DataItem(string stringItem, bool isIdent)
        {
			if (isIdent)
			{
				Identifier = stringItem;
				Type = DataItemType.Identifier;
			}
			else
			{
                StringLiteral = stringItem;
                Type = DataItemType.StringLiteral;
            }
        }
    }

	public enum DataType
	{
		Int32,
		String,
	}

	public enum DataItemType
	{
		StringLiteral,
		IntLiteral,
		Identifier
	}
}

