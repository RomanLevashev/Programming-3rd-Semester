namespace Reflector;

using System.Reflection;
using System.Text;

public class Reflector
{
    private readonly HashSet<string> Keywords = new HashSet<string>
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while",
    };

    public void PrintStructure(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var className = type.Name;
        string fileName = $"{className}.cs";
        string namespaceName = type.Namespace ?? "GeneratedNamespace";

        using var writer = new StreamWriter(fileName, false, Encoding.UTF8);

        writer.WriteLine($"namespace {namespaceName};");

        this.WriteType(writer, type, 1);
    }

    public void WriteType(StreamWriter writer, Type type, int indentLevel = 0)
    {
        int spaceCount = indentLevel * 4;
        string indent = new string(' ', spaceCount);

        writer.Write(indent);

        if (type.IsPublic || type.IsNestedPublic)
        {
            writer.Write("public ");
        }
        else if (type.IsNestedFamily)
        {
            writer.Write("protected ");
        }
        else if (type.IsNestedPrivate)
        {
            writer.Write("private ");
        }
        else if (type.IsNestedAssembly)
        {
            writer.Write("internal ");
        }
        else if (type.IsNestedFamORAssem)
        {
            writer.Write("protected internal ");
        }
        else if (type.IsNestedFamANDAssem)
        {
            writer.Write("private protected ");
        }

        if (type.IsAbstract && type.IsSealed)
        {
            writer.Write("static ");
        }
        else if (type.IsAbstract)
        {
            writer.Write("abstract ");
        }
        else if (type.IsSealed && type.IsClass)
        {
            writer.Write("sealed ");
        }

        if (type.IsClass)
        {
            if (type.IsInterface)
            {
                writer.Write("interface ");
            }
            else if (type.IsEnum)
            {
                writer.Write("enum ");
            }
            else
            {
                writer.Write("class ");
            }
        }
        else if (type.IsValueType && !type.IsEnum)
        {
            writer.Write("struct ");
        }

        writer.Write(this.GetTypeName(type));

        List<string> baseTypes = [];

        if (type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType))
        {
            baseTypes.Add(this.GetTypeName(type.BaseType));
        }

        var interfaces = type.GetInterfaces();
        foreach (var iface in interfaces)
        {
            baseTypes.Add(this.GetTypeName(iface));
        }

        if (baseTypes.Any())
        {
            writer.Write(" : " + string.Join(", ", baseTypes));
        }

        writer.WriteLine();
        writer.WriteLine(indent + "{");

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static |
                                   BindingFlags.DeclaredOnly);

        foreach (var field in fields.OrderBy(f => f.Name))
        {
            this.WriteField(writer, field, indentLevel + 1);
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.Instance | BindingFlags.Static |
                                          BindingFlags.DeclaredOnly);
        foreach (var property in properties.OrderBy(p => p.Name))
        {
            this.WriteProperty(writer, property, indentLevel + 1);
        }

        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic |
                                              BindingFlags.Instance | BindingFlags.Static |
                                              BindingFlags.DeclaredOnly);
        foreach (var constructor in constructors.OrderBy(c => c.GetParameters().Length))
        {
            this.WriteConstructor(writer, constructor, type, indentLevel + 1);
        }

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                    BindingFlags.Instance | BindingFlags.Static |
                                    BindingFlags.DeclaredOnly)
                         .Where(m => !m.IsSpecialName);

        foreach (var method in methods.OrderBy(m => m.Name))
        {
            this.WriteMethod(writer, method, indentLevel + 1);
        }

        var nestedTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(t => !t.GetCustomAttributesData()
                .Any(attr => attr.AttributeType.Name == "CompilerGeneratedAttribute"))
            .ToList();
        foreach (var nestedType in nestedTypes.OrderBy(t => t.Name))
        {
            writer.WriteLine();
            this.WriteType(writer, nestedType, indentLevel + 1);
        }

        writer.WriteLine(indent + "}");
    }

    private void WriteField(StreamWriter writer, FieldInfo field, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);

        writer.Write(indent);

        if (field.IsPublic)
        {
            writer.Write("public ");
        }
        else if (field.IsPrivate)
        {
            writer.Write("private ");
        }
        else if (field.IsFamily)
        {
            writer.Write("protected ");
        }
        else if (field.IsAssembly)
        {
            writer.Write("internal ");
        }
        else if (field.IsFamilyOrAssembly)
        {
            writer.Write("protected internal ");
        }
        else if (field.IsFamilyAndAssembly)
        {
            writer.Write("private protected ");
        }

        if (field.IsStatic)
        {
            writer.Write("static ");
        }

        if (field.IsInitOnly)
        {
            writer.Write("readonly ");
        }

        if (field.IsLiteral)
        {
            writer.Write("const ");
        }

        writer.Write(this.GetTypeName(field.FieldType) + " ");

        string fieldName = this.EscapeKeyword(field.Name);
        writer.Write(fieldName);

        if (field.IsLiteral)
        {
            writer.Write(" = ");
            object? value = field.GetRawConstantValue();
            this.WriteValue(writer, value, field.FieldType);
        }

        writer.WriteLine(";");
    }

    private void WriteProperty(StreamWriter writer, PropertyInfo property, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);

        writer.WriteLine(indent + $"// Property: {property.Name}");
    }

    private void WriteConstructor(StreamWriter writer, ConstructorInfo constructor, Type declaringType, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 4);

        writer.Write(indent);

        if (constructor.IsPublic)
        {
            writer.Write("public ");
        }
        else if (constructor.IsPrivate)
        {
            writer.Write("private ");
        }
        else if (constructor.IsFamily)
        {
            writer.Write("protected ");
        }
        else if (constructor.IsAssembly)
        {
            writer.Write("internal ");
        }
        else if (constructor.IsFamilyOrAssembly)
        {
            writer.Write("protected internal ");
        }
        else if (constructor.IsFamilyAndAssembly)
        {
            writer.Write("private protected ");
        }

        writer.Write(this.EscapeKeyword(declaringType.Name.Split('`')[0]));

        writer.Write("(");
        var parameters = constructor.GetParameters();
        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            writer.Write(this.GetTypeName(param.ParameterType) + " " + this.EscapeKeyword(param.Name));

            if (i < parameters.Length - 1)
            {
                writer.Write(", ");
            }
        }

        writer.Write(")");

        writer.WriteLine();
        writer.WriteLine(indent + "{");

        writer.WriteLine(indent + "    // Constructor implementation");

        writer.WriteLine(indent + "}");
        writer.WriteLine();
    }

    private void WriteMethod(StreamWriter writer, MethodInfo method, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);

        writer.Write(indent);

        if (method.IsPublic)
        {
            writer.Write("public ");
        }
        else if (method.IsPrivate)
        {
            writer.Write("private ");
        }
        else if (method.IsFamily)
        {
            writer.Write("protected ");
        }
        else if (method.IsAssembly)
        {
            writer.Write("internal ");
        }
        else if (method.IsFamilyOrAssembly)
        {
            writer.Write("protected internal ");
        }
        else if (method.IsFamilyAndAssembly)
        {
            writer.Write("private protected ");
        }

        if (method.IsStatic)
        {
            writer.Write("static ");
        }

        if (method.IsAbstract)
        {
            writer.Write("abstract ");
        }
        else if (method.IsVirtual && !method.IsFinal)
        {
            writer.Write("virtual ");
        }
        else if (method.IsFinal && method.IsVirtual)
        {
            writer.Write("sealed override ");
        }

        if (method.ReturnType == typeof(void))
        {
            writer.Write("void ");
        }
        else
        {
            writer.Write(this.GetTypeName(method.ReturnType) + " ");
        }

        string methodName = method.Name;
        if (method.IsGenericMethod)
        {
            var genericParams = method.GetGenericArguments();
            methodName += "<" + string.Join(", ", genericParams.Select(g => g.Name)) + ">";
        }

        writer.Write(this.EscapeKeyword(methodName));

        writer.Write("(");
        var parameters = method.GetParameters();
        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];

            if (param.IsOut)
            {
                writer.Write("out ");
            }
            else if (param.ParameterType.IsByRef)
            {
                writer.Write("ref ");
            }
            else if (param.IsIn)
            {
                writer.Write("in ");
            }

            writer.Write(this.GetTypeName(param.ParameterType) + " " + this.EscapeKeyword(param.Name));

            if (param.HasDefaultValue)
            {
                writer.Write(" = ");
                this.WriteValue(writer, param.DefaultValue, param.ParameterType);
            }

            if (i < parameters.Length - 1)
            {
                writer.Write(", ");
            }
        }

        writer.Write(")");

        if (method.IsGenericMethod)
        {
            var constraints = new List<string>();
            foreach (var genericParam in method.GetGenericArguments())
            {
                var paramConstraints = this.GetGenericConstraints(genericParam);
                if (!string.IsNullOrEmpty(paramConstraints))
                {
                    constraints.Add(paramConstraints);
                }
            }

            if (constraints.Any())
            {
                writer.Write(" where ");
                writer.Write(string.Join(" where ", constraints));
            }
        }

        writer.WriteLine();
        writer.WriteLine(indent + "{");

        writer.Write(indent + "    ");
        if (method.ReturnType != typeof(void))
        {
            writer.Write("return ");
            this.WriteDefaultValue(writer, method.ReturnType);
        }
        else
        {
            writer.Write("// Method implementation");
        }

        writer.WriteLine(";");

        writer.WriteLine(indent + "}");
        writer.WriteLine();
    }

    private string GetTypeName(Type type)
    {
        if (type == null)
        {
            return "void";
        }

        if (!type.IsGenericType)
        {
            if (type.IsArray)
            {
                return this.GetTypeName(type.GetElementType()) + "[]";
            }

            if (type.IsPointer)
            {
                return this.GetTypeName(type.GetElementType()) + "*";
            }

            return type.Name;
        }

        string typeName = type.Name.Split('`')[0];
        var genericArgs = type.GetGenericArguments();

        if (type.IsGenericTypeDefinition)
        {
            return typeName + "<" + string.Join(", ", genericArgs.Select(a => a.Name)) + ">";
        }
        else
        {
            return typeName + "<" + string.Join(", ", genericArgs.Select(this.GetTypeName)) + ">";
        }
    }

    private string GetGenericConstraints(Type genericParameter)
    {
        var constraints = new List<string>();

        if (genericParameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
        {
            constraints.Add("class");
        }

        if (genericParameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
        {
            constraints.Add("struct");
        }

        if (genericParameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
        {
            constraints.Add("new()");
        }

        var baseTypeConstraints = genericParameter.GetGenericParameterConstraints()
            .Where(t => t != typeof(object) && t != typeof(ValueType));

        foreach (var constraint in baseTypeConstraints)
        {
            constraints.Add(this.GetTypeName(constraint));
        }

        return string.Join(", ", constraints);
    }

    private void WriteValue(StreamWriter writer, object? value, Type type)
    {
        if (value == null)
        {
            writer.Write("null");
        }
        else if (type == typeof(string))
        {
            writer.Write($"\"{value}\"");
        }
        else if (type == typeof(char))
        {
            writer.Write($"'{value}'");
        }
        else if (type.IsEnum)
        {
            writer.Write($"{type.Name}.{value}");
        }
        else if (type == typeof(bool))
        {
            writer.Write(value.ToString().ToLower());
        }
        else if (type.IsPrimitive)
        {
            writer.Write(value);
        }
        else
        {
            writer.Write($"default({this.GetTypeName(type)})");
        }
    }

    private void WriteDefaultValue(StreamWriter writer, Type type)
    {
        if (type == typeof(void))
        {
            return;
        }

        if (type.IsValueType && !type.IsEnum)
        {
            writer.Write($"default({this.GetTypeName(type)})");
        }
        else if (type.IsClass || type.IsInterface)
        {
            writer.Write("null");
        }
        else if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            if (values.Length > 0)
            {
                writer.Write($"({this.GetTypeName(type)}){values.GetValue(0)}");
            }
            else
            {
                writer.Write($"default({this.GetTypeName(type)})");
            }
        }
        else
        {
            writer.Write($"default({this.GetTypeName(type)})");
        }
    }

    private string EscapeKeyword(string identifier)
    {
        if (this.Keywords.Contains(identifier.ToLower()))
        {
            return "@" + identifier;
        }

        return identifier;
    }

    public void DiffClasses(Type typeA, Type typeB)
    {
        if (typeA == null)
        {
            throw new ArgumentNullException(nameof(typeA));
        }

        if (typeB == null)
        {
            throw new ArgumentNullException(nameof(typeB));
        }

        Console.WriteLine($"Сравнение классов: {typeA.Name} и {typeB.Name}");
        Console.WriteLine("==========================================");

        this.CompareFields(typeA, typeB);
        Console.WriteLine();

        this.CompareProperties(typeA, typeB);
        Console.WriteLine();

        this.CompareMethods(typeA, typeB);
        Console.WriteLine();

        this.CompareConstructors(typeA, typeB);
    }

    private void CompareFields(Type typeA, Type typeB)
    {
        var fieldsA = this.GetFields(typeA);
        var fieldsB = this.GetFields(typeB);

        Console.WriteLine("ПОЛЯ:");

        var onlyInA = fieldsA.Where(fa => !fieldsB.Any(fb => this.AreFieldsEqual(fa, fb)));
        if (onlyInA.Any())
        {
            Console.WriteLine("Только в классе A:");
            foreach (var field in onlyInA)
            {
                Console.WriteLine($"  {this.GetFieldSignature(field)}");
            }
        }

        var onlyInB = fieldsB.Where(fb => !fieldsA.Any(fa => this.AreFieldsEqual(fa, fb)));
        if (onlyInB.Any())
        {
            Console.WriteLine("Только в классе B:");
            foreach (var field in onlyInB)
            {
                Console.WriteLine($"  {this.GetFieldSignature(field)}");
            }
        }

        var commonButDifferent = new List<(FieldInfo, FieldInfo)>();
        foreach (var fieldA in fieldsA)
        {
            foreach (var fieldB in fieldsB)
            {
                if (fieldA.Name == fieldB.Name && !this.AreFieldsEqual(fieldA, fieldB))
                {
                    commonButDifferent.Add((fieldA, fieldB));
                }
            }
        }

        if (commonButDifferent.Any())
        {
            Console.WriteLine("Общие поля с отличиями:");
            foreach (var (fieldA, fieldB) in commonButDifferent)
            {
                Console.WriteLine($"  A: {this.GetFieldSignature(fieldA)}");
                Console.WriteLine($"  B: {this.GetFieldSignature(fieldB)}");
                Console.WriteLine($"  Отличия: {this.GetFieldDifferences(fieldA, fieldB)}");
                Console.WriteLine();
            }
        }
    }

    private void CompareProperties(Type typeA, Type typeB)
    {
        var propsA = this.GetProperties(typeA);
        var propsB = this.GetProperties(typeB);

        Console.WriteLine("СВОЙСТВА:");

        var onlyInA = propsA.Where(pa => !propsB.Any(pb => this.ArePropertiesEqual(pa, pb)));
        if (onlyInA.Any())
        {
            Console.WriteLine("Только в классе A:");
            foreach (var prop in onlyInA)
            {
                Console.WriteLine($"  {this.GetPropertySignature(prop)}");
            }
        }

        var onlyInB = propsB.Where(pb => !propsA.Any(pa => this.ArePropertiesEqual(pa, pb)));
        if (onlyInB.Any())
        {
            Console.WriteLine("Только в классе B:");
            foreach (var prop in onlyInB)
            {
                Console.WriteLine($"  {this.GetPropertySignature(prop)}");
            }
        }
    }

    private void CompareMethods(Type typeA, Type typeB)
    {
        var methodsA = this.GetMethods(typeA);
        var methodsB = this.GetMethods(typeB);

        Console.WriteLine("МЕТОДЫ:");

        var onlyInA = methodsA.Where(ma => !methodsB.Any(mb => this.AreMethodsEqual(ma, mb)));
        if (onlyInA.Any())
        {
            Console.WriteLine("Только в классе A:");
            foreach (var method in onlyInA)
            {
                Console.WriteLine($"  {this.GetMethodSignature(method)}");
            }
        }

        var onlyInB = methodsB.Where(mb => !methodsA.Any(ma => this.AreMethodsEqual(ma, mb)));
        if (onlyInB.Any())
        {
            Console.WriteLine("Только в классе B:");
            foreach (var method in onlyInB)
            {
                Console.WriteLine($"  {this.GetMethodSignature(method)}");
            }
        }

        var commonButDifferent = new List<(MethodInfo, MethodInfo)>();
        foreach (var methodA in methodsA)
        {
            foreach (var methodB in methodsB)
            {
                if (methodA.Name == methodB.Name && !this.AreMethodsEqual(methodA, methodB))
                {
                    commonButDifferent.Add((methodA, methodB));
                }
            }
        }

        if (commonButDifferent.Any())
        {
            Console.WriteLine("Общие методы с отличиями:");
            foreach (var (methodA, methodB) in commonButDifferent)
            {
                Console.WriteLine($"  A: {this.GetMethodSignature(methodA)}");
                Console.WriteLine($"  B: {this.GetMethodSignature(methodB)}");
                Console.WriteLine($"  Отличия: {this.GetMethodDifferences(methodA, methodB)}");
                Console.WriteLine();
            }
        }
    }

    private void CompareConstructors(Type typeA, Type typeB)
    {
        var ctorsA = this.GetConstructors(typeA);
        var ctorsB = this.GetConstructors(typeB);

        Console.WriteLine("КОНСТРУКТОРЫ:");

        var onlyInA = ctorsA.Where(ca => !ctorsB.Any(cb => this.AreConstructorsEqual(ca, cb)));
        if (onlyInA.Any())
        {
            Console.WriteLine("Только в классе A:");
            foreach (var ctor in onlyInA)
            {
                Console.WriteLine($"  {this.GetConstructorSignature(ctor)}");
            }
        }

        var onlyInB = ctorsB.Where(cb => !ctorsA.Any(ca => this.AreConstructorsEqual(ca, cb)));
        if (onlyInB.Any())
        {
            Console.WriteLine("Только в классе B:");
            foreach (var ctor in onlyInB)
            {
                Console.WriteLine($"  {this.GetConstructorSignature(ctor)}");
            }
        }
    }

    private List<FieldInfo> GetFields(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.Static |
                            BindingFlags.DeclaredOnly)
                  .ToList();
    }

    private List<PropertyInfo> GetProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.DeclaredOnly)
                  .ToList();
    }

    private List<MethodInfo> GetMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                             BindingFlags.Instance | BindingFlags.Static |
                             BindingFlags.DeclaredOnly)
                  .Where(m => !m.IsSpecialName && !m.IsConstructor)
                  .ToList();
    }

    private List<ConstructorInfo> GetConstructors(Type type)
    {
        return type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic |
                                  BindingFlags.Instance | BindingFlags.Static |
                                  BindingFlags.DeclaredOnly)
                  .ToList();
    }

    private bool AreFieldsEqual(FieldInfo a, FieldInfo b)
    {
        return a.Name == b.Name &&
               a.FieldType == b.FieldType &&
               a.IsPublic == b.IsPublic &&
               a.IsPrivate == b.IsPrivate &&
               a.IsFamily == b.IsFamily &&
               a.IsStatic == b.IsStatic &&
               a.IsInitOnly == b.IsInitOnly &&
               a.IsLiteral == b.IsLiteral;
    }

    private bool ArePropertiesEqual(PropertyInfo a, PropertyInfo b)
    {
        return a.Name == b.Name &&
               a.PropertyType == b.PropertyType &&
               a.CanRead == b.CanRead &&
               a.CanWrite == b.CanWrite;
    }

    private bool AreMethodsEqual(MethodInfo a, MethodInfo b)
    {
        if (a.Name != b.Name)
        {
            return false;
        }

        if (a.ReturnType != b.ReturnType)
        {
            return false;
        }

        if (a.IsStatic != b.IsStatic)
        {
            return false;
        }

        if (a.IsGenericMethod != b.IsGenericMethod)
        {
            return false;
        }

        var paramsA = a.GetParameters();
        var paramsB = b.GetParameters();

        if (paramsA.Length != paramsB.Length)
        {
            return false;
        }

        for (int i = 0; i < paramsA.Length; i++)
        {
            if (paramsA[i].ParameterType != paramsB[i].ParameterType)
            {
                return false;
            }

            if (paramsA[i].IsOut != paramsB[i].IsOut)
            {
                return false;
            }

            if (paramsA[i].IsIn != paramsB[i].IsIn)
            {
                return false;
            }
        }

        return true;
    }

    private bool AreConstructorsEqual(ConstructorInfo a, ConstructorInfo b)
    {
        var paramsA = a.GetParameters();
        var paramsB = b.GetParameters();

        if (paramsA.Length != paramsB.Length)
        {
            return false;
        }

        for (int i = 0; i < paramsA.Length; i++)
        {
            if (paramsA[i].ParameterType != paramsB[i].ParameterType)
            {
                return false;
            }
        }

        return true;
    }

    private string GetFieldSignature(FieldInfo field)
    {
        var modifiers = new List<string>();

        if (field.IsPublic)
        {
            modifiers.Add("public");
        }
        else if (field.IsPrivate)
        {
            modifiers.Add("private");
        }
        else if (field.IsFamily)
        {
            modifiers.Add("protected");
        }
        else if (field.IsAssembly)
        {
            modifiers.Add("internal");
        }
        else if (field.IsFamilyOrAssembly)
        {
            modifiers.Add("protected internal");
        }
        else if (field.IsFamilyAndAssembly)
        {
            modifiers.Add("private protected");
        }

        if (field.IsStatic)
        {
            modifiers.Add("static");
        }

        if (field.IsInitOnly)
        {
            modifiers.Add("readonly");
        }

        if (field.IsLiteral)
        {
            modifiers.Add("const");
        }

        return $"{string.Join(" ", modifiers)} {this.GetTypeName(field.FieldType)} {field.Name}";
    }

    private string GetPropertySignature(PropertyInfo property)
    {
        return $"{property.PropertyType.Name} {property.Name} " +
               $"{{ {(property.CanRead ? "get; " : "")}{(property.CanWrite ? "set; " : "")}}}";
    }

    private string GetMethodSignature(MethodInfo method)
    {
        var modifiers = new List<string>();

        if (method.IsPublic)
        {
            modifiers.Add("public");
        }
        else if (method.IsPrivate)
        {
            modifiers.Add("private");
        }
        else if (method.IsFamily)
        {
            modifiers.Add("protected");
        }
        else if (method.IsAssembly)
        {
            modifiers.Add("internal");
        }
        else if (method.IsFamilyOrAssembly)
        {
            modifiers.Add("protected internal");
        }
        else if (method.IsFamilyAndAssembly)
        {
            modifiers.Add("private protected");
        }

        if (method.IsStatic)
        {
            modifiers.Add("static");
        }

        if (method.IsAbstract)
        {
            modifiers.Add("abstract");
        }
        else if (method.IsVirtual)
        {
            modifiers.Add("virtual");
        }

        string returnType = method.ReturnType == typeof(void) ? "void" : method.ReturnType.Name;

        var parameters = method.GetParameters()
            .Select(p => $"{this.GetTypeName(p.ParameterType)} {p.Name}");

        string genericParams = method.IsGenericMethod ?
            $"<{string.Join(", ", method.GetGenericArguments().Select(g => g.Name))}>" : "";

        return $"{string.Join(" ", modifiers)} {returnType} {method.Name}{genericParams}" +
               $"({string.Join(", ", parameters)})";
    }

    private string GetConstructorSignature(ConstructorInfo constructor)
    {
        var parameters = constructor.GetParameters()
            .Select(p => $"{this.GetTypeName(p.ParameterType)} {p.Name}");

        return $"({string.Join(", ", parameters)})";
    }

    private string GetFieldDifferences(FieldInfo a, FieldInfo b)
    {
        var differences = new List<string>();

        if (a.FieldType != b.FieldType)
        {
            differences.Add($"тип: {a.FieldType.Name} vs {b.FieldType.Name}");
        }

        if (a.IsPublic != b.IsPublic)
        {
            differences.Add("модификатор public");
        }

        if (a.IsPrivate != b.IsPrivate)
        {
            differences.Add("модификатор private");
        }

        if (a.IsFamily != b.IsFamily)
        {
            differences.Add("модификатор protected");
        }

        if (a.IsStatic != b.IsStatic)
        {
            differences.Add("static");
        }

        if (a.IsInitOnly != b.IsInitOnly)
        {
            differences.Add("readonly");
        }

        if (a.IsLiteral != b.IsLiteral)
        {
            differences.Add("const");
        }

        return string.Join(", ", differences);
    }

    private string GetMethodDifferences(MethodInfo a, MethodInfo b)
    {
        var differences = new List<string>();

        if (a.ReturnType != b.ReturnType)
        {
            differences.Add($"возвращаемый тип: {a.ReturnType.Name} vs {b.ReturnType.Name}");
        }

        if (a.IsStatic != b.IsStatic)
        {
            differences.Add("static");
        }

        if (a.IsAbstract != b.IsAbstract)
        {
            differences.Add("abstract");
        }

        if (a.IsVirtual != b.IsVirtual)
        {
            differences.Add("virtual");
        }

        var paramsA = a.GetParameters();
        var paramsB = b.GetParameters();

        if (paramsA.Length != paramsB.Length)
        {
            differences.Add($"количество параметров: {paramsA.Length} vs {paramsB.Length}");
        }
        else
        {
            for (int i = 0; i < paramsA.Length; i++)
            {
                if (paramsA[i].ParameterType != paramsB[i].ParameterType)
                {
                    differences.Add($"тип параметра {i}: {paramsA[i].ParameterType.Name} vs {paramsB[i].ParameterType.Name}");
                }
            }
        }

        return string.Join(", ", differences);
    }
}