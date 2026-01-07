using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using ASharp.Compiler.AST;
using ASharp.Compiler.Lexere;

namespace ASharp.Compiler.Codegen;
public class CodeGenVisitor
{
    private readonly ILCompiler _compiler;
    private readonly Dictionary<string, LocalBuilder> _variables;

    public CodeGenVisitor(ILCompiler compiler, Dictionary<string, LocalBuilder>? variables = null)
    {
        _compiler = compiler;
        _variables = variables ?? new Dictionary<string, LocalBuilder>();
    }

    public void Visit(Node node)
    {
        switch (node)
        {
            case ProgramNode prog: foreach(var stmt in prog.Stats) Visit(stmt); break;
            case LetNode let: emit_let(let.Name, let.Value); break;
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
                    foreach (var arg in c.Args)
                        Visit(arg);
                    _compiler.IL.EmitCall(OpCodes.Call, typeof(Math).GetMethod("Max", new[] { typeof(double), typeof(double) })!, null);
                }
                else if (c.FuncName == "MIN" || c.FuncName == "--")
                {
                    foreach (var arg in c.Args)
                        Visit(arg);
                    _compiler.IL.EmitCall(OpCodes.Call, typeof(Math).GetMethod("Min", new[] { typeof(double), typeof(double) })!, null);
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
    public void emit_let(string name, Node expr){
        Visit(expr);
        if(!_variables.TryGetValue(name, out var local)){
            local = _compiler.IL.DeclareLocal(typeof(double));
            _variables[name] = local;
        }

        _compiler.IL.Emit(OpCodes.Stloc, local);
    }

    void emit_var(string name){
        if(!_variables.TryGetValue(name, out var local)) throw new Exception($"Undefined Variable '{name}'");
        _compiler.IL.Emit(OpCodes.Ldloc, local); 
    }
}