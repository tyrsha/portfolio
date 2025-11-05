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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using Scriban;

namespace Roslyn
{
    [Generator]
    public class EnumLookupGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            Logger.Log("EnumLookupGenerator.Execute");
            // 모든 EnumDeclarationSyntax를 찾기
            var enumDeclarations = context.Compilation.SyntaxTrees
                .SelectMany(st => st.GetRoot().DescendantNodes().OfType<EnumDeclarationSyntax>());

            foreach (var enumSyntax in enumDeclarations)
            {
                var model = context.Compilation.GetSemanticModel(enumSyntax.SyntaxTree);
                var enumSymbol = model.GetDeclaredSymbol(enumSyntax);

                if (enumSymbol == null)
                    continue;

                Logger.Log($"EnumLookupGenerator.enum found: {enumSymbol.Name}");
                // [EnumLookup] Attribute 확인
                bool hasAttribute = enumSymbol.GetAttributes()
                    .Any(attr => attr.AttributeClass?.ToDisplayString() == 
                                 "Roslyn.EnumLookupAttribute");

                if (!hasAttribute)
                    continue;

                // 네임스페이스 처리
                Logger.Log($"EnumLookupGenerator.enum hasAttribute found: {enumSymbol.Name}");
                string ns = GetFullNamespace(enumSymbol.ContainingNamespace);
                string enumName = enumSymbol.Name;

                // 소스 코드 생성
                var source = GenerateLookupClass(context, ns, enumName, enumSymbol);

                // AddSource
                context.AddSource($"{enumName}_EnumLookup.generated.cs", source);
                Logger.Log($"EnumLookupGenerator.code generated: {enumSymbol.Name}");
            }
        }

        private static string GenerateLookupClass(GeneratorExecutionContext context, string ns, string enumName,
            INamedTypeSymbol enumSymbol)
        {        
            var assets = new Dictionary<string, string>
            {
                { "enumName", enumName },
                { "Audio", "Audio" },
                { "Binary", "Binary" }
            };
            
            var templateFile = context.AdditionalFiles
                .FirstOrDefault(x => Path.GetFileName(x.Path) == "EnumLookupExtensions.scriban");

            if (templateFile == null)
                return null;

            var templateText = templateFile.GetText(context.CancellationToken)!.ToString();
            var template = Template.Parse(templateText);

            var generatedCode = template.Render(new { assets });
            return generatedCode;
        }

        private static string GetFullNamespace(INamespaceSymbol? symbol)
        {
            if (symbol == null || symbol.IsGlobalNamespace)
                return string.Empty;

            var stack = new System.Collections.Generic.Stack<string>();
            var current = symbol;
            while (current != null && !current.IsGlobalNamespace)
            {
                stack.Push(current.Name);
                current = current.ContainingNamespace;
            }

            return string.Join(".", stack);
        }
    }
}
