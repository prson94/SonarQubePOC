using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Autofac.Integration.Mvc;
using d360.extensions;

namespace d360.web.Models.Modules
{
    public enum EnvironmentMode
    { 
        AzureHosted,
        SelfHosted
    }
    public class EnvironmentModule: Module
    {
        public EnvironmentMode Mode { get; set; }

        //public EnvironmentModule()
        //{
        //    Mode = EnvironmentMode.AzureHosted;
        //}

        protected override void Load(ContainerBuilder builder)
        {
            if (Mode == EnvironmentMode.AzureHosted)
            {
                builder.RegisterType<d360.extensions.search.AzureSearchSource>().As<ISearchSource>().InstancePerRequest();
                builder.RegisterType<d360.extensions.caching.MemoryCachingProvider>().As<ICachingProvider>().InstancePerRequest();
                builder.RegisterType<d360.extensions.queue.AzureQueueSource>().As<IQueueSource>().InstancePerRequest();
                builder.RegisterType<d360.extensions.storage.AzureStorageProvider>().As<IStorageProvider>().InstancePerRequest();
            }
            else
            {
                //builder.RegisterType<d360.extensions.search.local.SearchSource>().As<ISearchSource>().InstancePerRequest();
                builder.RegisterType<d360.extensions.caching.MemoryCachingProvider>().As<ICachingProvider>().InstancePerRequest();
                builder.RegisterType<d360.extensions.queue.AzureQueueSource>().As<IQueueSource>().InstancePerRequest();
                //builder.RegisterType<d360.extensions.storage.network.StorageProvider>().As<IStorageProvider>().InstancePerRequest();
            }
        }
    }
}