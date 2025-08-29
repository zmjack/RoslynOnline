using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Reflection;

namespace RoslynOnline.Pages
{
    public partial class Index
    {
        public string Output = "";
        const string DefaultCode = @"using System;

class Program
{
    public static void Main()
    {
        Console.WriteLine(""Hello World"");
    }
}";
        [JSInvokable]
        public static async Task<string> RunCode(string code)
        {
            return await JsRun(code);
        }

        [Inject] private HttpClient Client { get; set; }
        [Inject] private Monaco Monaco { get; set; }

        protected void OnInitialized()
        {
            Compiler.InitializeMetadataReferences(Client);
        }

        protected void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                Monaco.Initialize("container", DefaultCode, "csharp");
                Run();
            }
        }

        public Task Run()
        {
            return Compiler.WhenReady(() => RunInternal(Monaco.GetCode("container")));
        }
        public static async Task<string> JsRun(string code)
        {
            var Output = "";

            Console.WriteLine("Compiling and Running code");
            var sw = Stopwatch.StartNew();

            var currentOut = Console.Out;
            var writer = new StringWriter();
            Console.SetOut(writer);

            Exception exception = null;
            try
            {
                var (success, asm) = Compiler.LoadSource(code);
                if (success)
                {
                    var entry = asm.EntryPoint;
                    if (entry.Name == "<Main>") // sync wrapper over async Task Main
                    {
                        entry = entry.DeclaringType.GetMethod("Main", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // reflect for the async Task Main
                    }
                    var hasArgs = entry.GetParameters().Length > 0;
                    var result = entry.Invoke(null, hasArgs ? new object[] { new string[0] } : null);
                    if (result is Task t)
                    {
                        await t;
                    }
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            Output = writer.ToString();
            if (exception != null)
            {
                Output += "\r\n" + exception.ToString();
            }
            Console.SetOut(currentOut);

            sw.Stop();
            Console.WriteLine("Done in " + sw.ElapsedMilliseconds + "ms");

            return Output;
        }

        async Task RunInternal(string code)
        {
            Output = "";

            Console.WriteLine("Compiling and Running code");
            var sw = Stopwatch.StartNew();

            var currentOut = Console.Out;
            var writer = new StringWriter();
            Console.SetOut(writer);

            Exception exception = null;
            try
            {
                var (success, asm) = Compiler.LoadSource(code);
                if (success)
                {
                    var entry = asm.EntryPoint;
                    if (entry.Name == "<Main>") // sync wrapper over async Task Main
                    {
                        entry = entry.DeclaringType.GetMethod("Main", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // reflect for the async Task Main
                    }
                    var hasArgs = entry.GetParameters().Length > 0;
                    var result = entry.Invoke(null, hasArgs ? new object[] { new string[0] } : null);
                    if (result is Task t)
                    {
                        await t;
                    }
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            Output = writer.ToString();
            if (exception != null)
            {
                Output += "\r\n" + exception.ToString();
            }
            Console.SetOut(currentOut);

            sw.Stop();
            Console.WriteLine("Done in " + sw.ElapsedMilliseconds + "ms");
        }
    }
}
