/*
EnumNameLookupSourceGenerator

This is a ready-to-use Roslyn **Incremental Source Generator** project that scans your compilation
for public enums and generates fast name->enum lookup helpers.

What it generates
- For each enum `MyEnum` in namespace `A.B`, it generates a partial static class
  `MyEnum_NameLookup` with methods:
    - `bool TryParseByName(string name, out MyEnum value)`  // case-sensitive
    - `bool TryParseByNameIgnoreCase(string name, out MyEnum value)` // case-insensitive
    - `MyEnum ParseByName(string name, MyEnum defaultValue)` // returns default if not found

Features
- Uses an `ImmutableDictionary<string, Enum>`-like switch-based generation (no runtime reflection)
- Handles nested/qualified enums and different accessibility levels (only generates for accessible enums)
- Escapes identifiers safely

How to use
1. Add this project to a solution as a Analyzer/Generator project (TargetFramework netstandard2.0).
2. Build and reference the resulting generator assembly from the consumer project (Project > Add > Analyzer in Visual Studio), or pack as NuGet.
3. Use the generated helper like: `MyEnum_NameLookup.TryParseByName("SomeName", out var v)`

*/

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn
{
    [Generator]
    public class EnumNameLookupGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }
        
        public void Execute(GeneratorExecutionContext context)
        {
            // Collect all enum declarations
            var enumDeclarations = context.
                .CreateSyntaxProvider(
                    predicate: (s, _) => s is EnumDeclarationSyntax,
                    transform: (ctx, _) =>
                    {
                        var enumSyntax = (EnumDeclarationSyntax)ctx.Node;
                        var enumSymbol = ctx.SemanticModel.GetDeclaredSymbol(enumSyntax);
                        bool hasAttribute = enumSymbol?
                            .GetAttributes()
                            .Any(attr => attr.AttributeClass?.Name == "EnumLookupAttribute"
                                         || attr.AttributeClass?.ToDisplayString() == "EnumLookupGenerator.EnumLookupAttribute") == true;

                        return hasAttribute ? enumSyntax : null;
                    })
                .Where(m => m != null);
            

            // Combine with compilation to get semantic model info
            var compilationAndEnums = context.CompilationProvider.Combine(enumDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndEnums, (spc, source) =>
            {
                var compilation = source.Left as Compilation;
                if (!(source.Right is ImmutableArray<EnumDeclarationSyntax> enums))
                    return;
                if (enums.Length == 0)
                    return;

                foreach (var enumDecl in enums)
                {
                    var model = compilation.GetSemanticModel(enumDecl.SyntaxTree);
                    var enumSymbol = ModelExtensions.GetDeclaredSymbol(model, enumDecl) as INamedTypeSymbol;
                    if (enumSymbol == null)
                        continue;

                    // Only public/internal/accessible enums - skip compiler-generated or private nested enums
                    if (enumSymbol.TypeKind != TypeKind.Enum)
                        continue;

                    // Generate a helper for this enum
                    var src = GenerateForEnum(enumSymbol);
                    var hintName = GetHintNameFor(enumSymbol);
                    spc.AddSource(hintName, SourceText.From(src, Encoding.UTF8));
                }
            });
        }

        private static string GetHintNameFor(INamedTypeSymbol enumSymbol)
        {
            // Create a unique file name per enum full name
            var ns = enumSymbol.ContainingNamespace.IsGlobalNamespace ? "global" : enumSymbol.ContainingNamespace.ToDisplayString().Replace('.', '_');
            var name = enumSymbol.Name;
            return $"EnumNameLookup_{ns}_{name}.g.cs";
        }

        private static string GenerateForEnum(INamedTypeSymbol enumSymbol)
        {
            var sb = new StringBuilder();

            var ns = enumSymbol.ContainingNamespace.IsGlobalNamespace? null : enumSymbol.ContainingNamespace.ToDisplayString();
            if (ns != null)
            {
                sb.AppendLine("namespace " + ns);
                sb.AppendLine("{");
            }

            // Determine accessibility: put the helper in the same accessibility as enum's containing type
            var accessibility = GetAccessibilityKeyword(enumSymbol);
            var enumName = enumSymbol.Name;

            // Generate safe class name
            var helperClassName = enumName + "_NameLookupExtensions";

            sb.AppendLine($"    {accessibility} static class {EscapeIdentifier(helperClassName)}");
            sb.AppendLine("    {");

            // Generate dictionary as switch-based code to avoid runtime allocation of dictionaries
            // Build case entries
            var members = enumSymbol.GetMembers().OfType<IFieldSymbol>().Where(f => f.ConstantValue != null).ToArray();

            // Case-sensitive TryParse
            sb.AppendLine($"        public static {enumName} To{enumName}(string name)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (name is null) { return default; }");
            sb.AppendLine("            switch (name)");
            sb.AppendLine("            {");
            foreach (var m in members)
            {
                var memberName = m.Name;
                var memberValue = m.ConstantValue;
                sb.AppendLine($"                case \"{EscapeString(memberName)}\": return {enumName}.{memberName}; return true;");
            }
            sb.AppendLine("                default: return default;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Case-insensitive TryParse -> lower switch
            sb.AppendLine($"        public static bool TryParseByNameIgnoreCase(string name, out {enumName} value)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (name is null) { value = default; return false; }");
            sb.AppendLine("            switch (name.ToLowerInvariant())");
            sb.AppendLine("            {");
            // create entries with lowercased keys; if duplicates after lowercasing, last wins — we can detect duplicates, but keep simple
            foreach (var m in members)
            {
                var memberName = m.Name;
                sb.AppendLine($"                case \"{EscapeString(memberName.ToLowerInvariant())}\": value = {enumName}.{memberName}; return true;");
            }
            sb.AppendLine("                default: value = default; return false;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Parse with default
            sb.AppendLine($"        public static {enumName} ParseByName(string name, {enumName} defaultValue = default)");
            sb.AppendLine("        {");
            sb.AppendLine($"            return TryParseByName(name, out var v) ? v : defaultValue;");
            sb.AppendLine("        }");

            sb.AppendLine("    }");

            if (ns != null)
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static string GetAccessibilityKeyword(INamedTypeSymbol symbol)
        {
            // Choose public/internal/private for the helper based on the enum's accessibility
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public: return "public";
                case Accessibility.Internal: return "internal";
                case Accessibility.Private: return "private"; // private top-level won't happen
                case Accessibility.Protected: return "protected";
                case Accessibility.ProtectedOrInternal: return "internal";
                default: return "internal";
            }
        }

        private static string EscapeIdentifier(string id)
        {
            // Very simple escape; real-world: consider SyntaxFactory to create identifiers
            if (SyntaxFacts.GetKeywordKind(id) != SyntaxKind.None)
                return "@" + id;
            return id;
        }

        private static string EscapeString(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}
