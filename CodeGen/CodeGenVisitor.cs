//using System;
//using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using sharpash.AST;
using sharpash.Lexering;
using sharpash.Parsing;

namespace sharpash.Codegen;
public class CodeGenVisitor
{
    private readonly ILCompiler _compiler;
    private readonly Dictionary<string, LocalBuilder> _variables;
    private string _curPath;
    private static readonly Dictionary<string, Node> _astCache = new();

    public CodeGenVisitor(ILCompiler compiler, string curPath, Dictionary<string, LocalBuilder>? variables = null)
    {
        _compiler = compiler;
        _variables = variables ?? new Dictionary<string, LocalBuilder>();
        _curPath = curPath;
    }

    /*private double get_value(Node node){
        return node switch{
            NumberNode num => num.Value,
            VarNode v => get_value(v.Value),
            BinOpNode b => b.Op switch {
                TokenType.PLUS => get_value(b.Left) + get_value(b.Right),
                TokenType.MINUS => get_value(b.Left) - get_value(b.Right),
                TokenType.MULT => get_value(b.Left) * get_value(b.Right),
                TokenType.DIV => get_value(b.Left) / get_value(b.Right),
                TokenType.POW => Math.Pow(get_value(b.Left), get_value(b.Right)),
                _ => ThrowError("Unsupported binary operrand"),
            },
            UnOpNode u => u.Op switch {
                TokenType.MINUS => -get_value(u.Expr),
                TokenType.SQRT => Math.Sqrt(get_value(u.Expr)),
                TokenType.ROUND => Math.Round(get_value(u.Expr)),
                _ => ThrowError("Unsupported unary operrand"),
            },
            CallNode c => c.FuncName switch {
                "MAX" => get_value(c.Args[0]),
                _ => ThrowError("Unsupported callable"),
            },
            _ => ThrowError("Node is not a constant"),
        };
    }*/

    private string get_libsource(string fileName){
        var assembly = Assembly.GetExecutingAssembly();

        string resName = $"sharpash.libs.{fileName}";

        using (Stream stream = assembly.GetManifestResourceStream(resName)!){
            if (stream != null){
                using(StreamReader reader = new StreamReader(stream)){
                    return reader.ReadToEnd();
                }
            }
        }
        string localPath = Path.Combine(Path.GetDirectoryName(_curPath) ?? "", fileName);
        if(File.Exists(localPath)) return File.ReadAllText(localPath);

        string exePath = Path.Combine(AppContext.BaseDirectory, "libs", fileName);
        if(File.Exists(exePath)) return File.ReadAllText(exePath);

        ThrowError($"FilePath Error: Library/File '{fileName}' not found.");
        return "";
    }

    public void Visit(Node node)
    {
        switch (node)
        {
            case ProgramNode prog:
                for(int i = 0; i<prog.Stats.Count; i++) {
                    var curStat = prog.Stats[i];
                    bool isLast = (i == prog.Stats.Count - 1);

                    if(curStat is TypeNode let){
                        switch (let.Type){
                            case TokenType.LET: emit_let(let.Name, let.Var, leaveOnStack: isLast); break;
                            case TokenType.CONST: emit_const(let.Name, let.Var, leaveOnStack: isLast); break;
                        }
                    } else {
                        Visit(curStat);
                        if(!isLast){
                            _compiler.IL.Emit(OpCodes.Pop);
                        }
                    }
                }
                break;
            case ImportNode imp:
                string fileName = imp.Path;

                if(!_astCache.TryGetValue(fileName, out var cachedNode)){
                    string subSource = get_libsource(fileName);
                    var lexer = new Lexer(subSource);
                    var parser = new Parser(lexer.GenerateTokens());
                    cachedNode = parser.parse();
                    _astCache[fileName] = cachedNode;
                } this.Visit(cachedNode);
                break;
            case NumberNode n:
                _compiler.IL.Emit(OpCodes.Ldc_R8, n.Value);
                break;

            case VarNode v:
                if(v.Value != null) {emit_var(v.Name, v.Value); _compiler.IL.Emit(OpCodes.Pop);}
                else emit_var(v.Name);
                break;

            case BinOpNode b:
                Visit(b.Left);
                Visit(b.Right);
                switch (b.Op)
                {
                    case TokenType.PLUS: _compiler.IL.Emit(OpCodes.Add); break;
                    case TokenType.MINUS: _compiler.IL.Emit(OpCodes.Sub); break;
                    case TokenType.MULT: _compiler.IL.Emit(OpCodes.Mul); break;
                    case TokenType.DIV: _compiler.IL.Emit(OpCodes.Div); break;
                    case TokenType.POW:
                        MethodInfo powDouble = typeof(Math).GetMethod("Pow")!; 
                        _compiler.IL.Emit(OpCodes.Call, powDouble); break;
                    default: throw new Exception($"Unsupported binary opperand: {b.Op}");
                }
                break;

            case UnOpNode u:
                Visit(u.Expr);
                switch (u.Op)
                {
                    case TokenType.MINUS: _compiler.IL.Emit(OpCodes.Neg); break;
                    case TokenType.SQRT:
                        MethodInfo sqrtDouble = typeof(Math).GetMethod("Sqrt", new[] { typeof(double) })!;
                        _compiler.IL.Emit(OpCodes.Call, sqrtDouble); break;
                    case TokenType.ROUND:
                        MethodInfo roundDouble = typeof(Math).GetMethod("Round", new[] { typeof(double) })!;
                        _compiler.IL.Emit(OpCodes.Call, roundDouble); break;
                    default: throw new Exception($"Unsupported unary operrand {u.Op}");
                }
                break;

            case CallNode c:
                if (c.FuncName == "MAX" || c.FuncName == "+#")
                {
                    if(c.Args.Count < 2) ThrowError($"+#() method requires at least two arguments.");
                    MethodInfo MathMaxDouble = typeof(Math).GetMethod("Max", new[] { typeof(double), typeof(double) })!;
                    Visit(c.Args[0]);
                    for (int i = 1; i < c.Args.Count; i++){
                        Visit(c.Args[i]);
                        _compiler.IL.Emit(OpCodes.Call, MathMaxDouble);
                    }
                }
                else if (c.FuncName == "MIN" || c.FuncName == "-#")
                {
                    if(c.Args.Count < 2) ThrowError($"-#() method requires at least two arguments.");
                    MethodInfo MathMinDouble = typeof(Math).GetMethod("Min", new[] { typeof(double), typeof(double) })!;
                    Visit(c.Args[0]);
                    for (int i = 1; i < c.Args.Count; i++){
                        Visit(c.Args[i]);
                        _compiler.IL.Emit(OpCodes.Call, MathMinDouble);
                    }
                }
                else if (c.FuncName == "ABS")
                {
                    Visit(c.Args[0]);
                    MethodInfo MathAbsDouble = typeof(Math).GetMethod("Abs", new[] { typeof(double) })!;
                    _compiler.IL.Emit(OpCodes.Call, MathAbsDouble);
                }
                else if(c.FuncName == "LOG" || c.FuncName == "log"){
                    Visit(c.Args[0]);
                    MethodInfo printDouble = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(double) })!;
                    _compiler.IL.Emit(OpCodes.Dup);
                    _compiler.IL.Emit(OpCodes.Call, printDouble);
                }
                else
                {
                    throw new Exception($"Unknown function '{c.FuncName}'");
                }
                break;
            default: throw new Exception($"Unknown AST node '{node.GetType()}'");
        }
    }
    public void emit_let(string name, Node expr, bool leaveOnStack){
        Visit(expr);
        if(!_variables.TryGetValue(name, out var local)){
            local = _compiler.IL.DeclareLocal(typeof(double));
            _variables[name] = local;
        }

        _compiler.IL.Emit(OpCodes.Stloc, local);
        if(leaveOnStack) _compiler.IL.Emit(OpCodes.Ldloc, local);
    }
    public void emit_const(string name, Node expr, bool leaveOnStack){
        Visit(expr);

        if(_variables.ContainsKey(name)){
            ThrowError($"VariableError: Constant '{name}' is already defined");
        }

        var local = _compiler.IL.DeclareLocal(typeof(double));
        _variables[name] = local;
        _compiler.IL.Emit(OpCodes.Stloc, local);
        if(leaveOnStack) _compiler.IL.Emit(OpCodes.Ldloc, local);
    }
    void emit_var(string name, Node value = null!){
        if(!_variables.TryGetValue(name, out var local)) ThrowError($"Variable '{name}' is not defined.");
        if(value != null) {Visit(value); _compiler.IL.Emit(OpCodes.Stloc, local!);}
        else _compiler.IL.Emit(OpCodes.Ldloc, local!);
    }
    private void ThrowError(string message){
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{message}");
        Console.ResetColor();
        Environment.Exit(1);
        throw new Exception("");
    }
}