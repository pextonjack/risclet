using System;
using System.IO;
using System.Text;

namespace RISClet_Compiler
{
	public class FileManager
	{
        /// <summary>
        /// Reads all text from the given path, replacing Windows line endings (\r\n) with Unix line endings (\n), and returning as a string
        /// </summary>
        /// <param name="path"></param>
        /// <returns>String of text file</returns>
		public string ReadText(string path)
		{
            string code = File.ReadAllText(path);
            code = code.ReplaceLineEndings("\n");
            return code;
        }
    }
}

