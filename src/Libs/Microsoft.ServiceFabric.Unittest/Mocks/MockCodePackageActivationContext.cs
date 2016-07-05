// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Unittest.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Fabric.Health;
    using System.IO;

    public class MockCodePackageActivationContext : ICodePackageActivationContext
    {
        public MockCodePackageActivationContext() : this("testApp", "testService", "1.0.0", "code", "1.0.0")
        {
        }

        public MockCodePackageActivationContext(string applicationName, string serviceManifestName, string serviceManifestVersion, string codePackageName, string codePackageVersion)
        {
            this.ApplicationName = applicationName;
            this.ApplicationTypeName = applicationName + "Type";
            this.serviceManifestName = serviceManifestName;
            this.serviceManifestVersion = serviceManifestVersion;
            this.CodePackageName = codePackageName;
            this.CodePackageVersion = codePackageVersion;
            
            this.ContextId = Guid.NewGuid().ToString();
            this.LogDirectory = Path.GetTempPath();
            this.TempDirectory = Path.GetTempPath();
            this.WorkDirectory = Path.GetTempPath();

            this.configPackages = new Dictionary<string, ConfigurationPackage>();
        }

        public void AddConfigurationPackage(string package, ConfigurationPackage config)
        {
            this.configPackages[package] = config;
        }

        public string ApplicationName { get; set; }

        public string ApplicationTypeName { get; set; }

        public string CodePackageName { get; set; }

        public string CodePackageVersion { get; set; }

        public string ContextId { get; set; }

        public string LogDirectory { get; set; }

        public string TempDirectory { get; set; }

        public string WorkDirectory { get; set; }

        public event EventHandler<PackageAddedEventArgs<CodePackage>> CodePackageAddedEvent;

        public event EventHandler<PackageModifiedEventArgs<CodePackage>> CodePackageModifiedEvent;

        public event EventHandler<PackageRemovedEventArgs<CodePackage>> CodePackageRemovedEvent;

        public event EventHandler<PackageAddedEventArgs<ConfigurationPackage>> ConfigurationPackageAddedEvent;

        public event EventHandler<PackageModifiedEventArgs<ConfigurationPackage>> ConfigurationPackageModifiedEvent;

        public event EventHandler<PackageRemovedEventArgs<ConfigurationPackage>> ConfigurationPackageRemovedEvent;

        public event EventHandler<PackageAddedEventArgs<DataPackage>> DataPackageAddedEvent;

        public event EventHandler<PackageModifiedEventArgs<DataPackage>> DataPackageModifiedEvent;

        public event EventHandler<PackageRemovedEventArgs<DataPackage>> DataPackageRemovedEvent;

        public void Dispose()
        {
        }

        public ApplicationPrincipalsDescription GetApplicationPrincipals()
        {
            return new ApplicationPrincipalsDescription();
        }

        public IList<string> GetCodePackageNames()
        {
            var result = new List<string>();
            result.Add(this.CodePackageName);
            return result;
        }

        public CodePackage GetCodePackageObject(string packageName)
        {
            // TODO: There is no way to mock a CodePackage since it's can't be constructed outside of real runtime
            throw new NotImplementedException();
        }

        public IList<string> GetConfigurationPackageNames()
        {
            return new List<string>(this.configPackages.Keys);
        }

        public ConfigurationPackage GetConfigurationPackageObject(string packageName)
        {
            return this.configPackages[packageName];
        }

        public IList<string> GetDataPackageNames()
        {
            // TODO: There is no way to mock a DataPackage 
            throw new NotImplementedException();
        }

        public DataPackage GetDataPackageObject(string packageName)
        {
            // TODO: There is no way to mock a DataPackage 
            throw new NotImplementedException();
        }

        public EndpointResourceDescription GetEndpoint(string endpointName)
        {
            throw new NotImplementedException();
        }

        public KeyedCollection<string, EndpointResourceDescription> GetEndpoints()
        {
            throw new NotImplementedException();
        }

        // TODO: Should we support ServiceGroup?
        public KeyedCollection<string, ServiceGroupTypeDescription> GetServiceGroupTypes()
        {
            throw new NotImplementedException();
        }

        public string GetServiceManifestName()
        {
            return this.serviceManifestVersion;
        }

        public string GetServiceManifestVersion()
        {
            return this.serviceManifestVersion;
        }

        // TODO: Should we support ServiceTypes?
        public KeyedCollection<string, ServiceTypeDescription> GetServiceTypes()
        {
            throw new NotImplementedException();
        }

        public void ReportApplicationHealth(HealthInformation healthInfo)
        {
            this.ApplicationHealthReported?.Invoke(this, healthInfo);
        }

        public void ReportDeployedApplicationHealth(HealthInformation healthInfo)
        {
            this.DeployedApplicationHealthReported?.Invoke(this, healthInfo);
        }

        public void ReportDeployedServicePackageHealth(HealthInformation healthInfo)
        {
            this.DeployedServicePackageHealthReported?.Invoke(this, healthInfo);
        }

        public event EventHandler<HealthInformation> ApplicationHealthReported;
        public event EventHandler<HealthInformation> DeployedApplicationHealthReported;
        public event EventHandler<HealthInformation> DeployedServicePackageHealthReported;

        private string serviceManifestName;
        private string serviceManifestVersion;
        private Dictionary<string, ConfigurationPackage> configPackages;
    }
}