﻿using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BinaryVibrance.MLEM.Binding.Generator
{
    [Generator]
    public class BindingSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(PostInitialize);

            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new BindingSyntaxReceiver());
        }

        private void PostInitialize(GeneratorPostInitializationContext context)
        {
            var assembly = typeof(BindingSourceGenerator).Assembly;
            foreach (var name in assembly.GetManifestResourceNames().Where(rn => rn.EndsWith(".cs")))
            {
                using var stream = assembly.GetManifestResourceStream(name) ?? throw new Exception($"Error resolving embedded resource stream {name}");
                context.AddSource($"_{name.Replace(".cs", ".g.cs")}", SourceText.From(stream, canBeEmbedded: true));
            }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retrieve the populated receiver 
            if (!(context.SyntaxContextReceiver is BindingSyntaxReceiver receiver))
                return;

            // group the fields by class, and generate the source
            foreach (var group in receiver.Properties.GroupBy<IPropertySymbol, INamedTypeSymbol>(f => f.ContainingType, SymbolEqualityComparer.Default))
            {
                var generator = new BindingClassGenerator(context);
                var classSource = generator.GenerateClassSource(group.Key, group.ToList());
                if (classSource is null) continue;

                var sourceText = SourceText.From(classSource.NormalizeWhitespace().ToFullString(), Encoding.UTF8);
                context.AddSource($"{group.Key.Name}_BindingExtensions.g.cs", sourceText);
            }
        }
    }
}
