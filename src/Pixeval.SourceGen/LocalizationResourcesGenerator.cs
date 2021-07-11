﻿using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.CodeAnalysis;

namespace Pixeval.SourceGen
{
    // see https://platform.uno/blog/using-msbuild-items-and-properties-in-c-9-source-generators/
    [Generator]
    public class LocalizationResourcesGenerator : ISourceGenerator
    {
        private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

        public void Initialize(GeneratorInitializationContext context)
        {
            
        }
        
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.AdditionalFiles.Where(
                    f => context.AnalyzerConfigOptions.GetOptions(f).TryGetValue(SourceItemGroupMetadata, out var sourceItemGroup)
                         && sourceItemGroup == "PRIResource"
                         && f.Path.EndsWith(".resw")).ToImmutableArray() is var resources
                && resources.Any())
            {
                const char open = '{';
                const char close = '}';
                var stringBuilder = new IndentedStringBuilder(4);
                using (var namespaceBuilder = stringBuilder.Block("namespace Pixeval", open, close))
                {
                    foreach (var additionalText in resources)
                    {
                        var className = $"{Path.GetFileNameWithoutExtension(additionalText.Path)}Resources";
                        using var classBuilder = namespaceBuilder.Block($"public static class {className}", open, close);
                        classBuilder.AppendLine(@$"public static readonly Microsoft.ApplicationModel.Resources.ResourceLoader ResourceLoader = new(""{Path.GetFileNameWithoutExtension(additionalText.Path)}"");");
                        var doc = new XmlDocument();
                        doc.Load(additionalText.Path);
                        if (doc.SelectNodes("//data") is { } nodes)
                        {
                            var elements = nodes.Cast<XmlElement>();
                            foreach (var node in elements)
                            {
                                var name = node.GetAttribute("name");
                                if (node.ChildNodes.Cast<object>().FirstOrDefault(n => n is XmlElement {Name: "value"}) is XmlElement ele)
                                {
                                    classBuilder.AppendLine(@$"public const string {name.Replace(".", string.Empty)} = ""{ele.FirstChild.Value}"";");
                                }
                            }
                        }
                    }
                }
                
                context.AddSource("LocalizationResources", stringBuilder.ToString());
            }
        }
    }
}