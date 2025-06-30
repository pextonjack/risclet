using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RISClet_Compiler;

/// <summary>
/// Program entry point; used for managing CLI interactions
/// </summary>
class Program
{
    public const string VersionID = "Alpha 1B \"Metis\"";

    static void Main(string[] args)
    {
        Profile prof = ProcessArgs(args);

        switch (prof.Task)
        {
            case TaskType.Help:
                Help();
                break;
            case TaskType.Version:
                Version();
                break;
            case TaskType.Invalid:
                Invalid();
                break;
            case TaskType.Compile:
                Compile(prof);
                break;
        }
    }

    private static void Help()
    {
        Console.WriteLine("""
            Usage: risclet <source-file> [options]

            Options:
                -o <file>       Write output to <file>
                -v              Enable verbose mode
                -Wall           Enable all warnings
                --help          Show this help message
                --version       Show version information
            """);
    }

    private static void Version()
    {
        Console.WriteLine($"""
            RISClet compiler {VersionID}
            (c) 2025 Jack Pexton, MIT License
            """);
    }

    private static void Invalid()
    {
        Console.WriteLine("""
            Error: invalid command syntax
            
            Usage: risclet <source-file> [options]
            Use 'risclet --help' for more information.
            """);
    }

    private static void Compile(Profile profile)
    {
        bool verbose = profile.Verbose;
        bool warnings = profile.ShowWarnings;

        Stopwatch stopwatch = new();

        if (verbose)
        {
            stopwatch.Start();
        }

        string filePath = Path.GetFullPath(profile.SourceFilename);
        string src = new FileManager().ReadText(filePath);
        if (verbose)
        {
            Console.WriteLine($"RISClet Source Code, from \"{filePath}\":\n{src}\n");
        }

        var tokens = new Lexer().Tokenise(src);
        if (verbose)
        {
            System.Text.StringBuilder tokenString = new();
            foreach (Token t in tokens) tokenString.Append(t.ToString() + " ");

            Console.WriteLine($"Tokens:\n{tokenString}\n");
        }

        var ast = new Parser().Parse(tokens);
        if (verbose)
        {
            System.Text.StringBuilder astString = new();
            foreach (ASTNode node in ast.Statements) astString.Append(node.GetType().ToString() + "\n");

            Console.WriteLine($"AST Nodes:\n{astString}\n");
        }

        var tupleIR = new TupleIR().GenerateTupleIR(ast);
        if (verbose)
        {
            Console.WriteLine($"Tuple IR:\n{tupleIR}\n");
        }

        var lowerIR = new LowerIR().GenerateLowerIR(tupleIR);
        if (verbose)
        {
            System.Text.StringBuilder lowerIRString = new();
            foreach (LowerIRInstruction lowerInstruction in lowerIR.Instructions) lowerIRString.Append(lowerInstruction.ToString() + "\n");

            Console.WriteLine($"Lower IR:\n{lowerIRString}\n");
        }

        var code = new CodeGenerator().GenerateCode(lowerIR);
        if (verbose)
        {
            Console.WriteLine($"AArch64:\n{code}\n");
        }

        new FileManager().SaveText(Path.GetFullPath(profile.DestinationFilename ?? "null.s"), code);
        Console.WriteLine($"Compiled, ready for assembly, in {Path.GetFullPath(profile.DestinationFilename ?? "null.s")}");

        if (verbose)
        {
            stopwatch.Stop();
            Console.WriteLine($"Completed in {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    private static Profile ProcessArgs(string[] args)
    {
        // Case 0: args is empty
        if (args.Length == 0)
            return new Profile() { Task = TaskType.Invalid };

        // Special cases
        // Case 1: --help
        if (args[0] == "--help")
            return new Profile() { Task = TaskType.Help };

        // Case 2: --version
        if (args[0] == "--version")
            return new Profile() { Task = TaskType.Version };

        // args[0] assumed to be the filename
        var profile = new Profile() { SourceFilename = args[0], Task = TaskType.Compile };

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "-o" && i + 1 < args.Length)
            {
                profile.DestinationFilename = args[i + 1];
            }
            else if (args[i] == "-v")
            {
                profile.Verbose = true;
            }
            else if (args[i] == "-Wall")
            {
                profile.ShowWarnings = true;
            }
        }

        if (profile.DestinationFilename == null)
        {
            profile.DestinationFilename = profile.SourceFilename.Split('.')[0] + ".s";
        }

        return profile;
    }

    private struct Profile
    {
        public string SourceFilename;
        public string? DestinationFilename;

        public bool Verbose;
        public bool ShowWarnings;

        public TaskType Task;

        public Profile()
        {
            SourceFilename = "";
            DestinationFilename = null;
            Verbose = false;
            ShowWarnings = false;
            Task = TaskType.Invalid;
        }
    }

    private enum TaskType
    {
        Compile,
        Help,
        Version,
        Invalid
    }
}