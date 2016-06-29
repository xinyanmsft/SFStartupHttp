using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Application1.UserSessionService
{
    /// <summary>
    /// Data types stored in Service Fabric reliable collection should be immutable, as explained in this article
    /// https://azure.microsoft.com/en-us/documentation/articles/service-fabric-work-with-reliable-collections/
    /// 
    /// </summary>
    [DataContract]
    public sealed class SessionData
    {
        public SessionData(string id, DateTimeOffset createdOn, DateTimeOffset lastModifiedOn, DateTimeOffset lastAccessedOn, IEnumerable<TodoItemData> items = null)
        {
            this.SessionId = id;
            this.CreatedOn = createdOn;
            this.LastModifiedOn = lastModifiedOn;
            this.LastAccessedOn = lastAccessedOn;
            this.TodoItems = items == null ? ImmutableList<TodoItemData>.Empty : items.ToImmutableList();
        }

        [DataMember]
        public readonly string SessionId;

        [DataMember]
        public readonly DateTimeOffset CreatedOn;

        [DataMember]
        public readonly DateTimeOffset LastModifiedOn;

        [DataMember]
        public readonly DateTimeOffset LastAccessedOn;

        [DataMember]
        public IEnumerable<TodoItemData> TodoItems { get; private set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Convert the deserialized collection to an immutable collection
            this.TodoItems = this.TodoItems == null ? ImmutableList<TodoItemData>.Empty : TodoItems.ToImmutableList();
        }
    }

    [DataContract]
    public sealed class TodoItemData
    {
        public TodoItemData(string id, string content)
        {
            this.Id = id;
            this.Content = content;
        }

        [DataMember]
        public readonly string Id;

        [DataMember]
        public readonly string Content;
    }
}
