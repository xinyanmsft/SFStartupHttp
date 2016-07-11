using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Application1.ValuesService
{
    public sealed class ValuesEntity
    {
        public ValuesEntity()
        {
        }

        public ValuesEntity(string id, DateTimeOffset createdOn, DateTimeOffset lastModifiedOn, DateTimeOffset lastAccessedOn, IEnumerable<string> values = null)
        {
            this.Id = id;
            this.CreatedOn = createdOn;
            this.LastModifiedOn = lastModifiedOn;
            this.LastAccessedOn = lastAccessedOn;
            this.Values = values ?? new string[] { };
        }

        public string Id { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset LastModifiedOn { get; set; }
        public DateTimeOffset LastAccessedOn { get; set; }
        public IEnumerable<string> Values { get; set; }
    }
}
