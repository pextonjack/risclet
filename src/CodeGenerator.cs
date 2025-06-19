using System;
namespace RISClet_Compiler
{
	/// <summary>
	/// Converts IR to AArch64
	/// </summary>
	public class CodeGenerator
	{
		public string GenerateCode(IRProgram tupleIR)
		{
			string codeHeader = """
// ───────────────────────────────────────────────────────────
// prog.s (Main Program)
// ───────────────────────────────────────────────────────────
""";


            string dataSectionHeader = """
.section .data
""";
            System.Text.StringBuilder dataSectionBuilder = new();
            // Variables
            foreach (KeyValuePair<string, DataType> pair in tupleIR.Variables)
			{
				dataSectionBuilder.Append(Constants.Indentation + IRVariableToAssembly(pair) + '\n');
			}

            string textSectionHeader = """
.section .text
.global _start
_start:
""";
			System.Text.StringBuilder textSectionBuilder = new();
            // Instructions
            for (int i = 0; i < tupleIR.Instructions.Count; i++)
			{
				string[] assemblyInstructions = IRInstructionToAssembly(tupleIR.Instructions[i]);
				foreach (string instruction in assemblyInstructions)
				{
					textSectionBuilder.Append(Constants.Indentation + instruction + '\n');
				}
			}

			System.Text.StringBuilder codeBuilder = new();
			codeBuilder.Append(codeHeader);
            codeBuilder.Append('\n');
            codeBuilder.Append(dataSectionHeader);
            codeBuilder.Append('\n');
            codeBuilder.Append(dataSectionBuilder);
            codeBuilder.Append('\n');
            codeBuilder.Append(textSectionHeader);
            codeBuilder.Append('\n');
            codeBuilder.Append(textSectionBuilder);
            codeBuilder.Append('\n');

            string exit = $"""
{Constants.Indentation}mov x0, #0
{Constants.Indentation}mov w8, #93
{Constants.Indentation}svc #0
""";

            codeBuilder.Append(exit);

            return codeBuilder.ToString();
		}

		// [ASSUMPTION]: Assumes all values are Int32. TODO: Add support for multiple data types (in future stages)
		public static string[] IRInstructionToAssembly(IRInstruction instruction)
		{
			List<string> instructions = new();

            // Case 1: IRVariableDeclarationInstruction
			if (instruction is IRVariableDeclarationInstruction varDeclare)
			{
				// If there's no value given to it, no need to add an instruction for it; it's already in .section .data
				if (varDeclare.Value != null)
				{
					// Need the address, but since it's overwriting it, there's no need to load its value...
					// Then, assign whatever has been given to it
					instructions.Add(LoadVariableAddress(Constants.FirstVarRegister, varDeclare.VarIdent));
                    if (varDeclare.Value is TempDataItem t)
					{
                        instructions.Add(MoveRegisterIntoRegister(Constants.FirstVarRegister + 1, t.tempVarID + Constants.FirstScratchpadRegister));
                    }
					else
					{
                        instructions.Add(MoveLiteralIntoRegister(Constants.FirstVarRegister + 1, varDeclare.Value.IntLiteral.Value));
                    }
					instructions.Add(StoreVariableValue(Constants.FirstVarRegister + 1, Constants.FirstVarRegister));
                }
			}

            // Case 2: IRVariableAssignmentInstruction
            else if (instruction is IRVariableAssignmentInstruction varAssign)
            {
                // Need the address, but since it's overwriting it, there's no need to load its value...
                // Then, assign whatever has been given to it
                instructions.Add(LoadVariableAddress(Constants.FirstVarRegister, varAssign.VarIdent));
                if (varAssign.Value is TempDataItem t)
                {
                    instructions.Add(MoveRegisterIntoRegister(Constants.FirstVarRegister + 1, t.tempVarID + Constants.FirstScratchpadRegister));
                }
                else
                {
                    instructions.Add(MoveLiteralIntoRegister(Constants.FirstVarRegister + 1, varAssign.Value.IntLiteral.Value));
                }
                instructions.Add(StoreVariableValue(Constants.FirstVarRegister + 1, Constants.FirstVarRegister));
            }

            // Case 3: IRSubroutineCallInstruction
            else if (instruction is IRSubroutineCallInstruction subCall)
            {
                // [ASSUMPTION]: Assumes a single parameter, and assumes that the parameter is just a variable (or literal). No temporary variables or complex expressions in subroutine parameters right now

                if (subCall.Parameters[0].Type == DataItemType.Identifier)
                {
                    instructions.Add(LoadVariableAddress(0, subCall.Parameters[0].Identifier));
                    instructions.Add(LoadVariableValue(0, 0));
                }
                else
                {
                    instructions.Add(MoveLiteralIntoRegister(0, subCall.Parameters[0].IntLiteral.Value));
                }

                instructions.Add("bl " + subCall.SubroutineIdent);
            }

            // Case 4: IRBinaryOperationInstruction
            else if (instruction is IRBinaryOperationInstruction binOp)
            {
                instructions.AddRange(BinaryOperationInstruction(binOp));
            }

            return instructions.ToArray();
		}

        // [ASSUMPTION]: This only assumes 4 types of operation: Add, subtract, multiply, and divide. This also assumes no temp registers WITHIN a binary operation (only involvement is in storing results of it)
        public static List<string> BinaryOperationInstruction(IRBinaryOperationInstruction binaryOperation)
        {
            List<string> instructions = new();

            // Best practise: Load the addresses and values of the variables used (if necessary), and load the literals into separate registers
            int nextFreeRegister = Constants.FirstVarRegister;
            int leftReg = 0;
            int rightReg = 0;

            if (binaryOperation.Left.Type == DataItemType.Identifier)
            {
                // Variable
                instructions.Add(LoadVariableAddress(nextFreeRegister, binaryOperation.Left.Identifier));
                instructions.Add(LoadVariableValue(nextFreeRegister + 1, nextFreeRegister));

                leftReg = nextFreeRegister + 1;
                nextFreeRegister += 2;
            }
            else
            {
                // Literal
                instructions.Add(MoveLiteralIntoRegister(nextFreeRegister, binaryOperation.Left.IntLiteral.Value));

                leftReg = nextFreeRegister;
                nextFreeRegister += 1;
            }

            if (binaryOperation.Right.Type == DataItemType.Identifier)
            {
                // Variable
                instructions.Add(LoadVariableAddress(nextFreeRegister, binaryOperation.Right.Identifier));
                instructions.Add(LoadVariableValue(nextFreeRegister + 1, nextFreeRegister));

                rightReg = nextFreeRegister + 1;
                nextFreeRegister += 2;
            }
            else
            {
                // Literal
                instructions.Add(MoveLiteralIntoRegister(nextFreeRegister, binaryOperation.Right.IntLiteral.Value));

                rightReg = nextFreeRegister;
                nextFreeRegister += 1;
            }

            string? operationType = binaryOperation.OpType switch
            {
                BinaryOpType.Add => "add",
                BinaryOpType.Subtract => "sub",
                BinaryOpType.Multiply => "mul",
                BinaryOpType.Divide => "sdiv", // [ASSUMPTION]: Assumes signed division (no generic division instruction in ARMv8)
                _ => null
            };
            if (operationType == null) ErrorReporter.CompilerError("Invalid operation type " + binaryOperation.OpType, (-1, -1));

            string operation = $"{operationType} w{binaryOperation.TempID + Constants.FirstScratchpadRegister}, w{leftReg}, w{rightReg}";
            instructions.Add(operation);

            return instructions;
        }

        public static string MoveRegisterIntoRegister(int dest, int source) => $"mov x{dest}, x{source}";
        public static string MoveLiteralIntoRegister(int dest, int value) => $"mov w{dest}, #{value}";

        public static string LoadVariableAddress(int destReg, string ident) => $"ldr x{destReg}, ={ident}";
        public static string LoadVariableValue(int destReg, int addrReg) => $"ldr w{destReg}, [x{addrReg}]"; // [ASSUMPTION] Int32
        public static string StoreVariableValue(int sourceReg, int addrReg) => $"str w{sourceReg}, [x{addrReg}]"; // [ASSUMPTION] Int32

        public static string IRVariableToAssembly(KeyValuePair<string, DataType> variable)
		{
			return variable.Key + ": " + DataTypeToAssembly(variable.Value) + " " + Constants.DataTypeDefaultValue(variable.Value);
		}

		public static string DataTypeToAssembly(DataType type)
		{
			string? t = type switch
			{
				DataType.Int32 => "word",
                DataType.String => "ascii",
                _ => null
			};
			if (t == null)
			{
				ErrorReporter.CompilerError("Invalid data type!", (-1, -1));
			}
			return t;
		}
	}
}

