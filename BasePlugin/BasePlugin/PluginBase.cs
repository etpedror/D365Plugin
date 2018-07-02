using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace BasePlugin
{
    /// <summary>
    /// A base class to be inherited when implementing a CRM Plugin
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        #region Helpers
        #region Plugin Execution Runtime Providers
        /// <summary> 
        /// The Service Provider 
        /// </summary>
        protected IServiceProvider ServiceProvider
        {
            get
            {
                return _serviceProvider;
            }
        }
        private IServiceProvider _serviceProvider = null;

        /// <summary> 
        /// A reference to the Tracing Service, to log messages on the CRM 
        /// </summary>
        protected ITracingService TracingService
        {
            get
            {
                if (_tracingService == null)
                {
                    if (_serviceProvider == null)
                    {
                        return null;
                    }
                    _tracingService = (ITracingService)_serviceProvider.GetService(typeof(ITracingService));
                }
                return _tracingService;
            }
        }
        private ITracingService _tracingService = null;
        
        /// <summary> 
        /// A reference to the Service Endpoint Service, to send messages to web endpoints
        /// </summary>
        protected IServiceEndpointNotificationService CloudService
        {
            get
            {
                if (_cloudService == null)
                {
                    if(_serviceProvider == null)
                    {
                        return null;
                    }
                    _cloudService = (IServiceEndpointNotificationService)_serviceProvider.GetService(typeof(IServiceEndpointNotificationService));
                }
                return _cloudService;
            }
        }
        private IServiceEndpointNotificationService _cloudService = null;
        
        /// <summary> 
        /// A reference to the Execution Context 
        /// </summary>
        protected IPluginExecutionContext Context
        {
            get
            {
                if (_context == null)
                {
                    if (_serviceProvider == null)
                    {
                        return null;
                    }
                    _context = (IPluginExecutionContext)_serviceProvider.GetService(typeof(IPluginExecutionContext));
                }
                return _context;
            }
        }
        private IPluginExecutionContext _context = null;

        /// <summary>
        /// A reference to the organization service
        /// </summary>
        protected IOrganizationService OrganizationService
        {
            get
            {
                if(_organizationService == null)
                {
                    if(ServiceProvider != null)
                    {
                        _organizationService = (IOrganizationService)ServiceProvider;
                    }
                }
                return _organizationService;
            }
        }
        private IOrganizationService _organizationService = null;
        #endregion

        #region Functions
        /// <summary>
        /// Sends the Execution Context to an Endpoint 
        /// </summary>
        /// <param name="EndpointId">The Id of the Service Endpoint (check the plugin registration tool to get the endpoint id)</param>
        /// <returns>a string with the operation result</returns>
        protected string SendContextToEndpoint(Guid EndpointId)
        {
            return CloudService.Execute(new EntityReference("serviceendpoint", EndpointId), Context);
        }

        /// <summary>
        /// Logs a message to the trace log
        /// </summary>
        /// <param name="message">The message to log</param>
        protected void Trace(string message)
        {
            if(TracingService != null)
            {
                TracingService.Trace(message);
            }
        }

        /// <summary>
        /// Executes a workflow on an record
        /// </summary>
        /// <param name="workflowId">The id of the workflow to execute</param>
        /// <param name="entityId">The id of the entity over which to execute to workflow</param>
        /// <returns>The response from the ExecuteWorkflow request</returns>
        protected ExecuteWorkflowResponse ExecuteWorkflowOnEntity(Guid workflowId, Guid entityId)
        {
            ExecuteWorkflowRequest request = new ExecuteWorkflowRequest()
            {
                WorkflowId = workflowId,
                EntityId = entityId
            };
            return(ExecuteWorkflowResponse)(OrganizationService.Execute(request));

        }

        /// <summary>
        /// Sends an email
        /// </summary>
        /// <param name="regarding">What the email regards</param>
        /// <param name="to">The recipient of the email</param>
        /// <param name="subject">The subject of the email</param>
        /// <param name="description">The body of the email (HTML supported)</param>
        /// <returns>The response of the SendEmail Request</returns>
        protected SendEmailResponse SendEmail(EntityReference regarding, EntityReference to, string subject, string description)
        {
            Entity email = new Entity("email");
            Entity fromParty = new Entity("activityparty");
            Entity toParty = new Entity("activityparty");

            toParty["partyid"] = new EntityReference("systemuser", to.Id);
            fromParty["partyid"] = new EntityReference("systemuser", Context.UserId);
            email["from"] = new Entity[] { fromParty };
            email["to"] = new Entity[] { toParty };

            email["subject"] = subject;
            email["description"] = description;
            email["regardingobjectid"] = regarding;

            Guid emailId = OrganizationService.Create(email);
            SendEmailRequest sendEmailreq = new SendEmailRequest
            {
                EmailId = emailId,
                TrackingToken = "",
                IssueSend = true
            };
            return (SendEmailResponse)OrganizationService.Execute(sendEmailreq);
        }
        #endregion

        #region Configurations
        /// <summary>
        /// Returns the content of the unsecure configuration for the step
        /// </summary>
        protected string UnsecureConfiguration
        {
            get
            {
                return _unsecureConfiguration;
            }
        }
        private string _unsecureConfiguration;

        /// <summary>
        /// Returns the content of the secure configuration for the step
        /// </summary>
        protected string SecureConfiguration
        {
            get
            {
                return _secureConfiguration;
            }
        }
        private string _secureConfiguration;
        
        /// <summary>
        /// Returns the value of an unsecure configuration item, if the UnsecureConfiguration is a valid xml
        /// </summary>
        /// <param name="label">the label of the parameter</param>
        /// <returns>The value of the parameter</returns>
        protected string GetUnsecureConfigurationDataString(string label)
        {
            return GetValueNode(UnsecureXml, label);
        }

        /// <summary>
        /// Returns the value of a secure configuration item, if the SecureConfiguration is a valid xml
        /// </summary>
        /// <param name="label">the label of the parameter</param>
        /// <returns>The value of the parameter</returns>
        protected string GetSecureConfigurationDataString(string label)
        {
            return GetValueNode(SecureXml, label);
        }

        /// <summary>
        /// Gets the UnsecureConfiguration as a XmlDocument
        /// The XML should be in the format
        /// <para />&lt;?xml version="1.0" encoding="utf-8"&gt;
        /// <para />&lt;Settings&gt;
        /// <para />  &lt;setting name = "ReportExc_ReportExecutionService" &gt;
        /// <para />    &lt; value &gt; http://localhost/ReportServer/ReportExecution2005.asmx&lt;/value&gt;
        /// <para />  &lt;/setting&gt;
        /// <para />  &lt;setting name = "Unique_Organization" &gt;
        /// <para />    &lt; value &gt; Contoso &lt;/ value &gt;
        /// <para />  &lt;/ setting &gt;
        /// <para />&lt;/ Settings &gt;
        /// </summary>
        protected XmlDocument UnsecureXml
        {
            get
            {
                if(_unsecureXml == null)
                {
                    _unsecureXml = new XmlDocument();
                    _unsecureXml.LoadXml(UnsecureConfiguration);
                }
                return _unsecureXml;
            }
        }
        private XmlDocument _unsecureXml = null;

        /// <summary>
        /// Gets the SecureConfiguration as a XmlDocument
        /// The XML should be in the format
        /// <para />&lt;?xml version="1.0" encoding="utf-8"&gt;
        /// <para />&lt;Settings&gt;
        /// <para />  &lt;setting name = "ReportExc_ReportExecutionService" &gt;
        /// <para />    &lt; value &gt; http://localhost/ReportServer/ReportExecution2005.asmx&lt;/value&gt;
        /// <para />  &lt;/setting&gt;
        /// <para />  &lt;setting name = "Unique_Organization" &gt;
        /// <para />    &lt; value &gt; Contoso &lt;/ value &gt;
        /// <para />  &lt;/ setting &gt;
        /// <para />&lt;/ Settings &gt;
        /// </summary>
        protected XmlDocument SecureXml
        {
            get
            {
                if (_secureXml == null)
                {
                    _secureXml = new XmlDocument();
                    _secureXml.LoadXml(SecureConfiguration);
                }
                return _secureXml;
            }
        }
        private XmlDocument _secureXml = null;

        private static string GetValueNode(XmlDocument doc, string key)
        {
            XmlNode node = doc.SelectSingleNode(String.Format("Settings/setting[@name='{0}']", key));
            if (node != null)
            {
                return node.SelectSingleNode("value").InnerText;
            }
            return string.Empty;
        }
        #endregion
        
        #region Entity Properties
        #region Images
        /// <summary> 
        /// The item being handled by the event 
        /// </summary>
        protected Entity Image
        {
            get
            {
                if (_image == null)
                {
                    if (Context.InputParameters.Contains("Target") && Context.InputParameters["Target"] is Entity)
                    {
                        // Obtain the target entity from the input parameters.
                        _image = (Entity)Context.InputParameters["Target"];
                    }
                }
                return _image;
            }
        }
        private Entity _image = null;

        /// <summary> 
        /// The first pre image 
        /// </summary>
        protected Entity PreImage
        {
            get
            {
                if (_preImage == null)
                {
                    _preImage = Context.PreEntityImages.FirstOrDefault(null).Value;
                }
                return _preImage;
            }
        }
        private Entity _preImage = null;

        /// <summary> 
        /// The first post image 
        /// </summary>
        protected Entity PostImage
        {
            get
            {
                if (_postImage == null)
                {
                    _postImage = Context.PreEntityImages.FirstOrDefault(null).Value;
                }
                return _postImage;
            }
        }
        private Entity _postImage = null;

        /// <summary> 
        /// A dictionary containing all the Pre Images 
        /// </summary>
        protected Dictionary<string, Entity> PreImages
        {
            get
            {
                if (_preImages == null)
                {
                    _preImages = Context.PreEntityImages.ToDictionary(x => x.Key, x => x.Value);
                    if (_preImages == null)
                    {
                        _preImages = new Dictionary<string, Entity>();
                    }
                }
                return _preImages;
            }
        }
        private Dictionary<string, Entity> _preImages = null;

        /// <summary> 
        /// A dictionary containing all the Post Images 
        /// </summary>
        protected Dictionary<string, Entity> PostImages
        {
            get
            {
                if (_postImages == null)
                {
                    _postImages = Context.PostEntityImages.ToDictionary(x => x.Key, x => x.Value);
                    if (_postImages == null)
                    {
                        _postImages = new Dictionary<string, Entity>();
                    }
                }
                return _postImages;
            }
        }
        private Dictionary<string, Entity> _postImages = null;
        #endregion

        /// <summary> 
        /// The logical name of the entity that the plugin is executing for 
        /// </summary>
        protected string LogicalName
        {
            get
            {
                if (_logicalName == null)
                {
                    if (Image == null)
                    {
                        return null;
                    }
                    _logicalName = Image.LogicalName;
                }
                return _logicalName;
            }
        }
        private string _logicalName = null;
        #endregion

        #region Execution Details
        /// <summary>
        /// The stage that the plugin is running on
        /// </summary>
        protected PluginStage Stage
        {
            get
            {
                return (PluginStage)Context.Stage;
            }
        }

        /// <summary>
        /// The mode that the plugin is running on
        /// </summary>
        protected PluginMode Mode
        {
            get
            {
                return (PluginMode)Context.Mode;
            }
        }
        
        /// <summary>
        /// Gets a list of Shared Variables
        /// </summary>
        protected Dictionary<string,object> SharedVariables
        {
            get
            {
                if (_sharedVariables == null)
                {
                    _sharedVariables = new Dictionary<string, object>();
                    foreach(var pair in Context.SharedVariables)
                    {
                        _sharedVariables.Add(pair.Key, pair.Value);
                    }
                }
                return _sharedVariables;
            }
        }
        private Dictionary<string, object> _sharedVariables = null;
        #endregion
        #endregion

        #region Constructor
        public PluginBase():base()
        {

        }

        public PluginBase(string unsecureConfiguration, string secureConfiguration):this()
        {
            _unsecureConfiguration = unsecureConfiguration;
            _secureConfiguration = secureConfiguration;
        }
        #endregion

        #region IPlugin Implementation
        /// <inheritdoc />
        public void Execute(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            Execute();
        }
        #endregion

        /// <summary> 
        /// Method to override to execute the plugin 
        /// </summary>
        public abstract void Execute();
    }
}
