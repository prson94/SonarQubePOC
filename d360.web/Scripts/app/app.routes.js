"use strict";
var router_1 = require('@angular/router');
var admin_routes_1 = require('./components/admin/admin.routes');
var home_routes_1 = require('./components/home/home.routes');
var artifact_routes_1 = require('./components/artifact/artifact.routes');
var model_routes_1 = require('./components/model/model.routes');
var policy_routes_1 = require('./components/policy/policy.routes');
var fusion_routes_1 = require('./components/fusion/fusion.routes');
var resource_routes_1 = require('./components/resource/resource.routes');
var rule_routes_1 = require('./components/rule/rule.routes');
var monitor_routes_1 = require('./components/monitor/monitor.routes');
var community_routes_1 = require('./components/community/community.routes');
var reference_routes_1 = require('./components/reference/reference.routes');
var search_routes_1 = require('./components/search/search.routes');
var group_routes_1 = require('./components/group/group.routes');
var workflow_routes_1 = require('./components/workflow/workflow.routes');
exports.routes = [
    //{ path: 'a/admin', component: AdminComponent }
    { path: 'a', redirectTo: 'a/home', pathMatch: 'full' }
].concat(admin_routes_1.AdminRoutes, home_routes_1.HomeRoutes, artifact_routes_1.ArtifactRoutes, group_routes_1.GroupRoutes, model_routes_1.ModelRoutes, policy_routes_1.PolicyRoutes, fusion_routes_1.FusionRoutes, resource_routes_1.ResourceRoutes, rule_routes_1.RuleRoutes, monitor_routes_1.MonitorRoutes, community_routes_1.CommunityRoutes, reference_routes_1.ReferenceRoutes, search_routes_1.SearchRoutes, workflow_routes_1.WorkflowRoutes);
exports.routing = router_1.RouterModule.forRoot(exports.routes);
//# sourceMappingURL=app.routes.js.map