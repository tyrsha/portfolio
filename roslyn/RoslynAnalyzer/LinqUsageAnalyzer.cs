using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LinqUsageAnalyzer : DiagnosticAnalyzer
    {
        private const string DiagnosticId = "TEST001";

        private static readonly LocalizableString Title = "Avoid LINQ usage in Unity runtime code";
        private static readonly LocalizableString MessageFormat = "LINQ usage detected: '{0}'. Consider using explicit loops or pooling to avoid allocations and GC pressure in Unity.";
        private static readonly LocalizableString Description = "LINQ (System.Linq) can cause hidden allocations and performance issues on Unity's runtime. Flagging uses helps maintain high-performance code paths.";
        private const string Category = "Performance";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            // Configure analyzer behavior
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register for invocation expressions to catch System.Linq extension method calls
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // Get the symbol for the invoked method
            var semanticModel = context.SemanticModel;
            var symbolInfo = semanticModel.GetSymbolInfo(invocation, context.CancellationToken);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

            // If multiple candidate symbols (overload resolution failed), try the first candidate
            if (methodSymbol == null && symbolInfo.CandidateSymbols.Length > 0)
            {
                methodSymbol = symbolInfo.CandidateSymbols[0] as IMethodSymbol;
            }

            if (methodSymbol == null)
                return;

            // Walk up containing namespace to see if it is System.Linq
            var ns = methodSymbol.ContainingNamespace;
            if (ns == null)
                return;

            // Example full namespace: System.Linq
            var nsName = ns.ToDisplayString();

            if (nsName != "System.Linq" && !nsName.StartsWith("System.Linq.")) return;
            
            // Build a friendly name for the invoked expression
            var invokedText = invocation.Expression?.ToString() ?? methodSymbol.Name;
            var location = invocation.GetLocation();
            var diag = Diagnostic.Create(Rule, location, invokedText);
            context.ReportDiagnostic(diag);
        }
    }
}

