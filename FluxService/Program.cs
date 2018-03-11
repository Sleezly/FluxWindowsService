using Topshelf;

namespace FluxService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<FluxWindowsService>(p =>
                {
                    p.ConstructUsing(name => new FluxWindowsService());
                    p.WhenStarted(tc => tc.Start());
                    p.WhenStopped(tc => tc.Stop());
                });

                x.RunAsLocalSystem();
                x.SetDescription("Flux Circadian Service");
                x.SetDisplayName("Flux Circadian Service");
                x.SetServiceName("Flux Circadian Service");
            });
        }
    }
}
