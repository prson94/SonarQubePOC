System.register(['@angular/router', './components/admin/admin.routes', './components/home/home.routes', './components/artifact/artifact.routes', './components/diagnostic/diagnostic.routes', './components/model/model.routes', './components/policy/policy.routes', './components/fusion/fusion.routes', './components/resource/resource.routes', './components/rule/rule.routes', './components/monitor/monitor.routes', './components/community/community.routes', './components/reference/reference.routes', './components/search/search.routes'], function(exports_1, context_1) {
    "use strict";
    var __moduleName = context_1 && context_1.id;
    var router_1, admin_routes_1, home_routes_1, artifact_routes_1, diagnostic_routes_1, model_routes_1, policy_routes_1, fusion_routes_1, resource_routes_1, rule_routes_1, monitor_routes_1, community_routes_1, reference_routes_1, search_routes_1;
    var routes, routing;
    return {
        setters:[
            function (router_1_1) {
                router_1 = router_1_1;
            },
            function (admin_routes_1_1) {
                admin_routes_1 = admin_routes_1_1;
            },
            function (home_routes_1_1) {
                home_routes_1 = home_routes_1_1;
            },
            function (artifact_routes_1_1) {
                artifact_routes_1 = artifact_routes_1_1;
            },
            function (diagnostic_routes_1_1) {
                diagnostic_routes_1 = diagnostic_routes_1_1;
            },
            function (model_routes_1_1) {
                model_routes_1 = model_routes_1_1;
            },
            function (policy_routes_1_1) {
                policy_routes_1 = policy_routes_1_1;
            },
            function (fusion_routes_1_1) {
                fusion_routes_1 = fusion_routes_1_1;
            },
            function (resource_routes_1_1) {
                resource_routes_1 = resource_routes_1_1;
            },
            function (rule_routes_1_1) {
                rule_routes_1 = rule_routes_1_1;
            },
            function (monitor_routes_1_1) {
                monitor_routes_1 = monitor_routes_1_1;
            },
            function (community_routes_1_1) {
                community_routes_1 = community_routes_1_1;
            },
            function (reference_routes_1_1) {
                reference_routes_1 = reference_routes_1_1;
            },
            function (search_routes_1_1) {
                search_routes_1 = search_routes_1_1;
            }],
        execute: function() {
            exports_1("routes", routes = [
                //{ path: 'a/admin', component: AdminComponent }
                { path: 'a', redirectTo: 'a/home', pathMatch: 'full' }
            ].concat(admin_routes_1.AdminRoutes, home_routes_1.HomeRoutes, artifact_routes_1.ArtifactRoutes, diagnostic_routes_1.DiagnosticRoutes, model_routes_1.ModelRoutes, policy_routes_1.PolicyRoutes, fusion_routes_1.FusionRoutes, resource_routes_1.ResourceRoutes, rule_routes_1.RuleRoutes, monitor_routes_1.MonitorRoutes, community_routes_1.CommunityRoutes, reference_routes_1.ReferenceRoutes, search_routes_1.SearchRoutes));
            exports_1("routing", routing = router_1.RouterModule.forRoot(routes));
        }
    }
});
