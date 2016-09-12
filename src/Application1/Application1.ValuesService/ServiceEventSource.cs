using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Http;
using Microsoft.ServiceFabric;

namespace Application1.ValuesService
{
    [EventSource(Name = "MyCompany-Application1-ValuesService")]
    internal sealed class ServiceEventSource : EventSource
    {
        public static readonly ServiceEventSource Current = new ServiceEventSource();

        static ServiceEventSource()
        {
            // A workaround for the problem where ETW activities do not get tracked until Tasks infrastructure is initialized.
            // This problem will be fixed in .NET Framework 4.6.2.
            Task.Run(() => { });
        }

        // Instance constructor is private to enforce singleton semantics
        private ServiceEventSource() : base() { }

        #region Keywords
        // Event keywords can be used to categorize events. 
        // Each keyword is a bit flag. A single event can be associated with multiple keywords (via EventAttribute.Keywords property).
        // Keywords must be defined as a public class named 'Keywords' inside EventSource that uses them.
        public static class Keywords
        {
            public const EventKeywords Requests = (EventKeywords)0x1L;
            public const EventKeywords ServiceInitialization = (EventKeywords)0x2L;
        }
        #endregion

        #region Events
        // Define an instance method for each event you want to record and apply an [Event] attribute to it.
        // The method name is the name of the event.
        // Pass any parameters you want to record with the event (only primitive integer types, DateTime, Guid & string are allowed).
        // Each event method implementation should check whether the event source is enabled, and if it is, call WriteEvent() method to raise the event.
        // The number and types of arguments passed to every event method must exactly match what is passed to WriteEvent().
        // Put [NonEvent] attribute on all methods that do not define an event.
        // For more information see https://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource.aspx

        [NonEvent]
        public void Message(string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                string finalMessage = string.Format(message, args);
                Message(finalMessage);
            }
        }

        private const int MessageEventId = 1;
        [Event(MessageEventId, Level = EventLevel.Informational, Message = "{0}")]
        public void Message(string message)
        {
            if (this.IsEnabled())
            {
                WriteEvent(MessageEventId, message);
            }
        }

        [NonEvent]
        public void ServiceMessage(ServiceContext serviceContext, string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                string finalMessage = string.Format(message, args);
                ServiceMessage(
                    serviceContext.ServiceName.ToString(),
                    serviceContext.ServiceTypeName,
                    GetReplicaOrInstanceId(serviceContext),
                    serviceContext.PartitionId,
                    serviceContext.CodePackageActivationContext.ApplicationName,
                    serviceContext.CodePackageActivationContext.ApplicationTypeName,
                    serviceContext.NodeContext.NodeName,
                    ServiceFabricDiagnostics.GetRequestCorrelationId() ?? string.Empty,
                    finalMessage);
            }
        }

        // For very high-frequency events it might be advantageous to raise events using WriteEventCore API.
        // This results in more efficient parameter handling, but requires explicit allocation of EventData structure and unsafe code.
        // To enable this code path, define UNSAFE conditional compilation symbol and turn on unsafe code support in project properties.
        private const int ServiceMessageEventId = 2;
        [Event(ServiceMessageEventId, Level = EventLevel.Informational, Message = "{7}")]
        private
#if UNSAFE
        unsafe
#endif
        void ServiceMessage(
            string serviceName,
            string serviceTypeName,
            long replicaOrInstanceId,
            Guid partitionId,
            string applicationName,
            string applicationTypeName,
            string nodeName,
            string correlationId,
            string message)
        {
#if !UNSAFE
            WriteEvent(ServiceMessageEventId, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, nodeName, correlationId, message);
#else
            const int numArgs = 9;
            fixed (char* pServiceName = serviceName, pServiceTypeName = serviceTypeName, pApplicationName = applicationName, pApplicationTypeName = applicationTypeName, pNodeName = nodeName, pCorrelationId = correlationId, pMessage = message)
            {
                EventData* eventData = stackalloc EventData[numArgs];
                eventData[0] = new EventData { DataPointer = (IntPtr) pServiceName, Size = SizeInBytes(serviceName) };
                eventData[1] = new EventData { DataPointer = (IntPtr) pServiceTypeName, Size = SizeInBytes(serviceTypeName) };
                eventData[2] = new EventData { DataPointer = (IntPtr) (&replicaOrInstanceId), Size = sizeof(long) };
                eventData[3] = new EventData { DataPointer = (IntPtr) (&partitionId), Size = sizeof(Guid) };
                eventData[4] = new EventData { DataPointer = (IntPtr) pApplicationName, Size = SizeInBytes(applicationName) };
                eventData[5] = new EventData { DataPointer = (IntPtr) pApplicationTypeName, Size = SizeInBytes(applicationTypeName) };
                eventData[6] = new EventData { DataPointer = (IntPtr) pNodeName, Size = SizeInBytes(nodeName) };
                eventData[7] = new EventData { DataPointer = (IntPtr) pMessage, Size = SizeInBytes(message) };
                eventData[8] = new EventData { DataPointer = (IntPtr) pCorrelationId, Size = SizeInBytes(correlationId) };
                WriteEventCore(ServiceMessageEventId, numArgs, eventData);
            }
#endif
        }

        private const int ServiceTypeRegisteredEventId = 3;
        [Event(ServiceTypeRegisteredEventId, Level = EventLevel.Informational, Message = "Service host process {0} registered service type {1}", Keywords = Keywords.ServiceInitialization)]
        public void ServiceTypeRegistered(int hostProcessId, string serviceType)
        {
            WriteEvent(ServiceTypeRegisteredEventId, hostProcessId, serviceType);
        }

        private const int ServiceHostInitializationFailedEventId = 4;
        [Event(ServiceHostInitializationFailedEventId, Level = EventLevel.Error, Message = "Service host initialization failed", Keywords = Keywords.ServiceInitialization)]
        public void ServiceHostInitializationFailed(string exception)
        {
            WriteEvent(ServiceHostInitializationFailedEventId, exception);
        }

        // A pair of events sharing the same name prefix with a "Start"/"Stop" suffix implicitly marks boundaries of an event tracing activity.
        // These activities can be automatically picked up by debugging and profiling tools, which can compute their execution time, child activities,
        // and other statistics.
        private const int ServiceRequestStartEventId = 5;

        [NonEvent]
        public void ServiceRequestStart(ServiceContext serviceContext, string method, string requestTypeName, string correlationId = null, string origin = null)
        {
            string serviceName = serviceContext != null ? serviceContext.ServiceName.ToString() : null;
            string serviceTypeName = serviceContext != null ? serviceContext.ServiceTypeName : null;
            long replicaOrInstanceId = serviceContext != null ? GetReplicaOrInstanceId(serviceContext) : 0;
            Guid partitionId = serviceContext != null ? serviceContext.PartitionId : Guid.Empty;
            string applicationName = serviceContext != null ? serviceContext.CodePackageActivationContext.ApplicationName : null;
            string applicationTypeName = serviceContext != null ? serviceContext.CodePackageActivationContext.ApplicationTypeName : null;
            if (correlationId == null)
            {
                correlationId = ServiceFabricDiagnostics.GetRequestCorrelationId() ?? string.Empty;
            }
            if (origin == null)
            {
                origin = ServiceFabricDiagnostics.GetRequestOrigin() ?? string.Empty;
            }
            ServiceRequestStart(requestTypeName, method, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, correlationId, origin);
        }

        [Event(ServiceRequestStartEventId, Level = EventLevel.Informational, Message = "Service request '{0}' started", Keywords = Keywords.Requests)]
        public void ServiceRequestStart(string requestTypeName, string method, string serviceName, string serviceTypeName, long replicaOrInstanceId, Guid partitionId, string applicationName, string applicationTypeName, string correlationId, string origin)
        {
            WriteEvent(ServiceRequestStartEventId, requestTypeName, method, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, correlationId, origin);
        }

        private const int ServiceRequestStopEventId = 6;
        
        [NonEvent]
        public void ServiceRequestStop(ServiceContext serviceContext, string method, string requestTypeName, int statusCode, string correlationId = null, string origin = null)
        {
            string serviceName = serviceContext != null ? serviceContext.ServiceName.ToString() : null;
            string serviceTypeName = serviceContext != null ? serviceContext.ServiceTypeName : null;
            long replicaOrInstanceId = serviceContext != null ? GetReplicaOrInstanceId(serviceContext) : 0;
            Guid partitionId = serviceContext != null ? serviceContext.PartitionId : Guid.Empty;
            string applicationName = serviceContext != null ? serviceContext.CodePackageActivationContext.ApplicationName : null;
            string applicationTypeName = serviceContext != null ? serviceContext.CodePackageActivationContext.ApplicationTypeName : null;
            if (correlationId == null)
            {
                correlationId = ServiceFabricDiagnostics.GetRequestCorrelationId() ?? string.Empty;
            }
            if (origin == null)
            {
                origin = ServiceFabricDiagnostics.GetRequestOrigin() ?? string.Empty;
            }
            ServiceRequestStop(requestTypeName, method, statusCode, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, correlationId, origin);
        }

        [Event(ServiceRequestStopEventId, Level = EventLevel.Informational, Message = "Service request '{0}' finished", Keywords = Keywords.Requests)]
        public void ServiceRequestStop(string requestTypeName, string method, int statusCode, string serviceName, string serviceTypeName, long replicaOrInstanceId, Guid partitionId, string applicationName, string applicationTypeName, string correlationId, string origin)
        {
            WriteEvent(ServiceRequestStopEventId, requestTypeName, method, statusCode, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, correlationId, origin);
        }

        private const int ServiceRequestFailedEventId = 7;
        [NonEvent]
        public void ServiceRequestFailed(ServiceContext serviceContext, string method, string requestTypeName, string exception, string correlationId = null, string origin = null)
        {
            string serviceName = serviceContext != null ? serviceContext.ServiceName.ToString() : null;
            string serviceTypeName = serviceContext != null ? serviceContext.ServiceTypeName : null;
            long replicaOrInstanceId = serviceContext != null ? GetReplicaOrInstanceId(serviceContext) : 0;
            Guid partitionId = serviceContext != null ? serviceContext.PartitionId : Guid.Empty;
            string applicationName = serviceContext != null ? serviceContext.CodePackageActivationContext.ApplicationName : null;
            string applicationTypeName = serviceContext != null ? serviceContext.CodePackageActivationContext.ApplicationTypeName : null;
            if (correlationId == null)
            {
                correlationId = ServiceFabricDiagnostics.GetRequestCorrelationId() ?? string.Empty;
            }
            if (origin == null)
            {
                origin = ServiceFabricDiagnostics.GetRequestOrigin() ?? string.Empty;
            }
            ServiceRequestFailed(requestTypeName, method, exception, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, correlationId, origin);
        }

        [Event(ServiceRequestFailedEventId, Level = EventLevel.Error, Message = "Service request '{0}' failed", Keywords = Keywords.Requests)]
        public void ServiceRequestFailed(string requestTypeName, string method, string exception, string serviceName, string serviceTypeName, long replicaOrInstanceId, Guid partitionId, string applicationName, string applicationTypeName, string correlationId, string origin)
        {
            WriteEvent(ServiceRequestFailedEventId, requestTypeName, method, exception, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, correlationId, origin);
        }
        #endregion

        #region Private methods
        private static long GetReplicaOrInstanceId(ServiceContext context)
        {
            StatelessServiceContext stateless = context as StatelessServiceContext;
            if (stateless != null)
            {
                return stateless.InstanceId;
            }

            StatefulServiceContext stateful = context as StatefulServiceContext;
            if (stateful != null)
            {
                return stateful.ReplicaId;
            }

            throw new NotSupportedException("Context type not supported.");
        }
#if UNSAFE
        private int SizeInBytes(string s)
        {
            if (s == null)
            {
                return 0;
            }
            else
            {
                return (s.Length + 1) * sizeof(char);
            }
        }
#endif
        #endregion
    }
}
