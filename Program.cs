using System.Reflection;
using sharpash.Codegen;
using sharpash.Lexering;
using sharpash.Parsing;

namespace sharpash;

internal static class Program
{
    static void Main(string[] args)
    {
        if(args.Length < 1){
            Console.WriteLine("Usage: .ash <file.ash>");
            return;
        }
        if(args[0] == "--version" || args[0] == "-v"){
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine($"ashc v{version?.Major}.{version?.Minor}.{version?.Build}");
            Console.WriteLine($"Target: .NET 10.0 CIL");
            return;
        } if(args[0] == "--changelog" || args[0] == "-log"){
            Console.WriteLine("Changelog:");
            Console.WriteLine("- Changed from '\\\\' to '{}' because of vscode issues and to resemble C#'s syntax more;");
            //Console.WriteLine("- ;");
            return;
        }
        
        var file = Path.GetFullPath(args[0]);
        if(!File.Exists(file)){ 
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"PathError: file \"{file}\" doesn't exist.");
            Console.ResetColor();
            Environment.Exit(1);
        }
        var source = File.ReadAllText(file);

        var lexer = new Lexer(source);
        var tokens = lexer.GenerateTokens();

        var parser = new Parser(tokens);
        var ast = parser.parse();

        var compiler = new ILCompiler();
        var codegen = new CodeGenVisitor(compiler, file);
        codegen.Visit(ast);

        compiler.Finish();

    }
}