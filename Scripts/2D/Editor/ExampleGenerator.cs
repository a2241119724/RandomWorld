namespace LAB
{
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public sealed class ExampleGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx =>
            {
                var sourceText = @"
                namespace LAB
                {
                    public static class HelloWorld
                    {
                        public static void SayHello()
                        {
                            Console.WriteLine(""Hello From Generator"");
                        }
                    }
                }";
                ctx.AddSource("AAAAAA.g", SourceText.From(sourceText, Encoding.UTF8));
            });
        }
    }
}