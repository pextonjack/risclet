namespace RISClet_Compiler;

/// <summary>
/// Program entry point; used for managing CLI interactions
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        string src = new FileManager().ReadText("/Users/jackpexton/Desktop/code.risclet");
        Console.WriteLine("Original source code:\n" + src + "\n");

        var tokens = new Lexer().Tokenise(src);
        Console.WriteLine("Tokens:");
        foreach (var token in tokens)
        {
            Console.Write(token.ToString() + " ");
        }

        Console.WriteLine("\n\nAST:");
        var ast = new Parser().Parse(tokens);
        foreach (ASTNode n in ast.Statements)
        {
            Console.WriteLine(n.GetType().ToString());
        }

        Console.WriteLine("\n\nIR:");
        var ir = new IR().GenerateTupleIR(ast);
        Console.WriteLine(ir.ToString());

        Console.WriteLine("\n\nAssembly:");
        var code = new CodeGenerator().GenerateCode(ir);
        Console.WriteLine(code);
    }
}