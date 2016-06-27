using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Threading;

namespace Application1.UserSessionService
{
    public class Program
    {
        // Entry point for the application.
        public static void Main(string[] args)
        {
            ServiceRuntime.RegisterServiceAsync("UserSessionServiceType", context => new WebHostingService(context, new ReliableStateManager(context))).GetAwaiter().GetResult();
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
