//using System;
using System.Reflection;
using System.Reflection.Emit;

namespace sharpash.Codegen;

public class ILCompiler
{
    private readonly AssemblyBuilder _asmBuilder;
    private readonly ModuleBuilder _module;
    private readonly TypeBuilder _type;
    private readonly MethodBuilder _method;
    private readonly ILGenerator _il;

    public ILCompiler()
    {
        var asmName = new AssemblyName("ASharpProgram");
        _asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
        _module = _asmBuilder.DefineDynamicModule("MainModule");

        _type = _module.DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);

        _method = _type.DefineMethod(
            "Main",
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(double),
            Type.EmptyTypes
        );

        _il = _method.GetILGenerator();
    }

    public ILGenerator IL => _il;

    public Type Finish()
    {
        _il.Emit(OpCodes.Ret);
        return _type.CreateType();
    }
}