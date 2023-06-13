import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AssetComponent } from './asset.component';

const routes: Routes = [
	{ path: ':assetUid', component: AssetComponent },
	{ path: ":uid/children", loadChildren: () => import("../../components/sidebar/children/children.module").then((m) => m.ChildrenModule) },
	{ path: ":Uid/score", loadChildren: () => import("../../components/sidebar/score/score.module").then((m) => m.ScoreModule) },
	{ path: ":Uid/score/:scoreType", loadChildren: () => import("../../components/sidebar/score/score.module").then((m) => m.ScoreModule) },
	{ path: ":assetUid/comments", loadChildren: () => import("../../components/sidebar/comments/comments.module").then((m) => m.CommentsModule) },
	{ path: ":uid/log", loadChildren: () => import("../../components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
	{ path: ":assetUid/actions", loadChildren: () => import("../../components/sidebar/actions/actions.module").then((m) => m.ActionsModule) },
	{ path: ":assetUid/workflowmonitor", loadChildren: () => import("../../components/sidebar/workflowmonitor/workflow-monitor.module").then((m) => m.WorkflowMonitorModule) },
	{ path: ":assetUid/followers", loadChildren: () => import("../../components/sidebar/followers/followers.module").then((m) => m.FollowersModule) },
	{ path: ":assetUid/results", loadChildren: () => import("../../components/sidebar/ruleresults/rule-results.module").then((m) => m.RuleResultsModule) },
	{ path: ":assetUid/owners", loadChildren: () => import("../../components/sidebar/ownership/ownership.module").then((m) => m.OwnershipModule) },
	{ path: ":assetUid/diagrams", loadChildren: () => import("../../components/sidebar/visualization/visualization.module").then((m) => m.VisualizationModule) },
	{ path: ":assetUid/diagrams/:diagramType", loadChildren: () => import("../../components/sidebar/visualization/visualization.module").then((m) => m.VisualizationModule) },
	{ path: ":assetUid/diagrams/:diagramType/:focusKey", loadChildren: () => import("../../components/sidebar/visualization/visualization.module").then((m) => m.VisualizationModule) },
	{ path: ":uid/relationships", loadChildren: () => import("../../components/sidebar/relationships/relationships.module").then((m) => m.RelationshipsModule) },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AssetRoutingModule { }