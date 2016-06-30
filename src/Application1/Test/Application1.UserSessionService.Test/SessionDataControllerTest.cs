using Application1.UserSessionService.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Unittest.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Application1.UserSessionService.Test
{
    [TestClass]
    public class SessionControllerTest
    {
        [TestInitialize]
        public void TestInitialize()
        {
            this.stateManager = new MockReliableStateManager();
            var serviceContext = MockServiceContextFactory.CreateStatefulServiceContext();

            this.controller = new SessionDataController(this.stateManager, serviceContext);
            this.controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()                
            };
        }

        [TestMethod]
        public void PostAndGetTest()
        {
            string sessionId = Guid.NewGuid().ToString();
            var result1 = this.controller.PostAsync(sessionId).Result;
            JsonResult json1 = (JsonResult)result1;
            SessionData data1 = (SessionData)json1.Value;
            Assert.AreEqual(data1.SessionId, sessionId);

            var result2 = this.controller.GetAsync(sessionId).Result;
            JsonResult json2 = (JsonResult)result2;
            SessionData data2 = (SessionData)json2.Value;
            Assert.AreEqual(data1.SessionId, data2.SessionId);
            Assert.AreEqual(data1.CreatedOn, data2.CreatedOn);
        }
        
        private SessionDataController controller;
        private MockReliableStateManager stateManager;
    }
}
