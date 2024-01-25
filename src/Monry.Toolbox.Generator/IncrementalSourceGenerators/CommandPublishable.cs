using Microsoft.CodeAnalysis;

namespace Monry.Toolbox.IncrementalSourceGenerators;

[Generator(LanguageNames.CSharp)]
public class CommandPublishable : IIncrementalGenerator
{
    private const string Namespace = "Monry.Toolbox.Attributes";
    private const string AttributeName = "CommandPublishableAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var source = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                $"{Namespace}.{AttributeName}",
                // 絞り込みは属性だけで OK
                (_, _) => true,
                (syntaxContext, _) => syntaxContext
            );
        context.RegisterPostInitializationOutput(OnPostInitialization);
        context.RegisterSourceOutput(source, Emit);
    }

    private static void OnPostInitialization(IncrementalGeneratorPostInitializationContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        context.AddSource(
            AttributeName,
            // lang=csharp
            $$"""
            namespace {{Namespace}}
            {
                [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                sealed class {{AttributeName}} : System.Attribute
                {
                    public {{AttributeName}}()
                    {
                    }
                }
            }
            """);
    }

    private static void Emit(SourceProductionContext context, GeneratorAttributeSyntaxContext syntaxContext)
    {
        var typeSymbol = (INamedTypeSymbol)syntaxContext.TargetSymbol;
        var isGlobalNamespace = typeSymbol.ContainingNamespace.IsGlobalNamespace;
        var @namespace = isGlobalNamespace
            ? string.Empty
            : $"namespace {typeSymbol.ContainingNamespace.ToDisplayString()}";
        var fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "")
            .Replace("<", "_")
            .Replace(">", "_");
        var indent = isGlobalNamespace ? string.Empty : "    ";

        var code = $$"""
            // <auto-generated />
            using Monry.Toolbox.VitalRouter;
            using VContainer;
            using VitalRouter;

            {{@namespace}}
            {{(isGlobalNamespace ? string.Empty : "{")}}
            {{indent}}partial class {{typeSymbol.Name}} : ICommandPublishable
            {{indent}}{
            {{indent}}    [Inject] public ICommandPublisher CommandPublisher { get; set; } = default!;
            {{indent}}}
            {{(isGlobalNamespace ? string.Empty : "}")}}
            """;
        context.AddSource($"{fullTypeName}.CommandPublishable.g.cs", code);
    }
}
