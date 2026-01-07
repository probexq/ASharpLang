using System;
using ASharp.Compiler.Codegen;
using ASharp.Compiler.Lexere;
using ASharp.Compiler.Parsing;

namespace ASharp.Compiler;

internal static class Program
{
    static void Main(string[] args)
    {
        if(args.Length == 0){
            Console.WriteLine("Usage: ash <file.ash>");
            return;
        }
        
        var file = args[0];
        var source = File.ReadAllText(file);

        var lexer = new Lexer(source);
        var tokens = lexer.GenerateTokens();

        var parser = new Parser(tokens);
        var ast = parser.parse();

        var compiler = new ILCompiler();
        var codegen = new CodeGenVisitor(compiler);
        codegen.Visit(ast);

        var programType = compiler.Finish();
        var result = programType.GetMethod("Main")!.Invoke(null, null);

        Console.WriteLine(result);
    }
}