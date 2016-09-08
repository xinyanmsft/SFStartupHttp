using Microsoft.ServiceFabric.Services.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric
{
    public struct NamedEndpoint : IEquatable<NamedEndpoint>
    {
        private readonly NamedService _service;
        private readonly string _endpointName;
        private readonly ServiceTarget _target;
        private readonly ServicePartitionKey _partitionKey;

        public NamedEndpoint(NamedService service, string endpointName) 
            : this(service, endpointName, ServiceTarget.Any, null)
        {
        }

        public NamedEndpoint(NamedService service, string endpointName, ServiceTarget target, long partitionKey) 
            : this(service, endpointName, target, new ServicePartitionKey(partitionKey))
        {
        }

        public NamedEndpoint(NamedService service, string endpointName, ServiceTarget target, ServicePartitionKey partitionKey)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            _service = service;
            _endpointName = endpointName;
            _target = target;
            _partitionKey = partitionKey;
        }

        public NamedService Service => _service;

        public string EndpointName => _endpointName;

        public ServiceTarget Target => _target;

        public ServicePartitionKey PartitionKey => _partitionKey;

        public override string ToString()
        {
            return $"{_service} {_endpointName} {_target} {_partitionKey}";
        }

        public override int GetHashCode()
        {
            int h = _service.GetHashCode() ^ _target.GetHashCode();
            if (_endpointName != null)
            {
                h ^= _endpointName.GetHashCode();
            }
            if (_partitionKey != null)
            {
                h ^= _partitionKey.GetHashCode();
            }
            return h;
        }

        public override bool Equals(object obj)
        {
            return (obj is NamedEndpoint) ? this.Equals((NamedEndpoint)obj) : false;
        }

        public bool Equals(NamedEndpoint other)
        {
            return _service == other._service 
                && _endpointName == other._endpointName 
                && _target == other._target 
                && _partitionKey == other._partitionKey;
        }

        public static Boolean operator == (NamedEndpoint e1, NamedEndpoint e2) => e1.Equals(e2);

        public static Boolean operator !=(NamedEndpoint e1, NamedEndpoint e2) => !e1.Equals(e2);
    }
}
