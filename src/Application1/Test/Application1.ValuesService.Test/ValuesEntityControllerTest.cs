using Application1.ValuesService;
using Application1.ValuesService.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Unittest.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Application1.ValuesService.Test
{
    [TestClass]
    public class ValuesEntityControllerTest
    {
        [TestInitialize]
        public void TestInitialize()
        {
            this.stateManager = new MockReliableStateManager();
            var serviceContext = MockServiceContextFactory.CreateStatefulServiceContext();

            this.controller = new ValuesController(this.stateManager, serviceContext);
            this.controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()                
            };
        }

        [TestMethod]
        public void PostAndGetTest()
        {
            string id = Guid.NewGuid().ToString();
            var result1 = this.controller.PostAsync(id).Result;
            JsonResult json1 = (JsonResult)result1;
            ValuesEntity data1 = (ValuesEntity)json1.Value;
            Assert.AreEqual(data1.Id, id);

            var result2 = this.controller.GetAsync(id).Result;
            JsonResult json2 = (JsonResult)result2;
            ValuesEntity data2 = (ValuesEntity)json2.Value;
            Assert.AreEqual(data1.Id, data2.Id);
            Assert.AreEqual(data1.CreatedOn, data2.CreatedOn);
        }
        
        private ValuesController controller;
        private MockReliableStateManager stateManager;
    }
}
