using System;
using System.IO;
using System.Text;

namespace RISClet_Compiler
{
	public class FileManager
	{
		public static string ReadText(string path)
		{
            return File.ReadAllText(path) ?? "";
        }
    }
}

