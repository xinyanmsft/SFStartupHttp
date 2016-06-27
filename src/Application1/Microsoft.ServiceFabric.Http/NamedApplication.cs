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

        public override string ToString() => this.ApplicationUri.ToString();
        
        public static implicit operator Uri(NamedApplication applicationName) => applicationName.ApplicationUri;

        public NamedService AppendNamedService(string serviceName) => new NamedService(this, serviceName);

        public static Boolean operator ==(NamedApplication app1, NamedApplication app2) => app1.Equals(app2);

        public static Boolean operator !=(NamedApplication app1, NamedApplication app2) => !app1.Equals(app2);

        public bool Equals(NamedApplication other) => UriEquals(m_uri, other.m_uri);

        public override bool Equals(object obj)
           => (obj is NamedApplication) ? UriEquals(m_uri, ((NamedApplication)obj).m_uri) : false;

        public override int GetHashCode() => this.ApplicationUri.GetHashCode();

        private Uri ApplicationUri
        {
            get
            {
                if (m_uri == null)
                {
                    m_uri = new Uri(FabricRuntime.GetActivationContext().ApplicationName);
                }
                return m_uri;
            }
        }

        private static bool UriEquals(Uri u1, Uri u2)
        {
            return u1 == null ? u2 == null : u1.Equals(u2);
        }
    }
}
