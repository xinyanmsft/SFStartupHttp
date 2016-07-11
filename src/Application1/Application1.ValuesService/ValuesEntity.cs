using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Application1.ValuesService
{
    /// <summary>
    /// Data types stored in Service Fabric reliable collection should be immutable, as explained in this article
    /// https://azure.microsoft.com/en-us/documentation/articles/service-fabric-work-with-reliable-collections/
    /// 
    /// </summary>
    [DataContract]
    public sealed class ValuesEntity
    {
        public ValuesEntity(string id, DateTimeOffset createdOn, DateTimeOffset lastModifiedOn, DateTimeOffset lastAccessedOn, IEnumerable<string> values = null)
        {
            this.Id = id;
            this.CreatedOn = createdOn;
            this.LastModifiedOn = lastModifiedOn;
            this.LastAccessedOn = lastAccessedOn;
            this.Values = values == null ? ImmutableList<string>.Empty : values.ToImmutableList();
        }

        [DataMember]
        public readonly string Id;

        [DataMember]
        public readonly DateTimeOffset CreatedOn;

        [DataMember]
        public readonly DateTimeOffset LastModifiedOn;

        [DataMember]
        public readonly DateTimeOffset LastAccessedOn;

        [DataMember]
        public IEnumerable<string> Values { get; private set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Convert the deserialized collection to an immutable collection
            this.Values = this.Values == null ? ImmutableList<string>.Empty : this.Values.ToImmutableList();
        }
    }
}
