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
					textSectionBuilder.Append(Constants.Indentation + instruction);
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
            return codeBuilder.ToString();
		}

		public static string[] IRInstructionToAssembly(IRInstruction instruction)
		{
			// TODO: Implement
			return new string[0];
		}

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

