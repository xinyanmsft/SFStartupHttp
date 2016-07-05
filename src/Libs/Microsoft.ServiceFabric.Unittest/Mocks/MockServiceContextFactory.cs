using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Unittest.Mocks
{
    public static class MockServiceContextFactory
    {
        public static string ApplicationName { get; set; }
        public static string ServiceName { get; set; }
        public static string ServiceVersion { get; set; }
        public static string CodePackageName { get; set; }
        public static string CodePackageVersion { get; set; }

        static MockServiceContextFactory()
        {
            ApplicationName = "fabric:/testApp";
            ServiceName = "testService";
            ServiceVersion = "1.0.0";
            CodePackageName = "code";
            CodePackageVersion = "1.0.0";
        }

        public static StatefulServiceContext CreateStatefulServiceContext()
        {
            return new StatefulServiceContext(
                new NodeContext("node0", new NodeId(0, 0), 0, "nodeType", "localhost"),
                new MockCodePackageActivationContext(ApplicationName, ServiceName, ServiceVersion, CodePackageName, CodePackageVersion),
                ServiceName,
                new Uri("fabric:/" + ServiceName),
                null,
                Guid.NewGuid(),
                0);
        }

        public static StatelessServiceContext CreateStatelessServiceContext()
        {
            return new StatelessServiceContext(
                new NodeContext("node0", new NodeId(0, 0), 0, "nodeType", "localhost"),
                new MockCodePackageActivationContext(ApplicationName, ServiceName, ServiceVersion, CodePackageName, CodePackageVersion),
                ServiceName,
                new Uri("fabric:/" + ServiceName),
                null,
                Guid.NewGuid(),
                0);
        }
    }
}
