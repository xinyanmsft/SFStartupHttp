using System;
using System.Fabric;

namespace Microsoft.ServiceFabric
{
    public struct NamedService : IEquatable<NamedService>
    {
        public const string InvalidCharacters = @"#$+./?[]";
        private readonly Uri m_uri;

        public NamedService(string applicationName, string serviceName)
        {
            if (applicationName.StartsWith("fabric:/", StringComparison.OrdinalIgnoreCase))
            {
                m_uri = new Uri($"{applicationName}/{serviceName}");
            }
            else
            {
                m_uri = new Uri($"fabric:/{applicationName}/{serviceName}");
            }
        }

        public NamedService(NamedApplication applicationName, string serviceName)
        {
            m_uri = new Uri(applicationName.ToString() + "/" + serviceName);
        }

        public NamedService(Uri serviceName)
        {
            m_uri = serviceName;
        }

        public override string ToString() => m_uri.ToString();

        public static implicit operator Uri(NamedService serviceName)
           => serviceName.m_uri;

        public static Boolean operator ==(NamedService s1, NamedService s2)
           => s1.Equals(s2);
        public static Boolean operator !=(NamedService s1, NamedService s2)
           => !s1.Equals(s2);

        public bool Equals(NamedService other)
                    => m_uri.Equals(other.m_uri);

        public override bool Equals(object obj)
           => (obj is NamedService) ? m_uri.Equals(((NamedService)obj).m_uri) : false;

        public override int GetHashCode() => m_uri.GetHashCode();
    }
}
