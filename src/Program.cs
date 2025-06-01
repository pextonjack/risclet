namespace RISClet_Compiler;

/// <summary>
/// Program entry point; used for managing CLI interactions
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        string src = FileManager.ReadText("/Users/jackpexton/Desktop/code.risclet");
        Console.WriteLine("Original source code:\n" + src + "\n");

        var tokens = Lexer.Tokenise(src);
        Console.WriteLine("Tokens:");
        foreach (var token in tokens)
        {
            Console.Write(token.ToString() + " ");
        }

        Console.WriteLine("\n\nAST:");
        var ast = Parser.Parse(tokens);
    }
}

