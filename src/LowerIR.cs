using System;
namespace RISClet_Compiler
{
	public class LowerIR
	{
		public LowerIR()
		{
			variables = new();
		}

        private Dictionary<string, (DataType, string)> variables = new();

        public LowerIRProgram GenerateLowerIR(IRProgram tupleProg)
		{
			List<LowerIRInstruction> pseudoAsm = new();
			variables = new();
			
			foreach (KeyValuePair<string, DataType> pair in tupleProg.Variables)
			{
				variables.Add(pair.Key, (pair.Value, "0"));
			}

            for (int i = 0; i < tupleProg.Instructions.Count; i++)
            {
				IRInstruction current = tupleProg.Instructions[i];

				pseudoAsm.AddRange(ProcessTupleIR(current));
            }

            return new LowerIRProgram(pseudoAsm, variables);
		}

		// [ASSUMPTION]: All literals are treated as integers...
		public List<LowerIRInstruction> ProcessTupleIR(IRInstruction tupleIR)
		{
			List<LowerIRInstruction> lowerIRInstructions = new();

			// Case 1: Variable declaration
			if (tupleIR is IRVariableDeclarationInstruction varDeclare)
			{
				// A: No initialiser
				if (varDeclare.Value == null)
				{
				}

				// B: Literal initialiser
				else if (varDeclare.Value.Type == DataItemType.IntLiteral)
				{
                    variables[varDeclare.VarIdent] = (variables[varDeclare.VarIdent].Item1, varDeclare.Value.IntLiteral.Value.ToString());
                }

				// C: Identifier initialiser
				else if (varDeclare.Value.Type == DataItemType.Identifier)
				{
					// Load variable into SECOND variable register (v1)
					lowerIRInstructions.Add(new VariableLoad(varDeclare.Value.Identifier, new RegisterID(RegisterType.Variable, 1)));

                    // Load address into v0, store value from v1 in [v0] (address of variable)
                    lowerIRInstructions.Add(new VariableStore(varDeclare.VarIdent, new RegisterID(RegisterType.Variable, 0), new RegisterID(RegisterType.Variable, 1)));
                }

				// D: Temp initialiser
				else if (varDeclare.Value is TempDataItem temp)
				{
                    // Load address into v0, store value from specified temp reg in [v0] (address of variable)
                    lowerIRInstructions.Add(new VariableStore(varDeclare.VarIdent, new RegisterID(RegisterType.Variable, 0), new RegisterID(RegisterType.Temp, temp.tempVarID)));
                }
            }

            // Case 2: Variable assignment
            else if (tupleIR is IRVariableAssignmentInstruction varAssign)
            {
                // A: Literal initialiser
                if (varAssign.Value.Type == DataItemType.IntLiteral)
                {
					lowerIRInstructions.Add(new LiteralLoad(new RegisterID(RegisterType.Variable, 1), varAssign.Value.IntLiteral.Value));
                    lowerIRInstructions.Add(new VariableStore(varAssign.VarIdent, new RegisterID(RegisterType.Variable, 0), new RegisterID(RegisterType.Variable, 1)));
                }

                // B: Identifier initialiser
                else if (varAssign.Value.Type == DataItemType.Identifier)
                {
                    // Load variable into SECOND variable register (v1)
                    lowerIRInstructions.Add(new VariableLoad(varAssign.Value.Identifier, new RegisterID(RegisterType.Variable, 1)));
                    lowerIRInstructions.Add(new VariableStore(varAssign.VarIdent, new RegisterID(RegisterType.Variable, 0), new RegisterID(RegisterType.Variable, 1)));
                }

                // C: Temp initialiser
                else if (varAssign.Value is TempDataItem temp)
                {
                    lowerIRInstructions.Add(new VariableStore(varAssign.VarIdent, new RegisterID(RegisterType.Variable, 0), new RegisterID(RegisterType.Temp, temp.tempVarID)));
                }
            }

            // Case 3: Subroutine call
            else if (tupleIR is IRSubroutineCallInstruction subCall)
            {
				// [ASSUMPTION]: Only a single parameter, with only a single ident/literal (no expressions)

				LoadValue(subCall.Parameters[0], 0, RegisterType.Parameter);
				lowerIRInstructions.Add(new SubroutineCall(subCall.SubroutineIdent));
            }

            // Case 4: Binary operation
            else if (tupleIR is IRBinaryOperationInstruction binOp)
            {
				// Operands can be a literal, an ident, or a temp; literals and idents need separate instructions for loading, temps don't
				// So, check if a given operand is a TempDataItem or not

				int currentRegId = 0;

				RegisterID left;
				RegisterID right;

				// Check left
				if (binOp.Left is TempDataItem leftTemp)
				{
					left = new(RegisterType.Temp, leftTemp.tempVarID);
				}
				else // Extra instruction(s) for loading value
				{
					lowerIRInstructions.Add(LoadValue(binOp.Left, currentRegId));

					left = new RegisterID(RegisterType.Variable, currentRegId);
					currentRegId++;
				}

                // Check right
                if (binOp.Right is TempDataItem rightTemp)
                {
                    right = new(RegisterType.Temp, rightTemp.tempVarID);
                }
                else // Extra instruction(s) for loading value
                {
                    lowerIRInstructions.Add(LoadValue(binOp.Right, currentRegId));

                    right = new RegisterID(RegisterType.Variable, currentRegId);
                    currentRegId++;
                }

				lowerIRInstructions.Add(new BinaryOperation(binOp.OpType, new RegisterID(RegisterType.Temp, binOp.TempID), left, right));
            }

            return lowerIRInstructions;
		}

		// [ASSUMPTION]: This function cannot take in TempDataItem
		public LowerIRInstruction LoadValue(DataItem dataItem, int regId, RegisterType regType=RegisterType.Variable)
		{
			// Case 1: Literal
			if (dataItem.Type == DataItemType.IntLiteral)
			{
				return new LiteralLoad(new RegisterID(regType, regId), dataItem.IntLiteral.Value);
			}

            // Case 2: Ident
            if (dataItem.Type == DataItemType.Identifier)
            {
                return new VariableLoad(dataItem.Identifier, new RegisterID(regType, regId));
            }

			return null;
        }
	}

	public class LowerIRProgram
	{
        public List<LowerIRInstruction> Instructions { get; set; }
        public Dictionary<string, (DataType, string)> Variables { get; set; }

        public LowerIRProgram(List<LowerIRInstruction> instructions, Dictionary<string, (DataType, string)> variables)
        {
            Instructions = instructions;
            Variables = variables;
        }
    }

	public abstract class LowerIRInstruction
	{

	}

	public class SubroutineCall : LowerIRInstruction
	{
		public string Ident;

		public SubroutineCall(string ident)
		{
			Ident = ident;
		}
	}

	public class BinaryOperation : LowerIRInstruction
	{
		// Like operation xResult, xLeft, xRight
		public BinaryOpType OpType;
        public RegisterID Result;
        public RegisterID Left;
		public RegisterID Right;

		public BinaryOperation(BinaryOpType opType, RegisterID result, RegisterID left, RegisterID right)
		{
			OpType = opType;
			Result = result;
			Left = left;
			Right = right;
		}
	}

	public class CopyRegister : LowerIRInstruction
    {
		// Like mov xDestination, xSource
		public RegisterID Source;
		public RegisterID Destination;

		public CopyRegister(RegisterID src, RegisterID dest)
		{
			Source = src;
			Destination = dest;
		}
	}

	public class LiteralLoad : LowerIRInstruction
    {
		// Like mov xRegister, #Value
		public RegisterID Register;
		public Int32 Value;

		public LiteralLoad(RegisterID reg, int val)
		{
			Register = reg;
			Value = val;
		}
	}

	public class VariableLoad : LowerIRInstruction
    {
		// Like ldr xRegister, =Ident; ldr xRegister, [xRegister]
		public string Ident;
		public RegisterID Register;

		public VariableLoad(string ident, RegisterID reg)
		{
			Ident = ident;
			Register = reg;
		}
	}

	public class VariableStore : LowerIRInstruction
    {
        // Like ldr xAddressRegister, =Ident; str xSource, [xAddressRegister]
        public string Ident;
		public RegisterID AddressRegister;
        public RegisterID SourceRegister;

		public VariableStore(string ident, RegisterID addrReg, RegisterID srcReg)
		{
			Ident = ident;
			AddressRegister = addrReg;
			SourceRegister = srcReg;
		}
    }

	public class RegisterID
	{
		public int ID;
		public RegisterType Type;

		public RegisterID(RegisterType type, int id)
		{
			ID = id;
			Type = type;
		}
	}

	public enum RegisterType
	{
		Variable,
		Temp,
		Parameter
	}
}

