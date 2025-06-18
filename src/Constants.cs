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
	}
}

