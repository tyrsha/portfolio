using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

                // Logger.Log($"EnumLookupGenerator.enum found: {enumSymbol.Name}");
                // [EnumLookup] Attribute 확인
                bool hasAttribute = enumSymbol.GetAttributes()
                    .Any(attr => attr.AttributeClass?.ToDisplayString() == 
                                 "RoslynCommon.EnumLookupAttribute");

                if (!hasAttribute)
                    continue;

                // 네임스페이스 처리
                var ns = GetFullNamespace(enumSymbol.ContainingNamespace);
                Logger.Log($"EnumLookupGenerator.enum hasAttribute found!!: {ns}.{enumSymbol.Name}");
                // 소스 코드 생성
                try
                {
                    GenerateLookupClass(context, enumSymbol);
                }
                catch (Exception e)
                {
                    Logger.Log($"EnumLookupGenerator Exception: {e}");
                }
            }
        }
        
        public class TemplateModel
        {
            public string EnumName { get; set; }
            public string Namespace { get; set; }
            public Dictionary<string, string> Members { get; set; }
        }

        private static void GenerateLookupClass(GeneratorExecutionContext context, INamedTypeSymbol enumSymbol)
        {
            Logger.Log($"EnumLookupGenerator.GenerateLookupClass: {enumSymbol.Name}");
            
            
            var assembly = typeof(EnumLookupGenerator).Assembly;
            // 1) 포함된 리소스 이름들 나열 (진단 메시지로 출력)
            var names = assembly.GetManifestResourceNames();
            foreach (var n in names)
            {
                Logger.Log($"EnumLookupGenerator.GetManifestResourceNames: {n}");
            }
            
            
            using var stream = assembly.GetManifestResourceStream("RoslynAnalyzer.EnumLookupExtensions.scriban");
            if (stream == null)
            {
                Logger.Log("stream is null");
                return;
            }
            using var reader = new StreamReader(stream);
            string templateText = reader.ReadToEnd();
            
            if (string.IsNullOrEmpty(templateText))
            {
                Logger.Log($"EnumLookupGenerator.cs: could not find EnumLookupExtensions.scriban");
                return;
            }

            var model = new TemplateModel();
            model.EnumName = enumSymbol.Name;
            model.Namespace = GetFullNamespace(enumSymbol.ContainingNamespace);
            model.Members = new Dictionary<string, string>();
            foreach (var member in enumSymbol.MemberNames)
            {
                model.Members.Add(member, $"{model.EnumName}.{member}");
            };
            
            var template = Template.Parse(templateText);
            var generatedCode = template.Render(model);
            
            context.AddSource($"{model.EnumName}Extensions.generated.cs", generatedCode);
            Logger.Log($"EnumLookupGenerator.code generatedCode: {generatedCode}");
            Logger.Log($"EnumLookupGenerator.code generated: {model.EnumName}");
        }

        private static string GetFullNamespace(INamespaceSymbol? symbol)
        {
            if (symbol == null || symbol.IsGlobalNamespace)
                return string.Empty;

            var stack = new Stack<string>();
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
