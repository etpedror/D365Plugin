using BasePlugin;
using System;

namespace ServiceBusDemos
{
    public class SendToServiceBus : PluginBase
    {
        public SendToServiceBus(string unsecureConfiguration, string secureConfiguration):base(unsecureConfiguration, secureConfiguration)
        {
        }

        public override void Execute()
        {
            var ServiceBusEndpointId = GetSecureConfigurationDataString("ServiceBusEndpointId");
            SendContextToEndpoint(new Guid(ServiceBusEndpointId));
        }
    }
}
