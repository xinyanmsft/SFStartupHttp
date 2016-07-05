using System;
using System.Fabric;

namespace Microsoft.ServiceFabric
{
    public struct NamedApplication : IEquatable<NamedApplication>
    {
        public const string InvalidCharacters = @"#$+./?[]";  // Make sure native code validate these URis always
        private Uri m_uri;

        public NamedApplication(string applicationName)
        {
            if (string.IsNullOrEmpty(applicationName) 
                || !applicationName.StartsWith("fabric:/", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(nameof(applicationName));
            }
            m_uri = new Uri(applicationName);
        }

        public NamedApplication(ServiceContext serviceContext)
        {
            if (serviceContext == null)
            {
                throw new ArgumentNullException(nameof(serviceContext));
            }
            m_uri = new Uri(serviceContext.CodePackageActivationContext.ApplicationName);
        }

        public override string ToString() => m_uri != null ? m_uri.ToString() : "null";
        
        public static implicit operator Uri(NamedApplication applicationName) => applicationName.m_uri;

        public NamedService AppendNamedService(string serviceName) => new NamedService(this, serviceName);

        public static Boolean operator ==(NamedApplication app1, NamedApplication app2) => app1.Equals(app2);

        public static Boolean operator !=(NamedApplication app1, NamedApplication app2) => !app1.Equals(app2);

        public bool Equals(NamedApplication other) => UriEquals(m_uri, other.m_uri);

        public override bool Equals(object obj)
           => (obj is NamedApplication) ? UriEquals(m_uri, ((NamedApplication)obj).m_uri) : false;

        public override int GetHashCode() => m_uri != null ? m_uri.GetHashCode() : 0;

        private static bool UriEquals(Uri u1, Uri u2)
        {
            return u1 == null ? u2 == null : u1.Equals(u2);
        }
    }
}
