using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Threading;

namespace Application1.ValuesService
{
    public class Program
    {
        // Entry point for the application.
        public static void Main(string[] args)
        {
            ServiceRuntime.RegisterServiceAsync("ValuesServiceType", context => new WebHostingService(context, new ReliableStateManager(context))).GetAwaiter().GetResult();
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
