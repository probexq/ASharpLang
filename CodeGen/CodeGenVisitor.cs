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

private bool isLast;
    public void Visit(Node node) // needs to end with +1
    {
        switch (node)
        {
            case ProgramNode prog:
                for(int i = 0; i<prog.Stats.Count; i++) {
                    var cur = prog.Stats[i];
                    Console.WriteLine($"{cur}, {i}");
                    isLast = (i == prog.Stats.Count - 1);
                    Visit(cur);
                    if(!isLast && cur is not TypeNode) _compiler.IL.Emit(OpCodes.Pop); // -1
                    else if(isLast && cur is LogicNode l && l.If is ProgramNode pr && pr.Stats.Count == 0) _compiler.IL.Emit(OpCodes.Ldc_R8, 0.0); // +1
                }
                break;
            case TypeNode t:
                switch (t.Type){
                    case TokenType.LET: emit_let(t.Name, t.Var, leaveOnStack: isLast); break;
                    case TokenType.CONST: emit_const(t.Name, t.Var, leaveOnStack: isLast); break;
                    case TokenType.COND: emit_condition(t.Name, t.Var, leaveOnStack: isLast); break;
                } break;

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
                if(v.Value != null) emit_var(v.Name, v.Value);
                else emit_var(v.Name);
                break;

            case BinOpNode b:
                Visit(b.Left); // +1
                Visit(b.Right); // +1
                switch (b.Op)
                {
                    // 'and'
                    case TokenType.AND:
                        LocalBuilder lb = _compiler.IL.DeclareLocal(typeof(long));
                        _compiler.IL.Emit(OpCodes.Conv_I8);
                        _compiler.IL.Emit(OpCodes.Stloc, lb); // -1
                        _compiler.IL.Emit(OpCodes.Conv_I8);
                        _compiler.IL.Emit(OpCodes.Ldloc, lb); // +1
                        _compiler.IL.Emit(OpCodes.And); // -1
                        _compiler.IL.Emit(OpCodes.Conv_R8);
                        break;
                    // 'or'
                    case TokenType.OR:
                        LocalBuilder lbor = _compiler.IL.DeclareLocal(typeof(long));
                        _compiler.IL.Emit(OpCodes.Conv_I8);
                        _compiler.IL.Emit(OpCodes.Stloc, lbor); // -1
                        _compiler.IL.Emit(OpCodes.Conv_I8);
                        _compiler.IL.Emit(OpCodes.Ldloc, lbor); // +1
                        _compiler.IL.Emit(OpCodes.Or); // -1
                        _compiler.IL.Emit(OpCodes.Conv_R8);
                        break;
                    // '>'
                    case TokenType.MORE:
                        _compiler.IL.Emit(OpCodes.Cgt); // -1 
                        _compiler.IL.Emit(OpCodes.Conv_R8);
                        break;
                    // '<'
                    case TokenType.LESS: 
                        _compiler.IL.Emit(OpCodes.Clt); // -1
                        _compiler.IL.Emit(OpCodes.Conv_R8); 
                        break;
                    // '=='
                    case TokenType.IFEQ:
                        _compiler.IL.Emit(OpCodes.Ceq); // -1
                        _compiler.IL.Emit(OpCodes.Conv_R8); 
                        break;
                    // '!='
                    case TokenType.NOTEQ:
                        _compiler.IL.Emit(OpCodes.Ceq); // -1
                        _compiler.IL.Emit(OpCodes.Ldc_I4_0); // +1
                        _compiler.IL.Emit(OpCodes.Ceq); // -1
                        _compiler.IL.Emit(OpCodes.Conv_R8); 
                        break;
                    // '+'
                    case TokenType.PLUS: 
                        _compiler.IL.Emit(OpCodes.Add); // -1
                        break;
                    // '-' (Substraction)
                    case TokenType.MINUS: 
                        _compiler.IL.Emit(OpCodes.Sub); // -1
                        break;
                    // '*' 
                    case TokenType.MULT: 
                        _compiler.IL.Emit(OpCodes.Mul); // -1
                        break;
                    // '/'
                    case TokenType.DIV: 
                        _compiler.IL.Emit(OpCodes.Div); // -1
                        break;
                    // '^'
                    case TokenType.POW:
                        MethodInfo powDouble = typeof(Math).GetMethod("Pow")!; 
                        _compiler.IL.Emit(OpCodes.Call, powDouble); // -1 
                        break;
                    default: throw new Exception($"Unsupported binary opperand: {b.Op}");
                }
                break;

            case UnOpNode u:
                Visit(u.Expr); // +1
                switch (u.Op)
                {
                    // '-' (Negative)
                    case TokenType.MINUS: 
                        _compiler.IL.Emit(OpCodes.Neg); 
                        break;
                    case TokenType.NOT:
                        break;
                    // '_' (Square Root)
                    case TokenType.SQRT:
                        MethodInfo sqrtDouble = typeof(Math).GetMethod("Sqrt", new[] { typeof(double) })!;
                        _compiler.IL.Emit(OpCodes.Call, sqrtDouble); 
                        break;
                    // '~' (Rounding to an integer)
                    case TokenType.ROUND:
                        MethodInfo roundDouble = typeof(Math).GetMethod("Round", new[] { typeof(double), typeof(MidpointRounding) })!;
                        _compiler.IL.Emit(OpCodes.Call, roundDouble); break; 
                    default: throw new Exception($"Unsupported unary operrand '{u.Op}'");
                }
                break;

            case CallNode c:
                if (c.FuncName == "MAX" || c.FuncName == "+#")
                {
                    if(c.Args.Count < 2) ThrowError($"+#() method requires at least two arguments.");
                    MethodInfo MathMaxDouble = typeof(Math).GetMethod("Max", new[] { typeof(double), typeof(double) })!;
                    Visit(c.Args[0]); // +1
                    for (int i = 1; i < c.Args.Count; i++){
                        Visit(c.Args[i]); // +1
                        _compiler.IL.Emit(OpCodes.Call, MathMaxDouble); // -1
                    }
                }
                else if (c.FuncName == "MIN" || c.FuncName == "-#")
                {
                    if(c.Args.Count < 2) ThrowError($"-#() method requires at least two arguments.");
                    MethodInfo MathMinDouble = typeof(Math).GetMethod("Min", new[] { typeof(double), typeof(double) })!;
                    Visit(c.Args[0]); // +1
                    for (int i = 1; i < c.Args.Count; i++){
                        Visit(c.Args[i]); // +1
                        _compiler.IL.Emit(OpCodes.Call, MathMinDouble); // -1
                    }
                }
                else if (c.FuncName == "ABS")
                {
                    Visit(c.Args[0]); // +1
                    MethodInfo MathAbsDouble = typeof(Math).GetMethod("Abs", new[] { typeof(double) })!;
                    _compiler.IL.Emit(OpCodes.Call, MathAbsDouble); // -1
                }
                else if(c.FuncName == "LOG" || c.FuncName == "log"){
                    Visit(c.Args[0]); //+1
                    MethodInfo printDouble = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(double) })!;
                    _compiler.IL.Emit(OpCodes.Dup); //+1
                    _compiler.IL.Emit(OpCodes.Call, printDouble); //-1
                }
                else
                {
                    throw new Exception($"Unknown function '{c.FuncName}'");
                }
                break;
            case LogicNode l:
                emit_guard(l, leaveOnStack: isLast);
                break;
            default: throw new Exception($"Unknown AST node '{node.GetType()}'");
        }
    }
    public void emit_let(string name, Node expr, bool leaveOnStack){
        Visit(expr); // +1
        if(!_variables.TryGetValue(name, out var local)){
            local = _compiler.IL.DeclareLocal(typeof(double));
            _variables[name] = local;
        }

        _compiler.IL.Emit(OpCodes.Stloc, local);// -1
        if(leaveOnStack) _compiler.IL.Emit(OpCodes.Ldloc, local);// +1
    }
    public void emit_const(string name, Node expr, bool leaveOnStack){
        Visit(expr); // +1

        if(_variables.ContainsKey(name)){
            ThrowError($"VariableError: Constant '{name}' is already defined");
        }

        var local = _compiler.IL.DeclareLocal(typeof(double));
        _variables[name] = local;
        _compiler.IL.Emit(OpCodes.Stloc, local); // -1
        if(leaveOnStack) _compiler.IL.Emit(OpCodes.Ldloc, local); // +1
    }
    public void emit_condition(string name, Node expr, bool leaveOnStack){
        Visit(expr); // +1

        _compiler.IL.Emit(OpCodes.Ldc_R8, 0.0); // +1
        _compiler.IL.Emit(OpCodes.Ceq); // -1
        _compiler.IL.Emit(OpCodes.Ldc_I4_0); // +1
        _compiler.IL.Emit(OpCodes.Ceq); // -1
        _compiler.IL.Emit(OpCodes.Conv_R8);

        if(!_variables.TryGetValue(name, out var local)){
            local = _compiler.IL.DeclareLocal(typeof(double));
            _variables[name] = local;
        }

        _compiler.IL.Emit(OpCodes.Stloc, local); // -1
        if(leaveOnStack) _compiler.IL.Emit(OpCodes.Ldloc, local); // +1
    }
    void emit_var(string name, Node value = null!){
        if(!_variables.TryGetValue(name, out var local)) ThrowError($"Variable '{name}' is not defined.");
        if(value != null) {Visit(value); _compiler.IL.Emit(OpCodes.Stloc, local!);}//if you are changing the value -1
        _compiler.IL.Emit(OpCodes.Ldloc, local!); //+1
    }
    void emit_guard(LogicNode l, bool leaveOnStack){
        Visit(l.Condition); // +1
        var arg = l.Condition;

        _compiler.IL.Emit(OpCodes.Ldc_R8, 0.0); // +1
        _compiler.IL.Emit(OpCodes.Ceq); // -1
        Label endGuard = _compiler.IL.DefineLabel();
        Label falsePath = _compiler.IL.DefineLabel();
        if(arg is UnOpNode u && u.Op == TokenType.NOT) _compiler.IL.Emit(OpCodes.Brfalse, falsePath); // -1
        else _compiler.IL.Emit(OpCodes.Brtrue, falsePath); // -1

        bool leave = leaveOnStack;

        Visit(l.If); // +1
        
        if(!leave)_compiler.IL.Emit(OpCodes.Pop); // if the guard is not last -1
        _compiler.IL.Emit(OpCodes.Br, endGuard);

        _compiler.IL.MarkLabel(falsePath);
        if(leave) _compiler.IL.Emit(OpCodes.Ldc_R8, 0.0); // +1

        _compiler.IL.MarkLabel(endGuard);
    }
    private void ThrowError(string message){
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{message}");
        Console.ResetColor();
        Environment.Exit(1);
        throw new Exception("");
    }
}