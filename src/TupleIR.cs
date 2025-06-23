using System;
namespace RISClet_Compiler
{
	/// <summary>
	/// Generates IR from ASTs
	/// </summary>
	public class TupleIR
	{
		public TupleIR()
		{
			variables = new();
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
		public IRProgram GenerateTupleIR(ProgramNode programNode)
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
                    // [ASSUMPTION] Currently assumes only a single parameter, in the case of the Output(x) function
                    instructions.Add(new IRSubroutineCallInstruction(subCall.Name, new DataItem[] { ConvertNodeToDataItem(subCall.Arguments[0]) }));
				}
				else
				{
					ErrorReporter.CompilerError("Unexpected expression in program; only variable declaration, assignment, and subroutine calling is allowed here", (-1,-1)); // TODO: Real error location reporting
				}
            }

			return new IRProgram(instructions, variables);//, intermediateVariables);
		}

		public List<IRInstruction> ProcessComplexInstruction(ASTNode node)
		{
			int tempCounter = 0; // Keeps track of number of temporary variables needed at once. Resets to zero between independent code blocks

			List<IRInstruction> instructions = new();

			// Case 1: VariableDeclaration
			if (node is VariableDeclarationNode varDeclare)
			{
				if (!variables.ContainsKey(varDeclare.Identifier))
					variables.Add(varDeclare.Identifier, GetDataType(varDeclare.Type)); // Add to dictionary

                // Case A: variable declaration of no item (e.g. x: Int32;)
                if (varDeclare.Initialiser == null)
				{
                    instructions.Add(new IRVariableDeclarationInstruction(varDeclare.Identifier, GetDataType(varDeclare.Type)));
                }

				// Case B: variable declaration of a single "item" (e.g. x: Int32 = 3;)
				else if (varDeclare.Initialiser is IDataItem dataItem)
				{
					instructions.Add(new IRVariableDeclarationInstruction(varDeclare.Identifier, GetDataType(varDeclare.Type), ConvertNodeToDataItem(varDeclare.Initialiser)));
				}

                // Case C: variable declaration of an expression with 2 "items" (e.g. x: Int32 = y + 3;)
                else if (varDeclare.Initialiser is BinaryExpressionNode binExpression)
                {
					// [ASSUMPTION] This assumes only a maximum of a+b (2 operands)
					// This reserves the intermdiate variable value t1

					instructions.Add(new IRBinaryOperationInstruction(binExpression.Operator, ConvertNodeToDataItem(binExpression.Left), ConvertNodeToDataItem(binExpression.Right), tempCounter));
                    tempCounter++;

                    instructions.Add(new IRVariableDeclarationInstruction(varDeclare.Identifier, GetDataType(varDeclare.Type), new TempDataItem(0)));
                }
            }
            // Case 2: Variable Assignment
            else if (node is AssignmentNode varAssign)
            {
                // Case A: variable assignment of a single "item" (e.g. x = 3;)
                if (varAssign.Expression is IDataItem dataItem)
                {
                    instructions.Add(new IRVariableAssignmentInstruction(varAssign.Identifier, ConvertNodeToDataItem(varAssign.Expression)));
                }

                // Case B: variable assignment of an expression with 2 "items" (e.g. x = y + 3;)
                else if (varAssign.Expression is BinaryExpressionNode binExpression)
                {
                    // [ASSUMPTION] This assumes only a maximum of a+b (2 operands)
                    // This reserves the intermdiate variable value t1

                    instructions.Add(new IRBinaryOperationInstruction(binExpression.Operator, ConvertNodeToDataItem(binExpression.Left), ConvertNodeToDataItem(binExpression.Right), tempCounter));
                    tempCounter++;

                    instructions.Add(new IRVariableAssignmentInstruction(varAssign.Identifier, new TempDataItem(0)));
                }
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

			ErrorReporter.CompilerError("Invalid data item", (-1, -1));
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

	public class IRProgram
	{
		public List<IRInstruction> Instructions { get; set; }
		public Dictionary<string, DataType> Variables { get; set; }

		public IRProgram(List<IRInstruction> instructions, Dictionary<string, DataType> variables)
		{
			Instructions = instructions;
			Variables = variables;
		}

        public override string ToString()
        {
			System.Text.StringBuilder s = new();

			s.Append("Tuple IR: \n");
			for (int i = 0; i < Instructions.Count; i++)
			{
				s.Append(Instructions[i].ToString() + '\n');
			}

			s.Append("\nVariables: \n");
			foreach (KeyValuePair<string, DataType> p in Variables)
			{
				s.Append($"{p.Key}: {p.Value}\n");
			}

			return s.ToString();
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

        public override string ToString()
        {
            if (Value == null)
			{
				return $"(DECLARE, {VarIdent}, {Type})";
            }
			else
			{
                return $"(DECLARE, {VarIdent}, {Type}, {Value})";
            }
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

        public override string ToString()
        {
			return $"(ASSIGN, {VarIdent}, {Value})";
        }
    }

	/*
	public class IRTempAssignmentInstruction : IRInstruction
	{
		public int TempVarID;
		public int IntValue; // Currently only works for integers; AArch64 registers can only store 64-bit values anyway

		public IRTempAssignmentInstruction(int varID, int value)
		{
			TempVarID = varID;
			IntValue = value;
		}
	}
	*/

    public class IRSubroutineCallInstruction : IRInstruction
	{
		public string SubroutineIdent;
		public DataItem[] Parameters;

		public IRSubroutineCallInstruction(string ident, DataItem[] parameters)
		{
			SubroutineIdent = ident;
			Parameters = parameters;
		}

        public override string ToString()
        {
            string args = string.Join(", ", Parameters.Select(p => p.ToString()));
            return $"(CALL, {SubroutineIdent}, {args})";
        }

    }

    public class IRBinaryOperationInstruction : IRInstruction
	{
        public BinaryOpType OpType;
		public DataItem Left;
		public DataItem Right;
		public int TempID;

		public IRBinaryOperationInstruction(BinaryOpType opType, DataItem left, DataItem right, int tempID)
		{
			OpType = opType;
			Left = left;
			Right = right;

			TempID = tempID;
		}

        public override string ToString()
        {
            return $"({OpType.ToString().ToUpper()}, {Left}, {Right}, t{TempID})";
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

        public override string ToString()
        {
            return Type switch
            {
                DataItemType.Identifier => Identifier!,
                DataItemType.IntLiteral => IntLiteral!.Value.ToString(),
                DataItemType.StringLiteral => $"\"{StringLiteral}\"",
                _ => "<UNKNOWN>"
            };
        }
    }

    public class TempDataItem : DataItem
	{
		public int tempVarID;

		public TempDataItem(int id) : base(0)
		{
			tempVarID = id;
		}

        public override string ToString()
        {
            return $"t{tempVarID}";
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

