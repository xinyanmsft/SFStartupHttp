using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Http.Utilities;
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
            long count = 0;
            var sessions = await this.GetSessionsDataAsync();
            await ExponentialBackoff.Run(async () =>
            {
                using (var tx = this.stateManager.CreateTransaction())
                {
                    count = await sessions.GetCountAsync(tx);
                }
            });
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
            await ExponentialBackoff.Run(async () =>
            {
                using (var tx = this.stateManager.CreateTransaction())
                {
                    var session = await sessions.TryGetValueAsync(tx, sessionId);
                    if (session.HasValue)
                    {
                        s = session.Value;
                        SessionData s2 = new SessionData(s.SessionId, s.CreatedOn, s.LastModifiedOn, DateTimeOffset.UtcNow, s.TodoItems);
                        await sessions.SetAsync(tx, sessionId, s2);
                    }
                    await tx.CommitAsync();
                }
            });
            return s;
        }

        private async Task<SessionData> CreateSessionAsync(string sessionId)
        {
            SessionData s = null;
            var sessions = await this.GetSessionsDataAsync();
            await ExponentialBackoff.Run(async () =>
            {
                s = new SessionData(sessionId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
                using (var tx = this.stateManager.CreateTransaction())
                {
                    await sessions.AddAsync(tx, sessionId, s);
                    await tx.CommitAsync();
                }
            });
            return s;
        }

        private async Task UpdateSessionAsync(string sessionId, SessionData data)
        {
            var sessions = await this.GetSessionsDataAsync();
            await ExponentialBackoff.Run(async () =>
            {
                using (var tx = this.stateManager.CreateTransaction())
                {
                    var session = await sessions.TryGetValueAsync(tx, sessionId);
                    SessionData s = session.HasValue ? new SessionData(sessionId, session.Value.CreatedOn, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, data.TodoItems)
                                                     : new SessionData(sessionId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTime.UtcNow, data.TodoItems);
                    await sessions.SetAsync(tx, sessionId, data);
                    await tx.CommitAsync();
                }
            });
        }

        private async Task DeleteSessionAsync(string sessionId)
        {
            var sessions = await this.GetSessionsDataAsync();
            await ExponentialBackoff.Run(async () =>
            {
                using (var tx = this.stateManager.CreateTransaction())
                {
                    await sessions.TryRemoveAsync(tx, sessionId);
                    await tx.CommitAsync();
                }
            });
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
