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
            RandomContainerGenerator generator = new RandomContainerGenerator();
            await generator.Build(new CancellationTokenSource().Token);

            AfirusSggmrssGenerator afirusGen = new AfirusSggmrssGenerator();
            //await afirusGen.CorrectGeopcdxReference();
            await afirusGen.GenerateVariants();
            await afirusGen.GenerateReskinBlueprints();
            //await afirusGen.CreatePreloadBlueprint();

            //await Scripts.CreateRandomSkins();
            //await Scripts.ConvertToPNGsForPreview();

            //CTSgnsGenerator gen = new CTSgnsGenerator();
            //await gen.GenerateVariants();
        }
    }
}
