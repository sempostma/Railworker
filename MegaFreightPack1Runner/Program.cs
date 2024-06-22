using RailworkerMegaFreightPack1;

namespace MegaFreightPack1Runner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            Run().Wait() ;
        }

        static async Task Run()
        {
            CTSgnsGenerator gen = new CTSgnsGenerator();
            await gen.GenerateVariants();
        }
    }
}
