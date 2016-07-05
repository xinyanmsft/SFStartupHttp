using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Fabric;
using System.Fabric.Description;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Unittest.Mocks
{
    public static class MockConfigurationPackage
    {
        public static void SetConfigurationValue(ServiceContext context, string package, string section, string name, string value)
        {
            MockCodePackageActivationContext mockContext = context.CodePackageActivationContext as MockCodePackageActivationContext;
            if (mockContext == null)
            {
                throw new ArgumentException("MockCodePackageActivationContext expected");
            }

            ConfigurationPackage config;
            if (!mockContext.GetConfigurationPackageNames().Contains(package))
            {
                config = CreateConfigurationPackage();
                mockContext.AddConfigurationPackage(package, config);
            }
            else
            {
                config = mockContext.GetConfigurationPackageObject(package);
            }
            
            System.Fabric.Description.ConfigurationSettings settings = config.Settings;
            if (!settings.Sections.Contains(section))
            {
                ConfigurationSection newSection = (ConfigurationSection)Activator.CreateInstance(typeof(ConfigurationSection), nonPublic: true);
                typeof(ConfigurationSection).GetProperty("Name").SetValue(newSection, section);
                settings.Sections.Add(newSection);
            }

            var s = settings.Sections[section];

            ConfigurationProperty p = (ConfigurationProperty) Activator.CreateInstance(typeof(ConfigurationProperty), nonPublic: true);
            typeof(ConfigurationProperty).GetProperty("Name").SetValue(p, name);
            typeof(ConfigurationProperty).GetProperty("Value").SetValue(p, value);

            s.Parameters.Add(p);
        }

        internal static ConfigurationPackage CreateConfigurationPackage(string path = null)
        {
            // new ConfigurationPackage
            ConfigurationPackage config = (ConfigurationPackage)Activator.CreateInstance(typeof(ConfigurationPackage), nonPublic: true);
            if (!string.IsNullOrEmpty(path))
            {
                // ConfigurationPackage.Path = path
                PropertyInfo pathProp = typeof(ConfigurationPackage).GetProperty("Path");
                pathProp.SetValue(config, path, BindingFlags.NonPublic, null, null, CultureInfo.InvariantCulture);
            }

            // settings = new ();
            System.Fabric.Description.ConfigurationSettings settings = (System.Fabric.Description.ConfigurationSettings)Activator.CreateInstance(typeof(System.Fabric.Description.ConfigurationSettings), nonPublic: true);

            // config.Settings = settings;
            typeof(ConfigurationPackage).GetProperty("Settings").SetValue(config, settings);

            return config;
        }
    }
}
