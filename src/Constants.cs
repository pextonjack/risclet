using System;
namespace RISClet_Compiler
{
	public class Constants
	{
		public const string Indentation = "    ";

		public static string DataTypeDefaultValue(DataType type)
		{
			return type switch
			{
                DataType.Int32 => "0",
                DataType.String => "\"\"",
            };
		}

		// x9 onwards are free to use in AAPCS64; x0-x7 are parameters, x8 is sometimes used for special return pointers; x0-x17 are all caller-saved
        public const int FirstScratchpadRegister = 15;
		public const int FirstVarRegister = 9;
	}
}

