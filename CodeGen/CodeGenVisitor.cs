using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ASharp.Compiler.AST;
using ASharp.Compiler.Lexere;
using ASharp.Compiler.Parsing;

namespace ASharp.Compiler.Codegen;
public class CodeGenVisitor
{
    private readonly ILCompiler _compiler;

    private static readonly MethodInfo MathMaxDouble = typeof(Math).GetMethod("Max", new[] { typeof(double), typeof(double) })!;
    private static readonly MethodInfo MathMinDouble = typeof(Math).GetMethod("Min", new[] { typeof(double), typeof(double) })!;
    private readonly Dictionary<string, LocalBuilder> _variables;
    private string _curPath;
    private static readonly Dictionary<string, Node> _astCache = new();

    public CodeGenVisitor(ILCompiler compiler, string curPath, Dictionary<string, LocalBuilder>? variables = null)
    {
        _compiler = compiler;
        _variables = variables ?? new Dictionary<string, LocalBuilder>();
        _curPath = curPath;
    }

    private string get_libsource(string fileName){
        var assembly = Assembly.GetExecutingAssembly();

        string resName = $"A#.Compiler.libs.{fileName}";

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

        throw new Exception($"FilePath Error: Library {fileName} not found.");
    }

    public void Visit(Node node)
    {
        switch (node)
        {
            case ProgramNode prog:
                for(int i = 0; i<prog.Stats.Count; i++) {
                    var curStat = prog.Stats[i];
                    bool isLast = (i == prog.Stats.Count - 1);

                    if(curStat is LetNode let){
                        emit_let(let.Name, let.Value, leaveOnStack: isLast);
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
                emit_var(v.Name);
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
                        _compiler.IL.EmitCall(OpCodes.Call, typeof(Math).GetMethod("Pow")!, null);
                        break;
                    default: throw new Exception($"Unsupported binop {b.Op}");
                }
                break;

            case UnOpNode u:
                Visit(u.Expr);
                switch (u.Op)
                {
                    case TokenType.MINUS: _compiler.IL.Emit(OpCodes.Neg); break;
                    case TokenType.SQRT:
                        _compiler.IL.EmitCall(OpCodes.Call, typeof(Math).GetMethod("Sqrt")!, null);
                        break;
                    case TokenType.ROUND:
                        _compiler.IL.EmitCall(OpCodes.Call, typeof(Math).GetMethod("Round", new[] { typeof(double) })!, null);
                        break;
                    default: throw new Exception($"Unsupported unary op {u.Op}");
                }
                break;

            case CallNode c:
                if (c.FuncName == "MAX" || c.FuncName == "++")
                {
                    if(c.Args.Count < 2) throw new Exception($"++() method requires at least two arguments.");

                    Visit(c.Args[0]);
                    for (int i = 1; i < c.Args.Count; i++){
                        Visit(c.Args[i]);
                        _compiler.IL.Emit(OpCodes.Call, MathMaxDouble);
                    }
                }
                else if (c.FuncName == "MIN" || c.FuncName == "--")
                {
                    if(c.Args.Count < 2) throw new Exception($"--() method requires at least two arguments.");

                    Visit(c.Args[0]);
                    for (int i = 1; i < c.Args.Count; i++){
                        Visit(c.Args[i]);
                        _compiler.IL.Emit(OpCodes.Call, MathMinDouble);
                    }
                }
                else if (c.FuncName == "ABS")
                {
                    Visit(c.Args[0]);
                    _compiler.IL.EmitCall(OpCodes.Call, typeof(Math).GetMethod("Abs", new[] { typeof(double) })!, null);
                }
                else if(c.FuncName == "LOG" || c.FuncName == "log"){
                    Visit(c.Args[0]);
                    _compiler.IL.Emit(OpCodes.Dup);
                    _compiler.IL.EmitCall(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] {typeof(double)})!, null);
                }
                else
                {
                    throw new Exception($"Unknown function {c.FuncName}");
                }
                break;

            default:
                throw new Exception($"Unknown AST node {node.GetType()}");
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

    void emit_var(string name){
        if(!_variables.TryGetValue(name, out var local)) throw new Exception($"Undefined Variable '{name}'");
        _compiler.IL.Emit(OpCodes.Ldloc, local); 
    }
}