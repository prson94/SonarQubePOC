using Microsoft.VisualStudio.TestTools.UnitTesting;
using d360.web.Controllers;
using d360.model;
using d360.extensions;
using d360.extensions.caching;
using d360.extensions.queue;
using System.Net.Http;
using System.Web.Http;
using System.Net;
using d360.web.Models;
using System;
using System.Collections.Generic;
namespace d360.test.web.Tests.Controllers.ApiController
{
    public abstract class BaseApiTest
    {
        public ICachingProvider cache;
        public IQueueSource queue;
        public ISecurityContextProvider context;
        public int resourceId;
        public int companyId;
        public bool isAdmin;
        public string companyPrefix;

        public CommunityContext community;
        public CompanyContext company;
        public D3SApiController controller;

        public BaseApiTest()
        {
            resourceId = 0;
            companyId = 4;
            companyPrefix = "demo.dev";
            isAdmin = true;

            cache = new DummyCachingProvider();
            queue = new DummyQueueSource();
            context = new DummySecurityContextProvider();

            context.CompanyID = companyId;
            context.CompanyPrefix = companyPrefix;
            context.IsAdministrator = isAdmin;
            context.ResourceID = resourceId;

            community = new CommunityContext(cache, queue, context);
            company = new CompanyContext(community, cache, queue, context, true);

            controller = new D3SApiController(community, company, context);
            controller.Request = new HttpRequestMessage();
            controller.Configuration = new HttpConfiguration();
            
        }

    }
}
