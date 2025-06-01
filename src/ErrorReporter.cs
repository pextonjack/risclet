using System;
namespace RISClet_Compiler
{
	/// <summary>
	/// Manages error handling; use this instead of directly saying 'throw new Exception()' for centralised error handling and logging
	/// </summary>
	public class ErrorReporter
	{
		public static void CompilerError(string msg, (int, int) position)
		{
			throw new Exception(msg);
		}
	}
}

