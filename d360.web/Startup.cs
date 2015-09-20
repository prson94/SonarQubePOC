using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Microsoft.AspNet.SignalR;

[assembly: OwinStartup(typeof(d360.web.Startup))]
namespace d360.web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //var di = new DiModel();
            //var config = new HubConfiguration() { Resolver = new Autofac.Integration.SignalR.AutofacDependencyResolver(di.GetContainer()) };
            app.MapSignalR();// (config);
        }
    }
}
