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
            string s1 = service.AppendNamedEndpoint("endpoint").BuildHttpUri();
            Assert.AreEqual(s1, "http://fabric/testapp/myservice/#//Any/endpoint/");

            string s2 = service.AppendNamedEndpoint("endpoint", ServiceTarget.Primary, partitionKey: 123).BuildHttpUri("default.html");
            Assert.AreEqual(s2, "http://fabric/testapp/myservice/#/123/Primary/endpoint/default.html");
        }
        
        [TestMethod]
        public void TestMethod2()
        {
            NamedService service = new NamedService(new NamedApplication("fabric:/testapp"), "myservice");
            string s1 = service.AppendNamedEndpoint("endpoint").BuildHttpUri(scheme: EndpointScheme.HTTPS);
            Assert.AreEqual(s1, "https://fabric/testapp/myservice/#//Any/endpoint/");

            string s2 = service.AppendNamedEndpoint("endpoint", ServiceTarget.Primary, partitionKey: 123).BuildHttpUri("foo/bar", scheme: EndpointScheme.HTTPS);
            Assert.AreEqual(s2, "https://fabric/testapp/myservice/#/123/Primary/endpoint/foo/bar");
        }
    }
}
