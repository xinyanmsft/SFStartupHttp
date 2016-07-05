using System;
using Microsoft.VisualStudio;
using Microsoft.ServiceFabric.Http.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ServiceFabric.Http.Test
{
    [TestClass]
    public class NamedServiceTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            NamedService service = new NamedService(new Uri("fabric:/testapp/myservice"));
            string s1 = service.BuildEndpointUri("endpoint", EndpointScheme.HTTP);
            Assert.AreEqual(s1, "http://fabric/testapp/myservice/#//Any/endpoint/");

            string s2 = service.BuildEndpointUri("endpoint", HttpServiceUriTarget.Primary, partitionKey: 123);
            Assert.AreEqual(s2, "http://fabric/testapp/myservice/#/123/Primary/endpoint/");
        }
        
        [TestMethod]
        public void TestMethod2()
        {
            NamedService service = new NamedService(new NamedApplication("fabric:/testapp"), "myservice");
            string s1 = service.BuildEndpointUri("endpoint", EndpointScheme.HTTPS);
            Assert.AreEqual(s1, "https://fabric/testapp/myservice/#//Any/endpoint/");

            string s2 = service.BuildEndpointUri("endpoint", HttpServiceUriTarget.Primary, partitionKey: 123, scheme: EndpointScheme.HTTPS);
            Assert.AreEqual(s2, "https://fabric/testapp/myservice/#/123/Primary/endpoint/");
        }
    }
}
