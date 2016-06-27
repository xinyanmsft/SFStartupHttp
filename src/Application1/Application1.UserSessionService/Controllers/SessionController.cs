using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Http;
using System;
using System.Fabric;
using System.Threading.Tasks;

namespace Application1.UserSessionService.Controllers
{
    [Route("api/[controller]")]
    public class SessionController : Controller
    {
        public SessionController(IReliableStateManager stateManager, ServiceContext serviceContext)
        {
            this.serviceContext = serviceContext;
            this.stateManager = stateManager;
        }

        [HttpGet]
        public async Task<long> Get()
        {
            long count;
            var sessions = await this.GetSessionsDataAsync();
            using (var tx = this.stateManager.CreateTransaction())
            {
                count = await sessions.GetCountAsync(tx);
            }
            return count;
        }

        [HttpGet("{sessionId}")]
        public async Task<IActionResult> Get(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return this.BadRequest();
            }

            SessionData s = await this.GetSessionAsync(sessionId);
            if (s == null)
            {
                return this.NotFound();
            }

            return new JsonResult(s);
        }
        
        [HttpPost("{sessionId}")]
        public async Task<IActionResult> Post(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return this.BadRequest();
            }

            SessionData s = await this.CreateSessionAsync(sessionId);
            return new JsonResult(s);
        }

        [HttpPut("{sessionId}")]
        public async Task<IActionResult> Put(string sessionId, [FromBody]SessionData value)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || value == null)
            {
                return this.BadRequest();
            }

            await this.UpdateSessionAsync(sessionId, value);
            return this.Ok();
        }

        [HttpDelete("{sessionId}")]
        public async Task<IActionResult> Delete(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return this.BadRequest();
            }

            await this.DeleteSessionAsync(sessionId);
            return this.Ok();
        }

        #region private members
        private async Task<SessionData> GetSessionAsync(string sessionId)
        {
            SessionData s = null;
            var sessions = await this.GetSessionsDataAsync();
            using (var tx = this.stateManager.CreateTransaction())
            {
                var session = await sessions.TryGetValueAsync(tx, sessionId);
                if (session.HasValue)
                {
                    s = session.Value;
                    s.LastAccessedOn = DateTimeOffset.UtcNow;
                    await sessions.SetAsync(tx, sessionId, s);
                }
                await tx.CommitAsync();
            }
            return s;
        }

        private async Task<SessionData> CreateSessionAsync(string sessionId)
        {
            SessionData s = new SessionData()
            {
                SessionId = sessionId,
                TodoItems = new TodoItemData[] { }
            };
            s.CreatedOn = s.LastAccessedOn = s.LastModifiedOn = DateTimeOffset.UtcNow;

            var sessions = await this.GetSessionsDataAsync();
            using (var tx = this.stateManager.CreateTransaction())
            {
                await sessions.AddAsync(tx, sessionId, s);
                await tx.CommitAsync();
            }

            return s;
        }

        private async Task UpdateSessionAsync(string sessionId, SessionData data)
        {
            var sessions = await this.GetSessionsDataAsync();
            using (var tx = this.stateManager.CreateTransaction())
            {
                var session = await sessions.TryGetValueAsync(tx, sessionId);
                if (session.HasValue)
                {
                    data.CreatedOn = session.Value.CreatedOn;
                    data.LastAccessedOn = data.LastModifiedOn = DateTimeOffset.UtcNow;
                }
                else
                {
                    data.CreatedOn = data.LastModifiedOn = data.LastAccessedOn = DateTimeOffset.UtcNow;
                }
                
                await sessions.SetAsync(tx, sessionId, data);
                await tx.CommitAsync();
            }
        }

        private async Task DeleteSessionAsync(string sessionId)
        {
            var sessions = await this.GetSessionsDataAsync();
            using (var tx = this.stateManager.CreateTransaction())
            {
                await sessions.TryRemoveAsync(tx, sessionId);
                await tx.CommitAsync();
            }
        }

        private Task<IReliableDictionary<string, SessionData>> GetSessionsDataAsync()
        {
            return this.stateManager.GetOrAddAsync<IReliableDictionary<string, SessionData>>("SessionData");
        }

        private readonly IReliableStateManager stateManager;
        private readonly ServiceContext serviceContext;
        #endregion
    }
}
