using System;
namespace RISClet_Compiler
{
	/// <summary>
	/// Generates IR from ASTs
	/// </summary>
	public class IR
	{
		
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

