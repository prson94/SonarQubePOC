import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule } from '../shared/tiles/tiles.module';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedFieldDefinitionModule } from '../shared/fielddefinition/shared-field-definition.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';
import { SharedAssetTypeEditorModule } from '../shared/assettypeeditor/shared-asset-type-editor.module';
import { HeaderModule } from '../shared/header/header.module';
import { PageHeaderModule } from '../shared/page-header/page-header.module';
import { TabsModule } from '../shared/tabs/tabs.module';
import { SharedResponsibilitiesModule } from '../shared/responsibilities/shared-responsibilities.module';
import { RelationshipGridModule } from '../shared/relationship-grid/relationship-grid.module';

import { ReferenceV2RoutingModule } from './reference-v2.routes';
import { ReferenceV2Component } from './reference-v2.component';

import { ReferenceItemTypeListV2Component } from './list/reference-item-type-list-v2.component';
import { ReferenceItemTypeDefinitionComponent } from './tabs/definition/reference-item-type-definition.component';
import { ReferenceItemTypeFieldsComponent } from './tabs/fields/reference-item-type-fields.component';
import { ReferenceItemTypeItemsComponent } from './tabs/items/referrence-item-type-items.component';
import { ReferenceItemTypeLogComponent } from './tabs/log/reference-item-type-log.component';
import { ReferenceItemTypeRelationshipsComponent } from './tabs/relationships/referemce-item-type-relationships.component';
import { ReferenceItemTypeResponsibilitiesComponent } from './tabs/responsibilities/reference-item-type-responsibilities.component';

import { ReferenceItemTypeHeaderComponent } from './tabs/shared/reference-header.component';
import { ReferenceItemTypeTabsComponent } from './tabs/shared/reference-tabs.component';

import { ReferenceItemsModule } from '../shared/reference-items/reference-items.module';
import { ConfigurationAssetTypeDeletePageComponentModule } from '../admin/asset-type-configuration/delete/configuration-asset-type-delete-page.module';
import { AssetTypeModalFormModule } from '../admin/asset-type-configuration/editor/asset-type-modal-form.module';

import { AngularSplitModule } from 'angular-split';
import { SidePanelModule } from '../shared/sidepanel/side-panel.module';
import { AssetTypeDetailV2Module } from '../shared/asset-type-detail-v2/asset-type-detail-v2.module';
import { PopupMenuModule } from '../shared/controls/popup-menu/popup-menu.component';
import { FieldTypeDetailModule } from '../shared/fielddefinition/field-type-details/field-type-details.module';
import { AssetDetailModule } from '../shared/asset-detail/asset-detail.module';
import { SiteModalModule } from "../shared/modal/gov-modal.module";
import { PortalsModule } from '../shared/portals/portals.module';
import { AuditModule } from '../sidebar/audit/audit.module';
import { MonitorModule } from '../monitor/monitor.module';
import { AssetPreviewModule } from '../shared/asset-preview/asset-preview.module';

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { EditorModule } from 'primeng/editor';
import { TooltipModule } from 'primeng/tooltip';
import { TableModule } from 'primeng/table';
import { DirectivesModule } from '../../directives/directives.module';
import { SearchFieldModule } from '../shared/controls/search-field/search-field.component';
import { AssignmentsModule } from '../assignments/assignments.module';
import { ReferenceItemTypeAssignmentsComponent } from './tabs/assignments/reference-item-type-assignments.component';

@NgModule({
	imports: [
		CommonModule,
		FormsModule,

		RouterModule,

		ReferenceV2RoutingModule,

		//primeng
		ButtonModule,
		EditorModule,
		InputTextModule,
		SharedModule,
		TooltipModule,
		TableModule,

		AngularSplitModule,
		SidePanelModule,
		AssetTypeDetailV2Module,
		PopupMenuModule,
		AssetTypeModalFormModule,
		ConfigurationAssetTypeDeletePageComponentModule,
		FieldTypeDetailModule,
		AssetDetailModule,
		SiteModalModule,
		PortalsModule,
		HeaderModule,

		//d3s        
		CoreModule,
		PipesModule,
		DirectivesModule,
		PageHeaderModule,
		TabsModule,
		AuditModule,
		SharedResponsibilitiesModule,
		RelationshipGridModule,
		MonitorModule,
		AssetPreviewModule,
		ReferenceItemsModule,
		SharedDeleteFormModule,
		SharedFieldDefinitionModule,
		SharedDynamicGridEditorModule,
		SharedGridPagingInfoModule,
		SharedObjectDetailsModule,
		SharedAssetTypeEditorModule,
		TilesModule,
		SearchFieldModule,
		AssignmentsModule
	],
	declarations: [
		ReferenceV2Component,
		ReferenceItemTypeListV2Component,
		ReferenceItemTypeDefinitionComponent,
		ReferenceItemTypeFieldsComponent,
		ReferenceItemTypeItemsComponent,
		ReferenceItemTypeLogComponent,
		ReferenceItemTypeRelationshipsComponent,
		ReferenceItemTypeResponsibilitiesComponent,
		ReferenceItemTypeHeaderComponent,
		ReferenceItemTypeAssignmentsComponent,
		ReferenceItemTypeTabsComponent,
	],
	exports: [
		ReferenceV2Component,
		ReferenceItemTypeListV2Component,
		ReferenceItemTypeDefinitionComponent,
		ReferenceItemTypeFieldsComponent,
		ReferenceItemTypeItemsComponent,
		ReferenceItemTypeLogComponent,
		ReferenceItemTypeRelationshipsComponent,
		ReferenceItemTypeResponsibilitiesComponent,
		ReferenceItemTypeAssignmentsComponent,
		ReferenceItemTypeHeaderComponent,
		ReferenceItemTypeTabsComponent,
	],
	providers: []
})
export class ReferenceV2Module { }