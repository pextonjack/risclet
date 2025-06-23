using System.Diagnostics;

namespace RISClet_Compiler;

/// <summary>
/// Program entry point; used for managing CLI interactions
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        string src = new FileManager().ReadText("/Users/jackpexton/Desktop/risclet/code.risclet");
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

        Console.WriteLine("\n\nTuple IR:");
        var tupleIr = new TupleIR().GenerateTupleIR(ast);
        Console.WriteLine(tupleIr.ToString());

        //Console.WriteLine("\n\nAssembly:");
        //var code = new CodeGenerator().GenerateCode(ir);
        //Console.WriteLine(code);

        Console.WriteLine("\n\nLower IR:");
        var lowerIr = new LowerIR().GenerateLowerIR(tupleIr);
        foreach (LowerIRInstruction instruction in lowerIr.Instructions)
        {
            Console.WriteLine(instruction.GetType().ToString());
        }

        Console.WriteLine("\n\nAssembly:");
        var code = new CodeGenerator().GenerateCode(lowerIr);
        Console.WriteLine(code);

        new FileManager().SaveText("/Users/jackpexton/Desktop/risclet/prog.s", code);
        Console.WriteLine("\nCompilation: Done!");
        stopwatch.Stop();
        Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");
    }
}