using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace MsgPackSerializerGenerator
{
    public static class MsgPackSerializerGenerator
    {
        private static HashSet<Type> _generatedTypes = new HashSet<Type>();

        public static string GenerateSerializer(Type rootType)
        {
            _generatedTypes.Clear();
            var unit = CompilationUnit()
                .AddUsings(
                    UsingDirective(IdentifierName("System")),
                    UsingDirective(IdentifierName("System.IO")),
                    UsingDirective(IdentifierName("System.Text")),
                    UsingDirective(IdentifierName("System.Collections.Generic")),
                    UsingDirective(IdentifierName("DataModels"))
                );

            var serializers = new List<MemberDeclarationSyntax>();
            var typeQueue = new Queue<Type>();
            typeQueue.Enqueue(rootType);

            while (typeQueue.Count > 0)
            {
                var type = typeQueue.Dequeue();
                if (_generatedTypes.Contains(type))
                    continue;

                _generatedTypes.Add(type);

                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                var methodStatements = new List<StatementSyntax>
                {
                    LocalDeclarationStatement(
                        VariableDeclaration(IdentifierName("var"))
                        .AddVariables(
                            VariableDeclarator("ms")
                            .WithInitializer(
                                EqualsValueClause(ObjectCreationExpression(IdentifierName("MemoryStream"))
                                .WithArgumentList(ArgumentList()))
                            )
                        )
                    ),
                    ExpressionStatement(
                        InvocationExpression(MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression, IdentifierName("ms"), IdentifierName("WriteByte")))
                        .WithArgumentList(ArgumentList(
                            SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0x80 | props.Length)))))
                        )
                    )
                };

                foreach (var prop in props)
                {
                    var name = prop.Name;
                    var propType = prop.PropertyType;

                    methodStatements.AddRange(new[]
                    {
                        // Write the key string
                        ExpressionStatement(
                            InvocationExpression(IdentifierName("WriteString"))
                            .WithArgumentList(ArgumentList(SeparatedList(new[]
                            {
                                Argument(IdentifierName("ms")),
                                Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(name)))
                            })))
                        )
                    });

                    if (propType == typeof(string))
                    {
                        methodStatements.Add(
                            ExpressionStatement(
                                InvocationExpression(IdentifierName("WriteString"))
                                .WithArgumentList(ArgumentList(SeparatedList(new[]
                                {
                                    Argument(IdentifierName("ms")),
                                    Argument(MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("obj"),
                                        IdentifierName(name)))
                                })))
                            )
                        );
                    }
                    else if (propType == typeof(DateTime))
                    {
                        methodStatements.Add(
                            ExpressionStatement(
                                InvocationExpression(IdentifierName("WriteDateTimeExt"))
                                .WithArgumentList(ArgumentList(SeparatedList(new[]
                                {
                                    Argument(IdentifierName("ms")),
                                    Argument(MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("obj"),
                                        IdentifierName(name)))
                                })))));
                    }
                    else if (propType == typeof(int))
                    {
                        methodStatements.Add(
                            ExpressionStatement(
                                InvocationExpression(IdentifierName("WriteInt"))
                                .WithArgumentList(ArgumentList(SeparatedList(new[]
                                {
                                    Argument(IdentifierName("ms")),
                                    Argument(MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("obj"),
                                        IdentifierName(name)))
                                })))
                            )
                        );
                    }
                    else if (propType.IsEnum)
                    {
                        methodStatements.Add(
                            ExpressionStatement(
                                InvocationExpression(IdentifierName("WriteInt"))
                                .WithArgumentList(ArgumentList(SeparatedList(new[]
                                {
                                    Argument(IdentifierName("ms")),
                                    Argument(
                                        CastExpression(
                                            PredefinedType(Token(SyntaxKind.IntKeyword)),
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("obj"),
                                                IdentifierName(name)
                                            )
                                        )
                                    )
                                })))
                            )
                        );
                    }
                    else if (propType == typeof(byte[]))
                    {
                        methodStatements.Add(
                            ExpressionStatement(
                                InvocationExpression(IdentifierName("WriteBytes"))
                                .WithArgumentList(ArgumentList(SeparatedList(new[]
                                {
                                    Argument(IdentifierName("ms")),
                                    Argument(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("obj"),
                                            IdentifierName(name)))
                                })))
                            )
                        );
                    }
                    else if (propType == typeof(bool))
                    {
                        methodStatements.Add(
                            ExpressionStatement(
                                InvocationExpression(IdentifierName("WriteBool"))
                                .WithArgumentList(ArgumentList(SeparatedList(new[]
                                {
                                    Argument(IdentifierName("ms")),
                                    Argument(MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("obj"),
                                        IdentifierName(name)))
                                })))
                            )
                        );
                    }
                    else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        var itemType = propType.GetGenericArguments()[0];
                        typeQueue.Enqueue(itemType);

                        // Wrap the statements wanted in a fake temp method so they can be extracted as a syntax tree
                        // This fixes the formatting in the final generated code.
                        var blockCode = $@"
namespace DummyNamespace
{{
    public class DummyClass
    {{
        public void DummyMethod()
        {{
            WriteArrayHeader(ms, obj.{name}.Count);
            foreach (var item in obj.{name})
            {{
                var nestedBytes = {itemType.Name}Serializer.Serialize(item);
                ms.Write(nestedBytes, 0, nestedBytes.Length);
            }}
        }}
    }}
}}";
                        var tree = CSharpSyntaxTree.ParseText(blockCode);
                        var root = tree.GetCompilationUnitRoot();
                        var method = root
                            .DescendantNodes()
                            .OfType<MethodDeclarationSyntax>()
                            .FirstOrDefault();
                        if (method == null)
                            throw new InvalidOperationException("Failed to parse generated dummy method");

                        methodStatements.AddRange(method.Body.Statements);
                    }
                    else if (!propType.IsPrimitive && propType != typeof(string))
                    {
                        typeQueue.Enqueue(propType);
                        methodStatements.Add(ParseStatement($@"
var nested = {propType.Name}Serializer.Serialize(obj.{name});
ms.Write(nested, 0, nested.Length);
"));
                    }
                }

                methodStatements.Add(ReturnStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("ms"), IdentifierName("ToArray")))));

                var serializeMethod = MethodDeclaration(
                        ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)))
                            .WithRankSpecifiers(SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(OmittedArraySizeExpression())))),
                        Identifier("Serialize"))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier("obj")).WithType(IdentifierName(type.Name)))
                    .WithBody(Block(methodStatements));

                var helpers = GenerateHelperMethods();

                var classDecl = ClassDeclaration(type.Name + "Serializer")
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                    .AddMembers(new MemberDeclarationSyntax[] { serializeMethod }.Concat(helpers).ToArray());

                serializers.Add(classDecl);
            }

            var ns = NamespaceDeclaration(IdentifierName("Generated")).AddMembers(serializers.ToArray());
            unit = unit.AddMembers(ns);

            return unit.NormalizeWhitespace().ToFullString();
        }

        private static MemberDeclarationSyntax[] GenerateHelperMethods()
        {
            return new[]
            {
                ParseMemberDeclaration(@"
public static void WriteString(Stream s, string str)
{
    var b = Encoding.UTF8.GetBytes(str);
    if (b.Length <= 31)
    {
        s.WriteByte((byte)(0xA0 | b.Length));
    }
    else
    {
        s.WriteByte(0xD9);
        s.WriteByte((byte)b.Length);
    }
    s.Write(b, 0, b.Length);
}"),
                ParseMemberDeclaration(@"
public static void WriteInt(Stream s, int val)
{
    if (val >= 0 && val <= 127)
    {
        s.WriteByte((byte)val);
    }
    else if (val >= 0 && val <= 255)
    {
        s.WriteByte(0xCC);
        s.WriteByte((byte)val);
    }
    else if (val >= 0 && val <= 65535)
    {
        s.WriteByte(0xCD);
        byte[] b = BitConverter.GetBytes((ushort)val);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(b);
        s.Write(b, 0, 2);
    }
    else
    {
        s.WriteByte(0xD2);
        byte[] b = BitConverter.GetBytes(val);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(b);
        s.Write(b, 0, 4);
    }
}"),
                ParseMemberDeclaration(@"
public static void WriteBytes(Stream s, byte[] data)
{
    if (data == null)
    {
        s.WriteByte(0xC0);  // null
        return;
    }
    int len = data.Length;
    if (len <= 255)
    {
        s.WriteByte(0xC4);
        s.WriteByte((byte)len);
    }
    else if (len <= 65535)
    {
        s.WriteByte(0xC5);
        byte[] b = BitConverter.GetBytes((ushort)len);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(b);
        s.Write(b, 0, 2);
    }
    else
    {
        s.WriteByte(0xC6);
        byte[] b = BitConverter.GetBytes(len);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(b);
        s.Write(b, 0, 4);
    }
    s.Write(data, 0, len);
}"),
                ParseMemberDeclaration(@"
public static void WriteDateTimeExt(Stream s, DateTime val)
{
    var utc = val.ToUniversalTime();
    long seconds = (long)(utc - new DateTime(1970, 1, 1)).TotalSeconds;
    int nanoseconds = (int)((utc.Ticks % TimeSpan.TicksPerSecond) * 100);
    if (nanoseconds == 0 && seconds >= 0 && seconds <= uint.MaxValue)
    {
        // timestamp32
        s.WriteByte(0xD6);
        s.WriteByte(0xFF);
        byte[] sec = BitConverter.GetBytes((uint)seconds);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(sec);
        s.Write(sec, 0, 4);
    }
    else if (seconds >= 0 && seconds <= ((1L << 34) - 1))
    {
        // timestamp64
        ulong data = ((ulong)nanoseconds << 34) | (ulong)seconds;
        s.WriteByte(0xD7);
        s.WriteByte(0xFF);
        byte[] b = BitConverter.GetBytes(data);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(b);
        s.Write(b, 0, 8);
    }
    else
    {
        // timestamp96
        s.WriteByte(0xC7);  // ext 8
        s.WriteByte(12);     // length
        s.WriteByte(0xFF);  // type -1 (ext)
        byte[] ns = BitConverter.GetBytes((uint)nanoseconds);
        byte[] sec = BitConverter.GetBytes(seconds);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(ns);
            Array.Reverse(sec);
        }
        s.Write(ns, 0, 4);
        s.Write(sec, 0, 8);
    }
}
"),
                ParseMemberDeclaration(@"
public static void WriteBool(Stream s, bool val)
{
    s.WriteByte(val ? (byte)0xC3 : (byte)0xC2);
}"),
                ParseMemberDeclaration(@"
public static void WriteArrayHeader(Stream s, int count)
{
    if (count <= 15)
    {
        s.WriteByte((byte)(0x90 | count));
    }
    else
    {
        s.WriteByte(0xDC);
        byte[] b = BitConverter.GetBytes((ushort)count);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(b);
        s.Write(b, 0, 2);
    }
}")
            };
        }
    }
}
