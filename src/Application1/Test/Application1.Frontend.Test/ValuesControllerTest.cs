using Application1.Frontend.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Unittest.HttpClient;
using Microsoft.ServiceFabric.Unittest.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Application1.Frontend.Test
{
    [TestClass]
    public class ValuesControllerTest
    {
        [TestInitialize]
        public void TestInitialize()
        {
            this.mockHandler = new MockHttpClientHandler();
            HttpClient testHttpClient = new HttpClient(this.mockHandler);
            var serviceContext = MockServiceContextFactory.CreateStatelessServiceContext();

            this.controller = new ValuesController(testHttpClient, serviceContext);
            this.controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()                
            };
        }

        [TestMethod]
        public void GetAsyncTest()
        {
            this.mockHandler.Handler = (request) => new HttpResponseMessage(HttpStatusCode.OK)
            { 
               Content = new StringContent("Test content")
            };
            var result = (ContentResult) controller.GetAsync("testId").Result;
            Assert.AreEqual(result.StatusCode, 200);
            Assert.AreEqual(result.Content, "Test content");
        }

        [TestMethod]
        public void PostAsyncTest()
        {
            this.mockHandler.Handler = (request) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("post result")
            };
            controller.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("some content"));
            var result = (ContentResult)controller.PostAsync().Result;
            Assert.AreEqual(result.StatusCode, 200);
        }

        private ValuesController controller;
        private MockHttpClientHandler mockHandler;
    }
}
