using Microsoft.ServiceFabric.Services.Runtime;
using System.Threading;

namespace Application1.Frontend
{
    public class Program
    {
        // Entry point for the application.
        public static void Main(string[] args)
        {
            ServiceRuntime.RegisterServiceAsync("FrontendType", context => new WebHostingService(context)).GetAwaiter().GetResult();
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
