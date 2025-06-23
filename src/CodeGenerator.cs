using System;
namespace RISClet_Compiler
{
	/// <summary>
	/// Converts IR to AArch64
	/// </summary>
	public class CodeGenerator
	{
		public string GenerateCode(LowerIRProgram ir)
		{
			string codeHeader = """
// ───────────────────────────────────────────────────────────
// prog.s (Main Program)
// Auto-compiled by the RISClet compiler
// For more information, see https://github.com/pextonjack/risclet
// ───────────────────────────────────────────────────────────
""";


            string dataSectionHeader = """
.section .data
""";
            System.Text.StringBuilder dataSectionBuilder = new();
            // Variables
            foreach (KeyValuePair<string, (DataType, string)> pair in ir.Variables)
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
            for (int i = 0; i < ir.Instructions.Count; i++)
			{
				string[] assemblyInstructions = IRInstructionToAssembly(ir.Instructions[i]);
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
		public static string[] IRInstructionToAssembly(LowerIRInstruction instruction)
		{
			List<string> instructions = new();

			// Case 1: Subroutine Call
			if (instruction is SubroutineCall subCall)
			{
				instructions.Add($"bl {SubroutineIdentifier(subCall.Ident)}");
			}

			// Case 2: Binary Operation
			else if (instruction is BinaryOperation binOp)
			{
				// [ASSUMPTION]: Only these 4 operations are allowed
				string op = binOp.OpType switch
				{
					BinaryOpType.Add => "add",
                    BinaryOpType.Subtract => "sub",
                    BinaryOpType.Multiply => "mul",
                    BinaryOpType.Divide => "sdiv",
                };

				instructions.Add($"{op} x{ProcessRegisterID(binOp.Result)}, x{ProcessRegisterID(binOp.Left)}, x{ProcessRegisterID(binOp.Right)}");
			}

			// Case 3: Copying Registers (mov)
			else if (instruction is CopyRegister regCopy)
			{
				instructions.Add($"mov x{ProcessRegisterID(regCopy.Destination)}, x{ProcessRegisterID(regCopy.Source)}");
			}

			// Case 4: Loading Literals (mov with #literal)
			else if (instruction is LiteralLoad litLoad)
			{
                instructions.Add($"mov x{ProcessRegisterID(litLoad.Register)}, #{litLoad.Value}");
            }

			// Case 5: Loading Variables (ldr)
			else if (instruction is VariableLoad varLoad)
			{
                instructions.Add($"ldr x{ProcessRegisterID(varLoad.Register)}, ={varLoad.Ident}");
                //instructions.Add($"ldr w{ProcessRegisterID(varLoad.Register)}, [x{ProcessRegisterID(varLoad.Register)}]");
				instructions.Add($"ldrsw x{ProcessRegisterID(varLoad.Register)}, [x{ProcessRegisterID(varLoad.Register)}]");
            }

			// Case 6: Variable Store (str)
			else if (instruction is VariableStore varStore)
			{
                instructions.Add($"ldr x{ProcessRegisterID(varStore.AddressRegister)}, ={varStore.Ident}");
                instructions.Add($"str w{ProcessRegisterID(varStore.SourceRegister)}, [x{ProcessRegisterID(varStore.AddressRegister)}]");
            }

            return instructions.ToArray();
		}

        public static string IRVariableToAssembly(KeyValuePair<string, (DataType, string?)> variable)
		{
            if (variable.Value.Item2 == null)
            {
                return variable.Key + ": " + DataTypeToAssembly(variable.Value.Item1) + " " + Constants.DataTypeDefaultValue(variable.Value.Item1);
            }
            else
            {
                return variable.Key + ": " + DataTypeToAssembly(variable.Value.Item1) + " " + variable.Value.Item2;
            }
		}

        public static string SubroutineIdentifier(string subroutineIdent)
        {
            return subroutineIdent switch
            {
                "Output" => "printint",
                _ => subroutineIdent
            };
        }

		public static int ProcessRegisterID(RegisterID regId)
		{
			return regId.Type switch
			{
				RegisterType.Parameter => regId.ID,
				RegisterType.Variable => regId.ID + Constants.FirstVarRegister,
                RegisterType.Temp => regId.ID + Constants.FirstScratchpadRegister,
            };
		}

		public static string DataTypeToAssembly(DataType type)
		{
			string? t = type switch
			{
				DataType.Int32 => ".word",
                DataType.String => ".ascii",
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

