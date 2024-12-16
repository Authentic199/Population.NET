using Humanizer;
using Infrastructure.Facades.Populates.Exceptions;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Infrastructure.Facades.Populates.Extensions;

public static class AnonymousTypeGenerator
{
    private const string TypeAlias = "Infrastructure.Facades.Common.ProjectionMappers.AnonymousType";
    private const string AssemblyAlias = "Infrastructure.Facades.Common.ProjectionMappers.AnonymousAssembly, Version=1.0.0.0";
    private const MethodAttributes GetSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
    private static readonly CustomAttributeBuilder CompilerGeneratedAttributeBuilder = new(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes)!, []);

    public static Type Generate(IEnumerable<PropertyInfo> properties)
    {
        AssemblyName dynamicAssemblyName = new(AssemblyAlias);
        AssemblyBuilder dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(dynamicAssemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder dynamicModule = dynamicAssembly.DefineDynamicModule(AssemblyAlias);
        TypeBuilder dynamicAnonymousType = dynamicModule.DefineType(TypeAlias, TypeAttributes.Public);
        dynamicAnonymousType.SetCustomAttribute(CompilerGeneratedAttributeBuilder);

        properties.ToList().ForEach
             (
                 info =>
                 {
                     string name = info.Name;
                     Type type = ChooseType(info.PropertyType);

                     FieldBuilder fieldBuilder = dynamicAnonymousType.DefineField(name.Camelize(), type, FieldAttributes.Private);
                     fieldBuilder.SetCustomAttribute(CompilerGeneratedAttributeBuilder);

                     PropertyBuilder propertyBuilder = dynamicAnonymousType.DefineProperty(name.Pascalize(), PropertyAttributes.None, CallingConventions.HasThis, type, Type.EmptyTypes);
                     propertyBuilder.SetCustomAttributes(info.GetCustomAttributesData());

                     MethodBuilder getMethodBuilder = dynamicAnonymousType.DefineMethod($"get_{name}", GetSetAttr, CallingConventions.HasThis, type, Type.EmptyTypes);
                     getMethodBuilder.SetCustomAttribute(CompilerGeneratedAttributeBuilder);

                     ILGenerator getMethodIL = getMethodBuilder.GetILGenerator();
                     getMethodIL.Emit(OpCodes.Ldarg_0);
                     getMethodIL.Emit(OpCodes.Ldfld, fieldBuilder);
                     getMethodIL.Emit(OpCodes.Ret);
                     propertyBuilder.SetGetMethod(getMethodBuilder);

                     MethodBuilder setMethodBuilder = dynamicAnonymousType.DefineMethod($"set_{name}", GetSetAttr, CallingConventions.HasThis, null, [type]);
                     setMethodBuilder.SetCustomAttribute(CompilerGeneratedAttributeBuilder);

                     ILGenerator setMethodIL = setMethodBuilder.GetILGenerator();
                     setMethodIL.Emit(OpCodes.Ldarg_0);
                     setMethodIL.Emit(OpCodes.Ldarg_1);
                     setMethodIL.Emit(OpCodes.Stfld, fieldBuilder);
                     setMethodIL.Emit(OpCodes.Ret);
                     propertyBuilder.SetSetMethod(setMethodBuilder);
                 }
             );

        return dynamicAnonymousType.CreateType() ?? throw new PopulateNotHandleException("Can't create anonymous type");
    }

    private static Type ChooseType(Type type) => !type.IsDbJsonType() && (type.IsClass() || type.IsGenericCollection()) ? typeof(object) : type;

    private static void SetCustomAttributes(this PropertyBuilder propertyBuilder, IEnumerable<CustomAttributeData> baseAttribute)
    {
        foreach (CustomAttributeData attribute in baseAttribute)
        {
            IList<CustomAttributeTypedArgument> constructorArgs = attribute.ConstructorArguments;
            if (constructorArgs.Any(x => x.Value?.GetType().IsAssignableTo(typeof(IList)) == true))
            {
                continue;
            }

            try
            {
                CustomAttributeBuilder builder = new(attribute.Constructor, constructorArgs.Select(x => x.Value).ToArray());
                propertyBuilder.SetCustomAttribute(builder);
            }
            catch (Exception ex)
            {
                throw new PopulateNotHandleException(
                    $"`{nameof(AnonymousTypeGenerator)}`:`{nameof(SetCustomAttributes)}` an error occur during set attribute for property {propertyBuilder.Name}",
                    ex);
            }
        }
    }
}
