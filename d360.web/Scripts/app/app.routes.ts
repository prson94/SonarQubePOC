import { NgModule } from "@angular/core";
import { RouterModule, Routes } from "@angular/router";
import { RedirectGuard } from "./guards/redirect.guard";


const routes: Routes = [
	{
		path: "",
		children: [],
		canActivate: [RedirectGuard],
		pathMatch: "full"
	},

	// lazy loaded modules 
	{ path: "assettype", loadChildren: () => import("./components/asset/asset.module").then((m) => m.AssetModule), data: { preload: false } },
	{ path: "assets", loadChildren: () => import("./components/assets-base/assets-base.module").then((m) => m.AssetsBaseModule), data: { preload: false } },
	{ path: "assets", loadChildren: () => import("./components/reference/reference.module").then((m) => m.ReferenceModule) },
	{ path: "community", loadChildren: () => import("./components/community/community.module").then((m) => m.CommunityModule) },
	{ path: "help", loadChildren: () => import("./components/help/help.module").then((m) => m.HelpModule) },
	{ path: "admin", loadChildren: () => import("./components/admin/admin.module").then((m) => m.AdminModule) },
	{ path: "monitor", loadChildren: () => import("./components/monitor/monitor.module").then((m) => m.MonitorModule) },
	{ path: "quality/rule", loadChildren: () => import("./components/rule/rule.module").then((m) => m.RuleModule) },
	{ path: "tag", loadChildren: () => import("./components/tag/tag.module").then((m) => m.TagModule) },
	{ path: "connectorLabel", loadChildren: () => import("./components/connector-label/connector-label.module").then((m) => m.ConnectorLabelModule) },
	{ path: "group", loadChildren: () => import("./components/group/group.module").then((m) => m.GroupModule) },
	{ path: "home", loadChildren: () => import("./components/home/home.module").then((m) => m.HomeModule) },
	{ path: "gallery", loadChildren: () => import("./components/gallery/gallery.module").then((m) => m.GalleryModule) },
	{ path: "search", loadChildren: () => import("./components/search/search.module").then((m) => m.SearchModule) },
	{ path: "workflow", loadChildren: () => import("./components/workflow/workflow.module").then((m) => m.WorkflowModule) },
	{ path: "dashboard", loadChildren: () => import("./components/sidebar/dashboard/dashboard.module").then((m) => m.DashboardModule) },
	{ path: "cart", loadChildren: () => import("./components/shoppingcart/shopping-cart.module").then((m) => m.ShoppingCartModule) },
	{ path: "sidebar/itemfollow", loadChildren: () => import("./components/sidebar/itemfollow/itemfollow.module").then((m) => m.ItemFollowModule) },
	{ path: "sidebar/itemown", loadChildren: () => import("./components/sidebar/itemown/itemown.module").then((m) => m.ItemOwnModule) },
	{ path: "users", loadChildren: () => import("./components/resource/resource.module").then((m) => m.ResourceModule) },
	{ path: "users", loadChildren: () => import("./components/sidebar/membergroup/membergroup.module").then((m) => m.MemberGroupModule) },
	//sidebar
	{ path: "assets", loadChildren: () => import("./components/sidebar/permissions/permissions.module").then((m) => m.PermissionsModule) },
	{ path: "asset", loadChildren: () => import("./components/asset/asset.module").then((m) => m.AssetModule), data: { preload: false } },
	{ path: "assets/:assetTypeUid/fields", loadChildren: () => import("./components/sidebar/fields/fields.module").then((m) => m.FieldsModule) },
	{ path: "semantics/:uid/log", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
	{ path: "semantics", loadChildren: () => import("./components/semantic/semantics.module").then((m) => m.SemanticsModule) },
	{ path: "tag/:uid/log", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
	{ path: "tag", loadChildren: () => import("./components/sidebar/audit/audit.module").then((m) => m.AuditModule) },
	{ path: "dataCatalog", loadChildren: () => import("./components/data-catalog/data-catalog.module").then((m) => m.DataCatalogModule) },

	{
		path: "**",
		canActivate: [RedirectGuard],
		children: [],
	},

];

@NgModule({
	imports: [RouterModule.forRoot(routes, { onSameUrlNavigation: "reload" })],
	exports: [RouterModule],
})
export class AppRoutingModule { }