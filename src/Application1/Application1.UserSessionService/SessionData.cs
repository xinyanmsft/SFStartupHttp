using System;

namespace Application1.UserSessionService
{
    public class SessionData
    {
        public string SessionId { get; set; }

        public TodoItemData[] TodoItems { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LastModifiedOn { get; set; }

        public DateTimeOffset LastAccessedOn { get; set; }
    }

    public class TodoItemData
    {
        public string Id { get; set; }

        public string Content { get; set; }
    }
}
