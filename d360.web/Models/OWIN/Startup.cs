using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Diagnostics;
using System.Configuration;
using d360.core;

[assembly: OwinStartup(typeof(d360.web.Models.OWIN.Startup))]

namespace d360.web.Models.OWIN
{
    public class ErrorHandlingPipelineModule : HubPipelineModule
    {
        protected override void OnIncomingError(ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
        {
            Debug.WriteLine("=> Exception " + exceptionContext.Error.Message);
            if (exceptionContext.Error.InnerException != null)
            {
                Debug.WriteLine("=> Inner Exception " + exceptionContext.Error.InnerException.Message);
            }
            base.OnIncomingError(exceptionContext, invokerContext);
        } 
    }

    public class LoggingPipelineModule : HubPipelineModule
    {
        protected override bool OnBeforeIncoming(IHubIncomingInvokerContext context)
        {
            Debug.WriteLine("=> Invoking " + context.MethodDescriptor.Name + " on hub " + context.MethodDescriptor.Hub.Name);
            return base.OnBeforeIncoming(context);
        }
        protected override bool OnBeforeOutgoing(IHubOutgoingInvokerContext context)
        {
            Debug.WriteLine("<= Invoking " + context.Invocation.Method + " on client hub " + context.Invocation.Hub);
            return base.OnBeforeOutgoing(context);
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var hubConfiguration = new HubConfiguration() { EnableDetailedErrors = false };

            GlobalHost.DependencyResolver.UseRedis("d3ssession.redis.cache.windows.net", 6379, "bnIUbvztGIYchNT/VSz4iHHaL/ChYMLsppmXLbJp5Jw=", "UI-SignalR");
            //GlobalHost.DependencyResolver.UseRedis("d3ssignalr.redis.cache.windows.net", 6380, "8ymYsgGiMttNlZeApex9AAPGmywzEyMnPMJVPfW7dwo=", "UI-SignalR");
            //var connectionString = constants.SERVICE_BUS_UI;
            //GlobalHost.DependencyResolver.UseServiceBus(connectionString, "D3S-UI");
            GlobalHost.HubPipeline.AddModule(new LoggingPipelineModule()); 
            GlobalHost.HubPipeline.AddModule(new ErrorHandlingPipelineModule());
            app.MapSignalR(hubConfiguration);     // 2.0
        }
    }
}
