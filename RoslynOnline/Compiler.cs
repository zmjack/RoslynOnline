using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Reflection;

namespace RoslynOnline
{
    public static class Compiler
    {
        class BlazorBoot
        {
            public bool cacheBootResources { get; set; }
            public object[] config { get; set; }
            public bool debugBuild { get; set; }
            public string entryAssembly { get; set; }
            public bool linkerEnabled { get; set; }
            public Resources resources { get; set; }
        }

        class Resources
        {
            public Dictionary<string, string> assembly { get; set; }
            public Dictionary<string, string> pdb { get; set; }
            public Dictionary<string, string> runtime { get; set; }
        }

        private static Task InitializationTask;
        private static List<MetadataReference> References;

        public static void InitializeMetadataReferences(HttpClient client)
        {
            async Task InitializeInternal()
            {
                var response = await client.GetFromJsonAsync<BlazorBoot>("_framework/blazor.boot.json");
                var assemblies = await Task.WhenAll(response.resources.assembly.Keys.Select(x => client.GetAsync("_framework/" + x)));

                var references = new List<MetadataReference>(assemblies.Length);
                foreach (var asm in assemblies)
                {
                    using (var task = await asm.Content.ReadAsStreamAsync())
                    {
                        references.Add(MetadataReference.CreateFromStream(task));
                    }
                }

                References = references;
            }
            InitializationTask = InitializeInternal();
        }

        public static async Task ReadyToRun(Func<Task> action)
        {
            if (InitializationTask.Status != TaskStatus.RanToCompletion)
            {
                await InitializationTask.ContinueWith(x => action());
            }
            else
            {
                await action();
            }
        }

        public static (bool success, Assembly asm) LoadSource(string source)
        {
            var compilation = CSharpCompilation.Create("Script")
                .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
                .AddReferences(References)
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)));

            ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

            bool error = false;
            foreach (Diagnostic diag in diagnostics)
            {
                switch (diag.Severity)
                {
                    case DiagnosticSeverity.Info:
                        Console.WriteLine(diag.ToString());
                        break;
                    case DiagnosticSeverity.Warning:
                        Console.WriteLine(diag.ToString());
                        break;
                    case DiagnosticSeverity.Error:
                        error = true;
                        Console.WriteLine(diag.ToString());
                        break;
                }
            }

            if (error)
            {
                return (false, null);
            }

            using (var outputAssembly = new MemoryStream())
            {
                compilation.Emit(outputAssembly);
                return (true, Assembly.Load(outputAssembly.ToArray()));
            }
        }

        public static string Format(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = tree.GetRoot();
            var normalized = root.NormalizeWhitespace();
            return normalized.ToString();
        }
    }
}
