using System.Reflection;

namespace RoslynOnline
{
    public static class AssemblyExtentions
    {
        public static MethodInfo GetEntryMethod(this Assembly asm)
        {
            var entry = asm.EntryPoint;
            // sync wrapper over async Task Main
            if (entry!.Name == "<Main>")
            {
                // reflect for the async Task Main
                entry = entry.DeclaringType!.GetMethod("Main", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }
            return entry!;
        }
    }
}
