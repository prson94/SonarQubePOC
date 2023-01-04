import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TreeTableModule } from 'primeng/treetable';

import { CoreModule } from '../../shared/core.module';
import { ConfigurationAssetTypeListPageComponent } from './list/configuration-asset-type-list-page.component';
import { ConfigurationAssetTypeListComponent } from './list/configuration-asset-type-list.component';
import { PipesModule } from '../../../pipes/pipes.module';
import { SidePanelModule } from '../../shared/sidepanel/side-panel.module';
import { AngularSplitModule } from 'angular-split';
import { AssetTypeListSidePanelWrapperComponent } from './list/asset-type-list-sidepanel-wrapper.component';
import { SearchFieldModule } from '../../shared/controls/search-field/search-field.component';
import { D3SSortIconModule } from '../../shared/turbotable-sorticon.component';
import { assetTypeConfigurationRoutes } from './asset-type-configuration.routes';
import { AssetTypeListHeaderComponent } from './list/asset-type-list-header.component';
import { ConfigurationAssetTypeEditorPageComponent } from './edit/configuration-asset-type-editor-page.component';
import { SharedAssetTypeEditorModule } from '../../shared/assettypeeditor/shared-asset-type-editor.module';
import { ConfigurationAssetTypeDeletePageComponent } from './delete/configuration-asset-type-delete-page.component';
import { AssetTypeDeleteModule } from '../asset-type-delete/asset-type-delete.module';
import { ConfigurationAssetTypeFieldsPageComponent } from './tabs/fields/configuration-asset-type-fields-page.component';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { ConfigurationAssetTypeHeaderComponent } from './tabs/shared/configuration-asset-type-header/asset-type-header.component';
import { PageHeaderModule } from '../../shared/page-header/page-header.module';
import { TabsModule } from '../../shared/tabs/tabs.module';
import { ConfigurationAssetTypeTabsComponent } from './tabs/shared/configuration-asset-type-tabs/asset-type-tabs.component';
import { ConfigurationAssetTypeOwnersPageComponent } from './tabs/owners/configuration-asset-type-owners-page.component';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';
import { ConfigurationAssetTypeAllocationsPageComponent } from './tabs/allocations/configuration-asset-type-allocations-page.component';
import { AdminModule } from '../admin.module';
import { ConfigurationAssetTypeRelationshipsPageComponent } from './tabs/relationships/configuration-asset-type-relationships-page.component';
import { AdminRelationshipEditorModule } from '../../shared/relationshipeditor/admin-relationship-editor.module';
import { ConfigurationAssetTypeLogPageComponent } from './tabs/log/configuration-asset-type-log-page.component';
import { AuditModule } from '../../sidebar/audit/audit.module';
import { ConfigurationAssetTypeBreadcrumbsComponent } from './tabs/shared/configuration-asset-type-breadcrumbs/configuration-asset-type-breadcrumbs.component';
import { HeaderModule } from '../../shared/header/header.module';
import { ConfigurationAssetTypeListTabsComponent } from './list/asset-type-list-tabs.component';
import { GovernanceRolesComponent } from './governanceRoles/governance-roles.component';
import { EditorModule } from 'primeng/editor';
import { DropdownModule } from 'primeng/dropdown';
import { ButtonModule } from 'primeng/button';
import { ConfigurationAssetTypeConnectorLabelsPageComponent } from './connectorLabels/configuration-asset-type-connector-labels-page.component';
import { ConnectorLabelsModule } from './connectorLabels/connector-labels.module';
import { AssetTypeDetailV2Module } from '../../shared/asset-type-detail-v2/asset-type-detail-v2.module';
import { SharedGridPagingInfoModule } from "../../shared/grid-paging-info.component";
import { PortalsModule } from '../../shared/portals/portals.module';
import { AssetTypeModalFormModule } from './editor/asset-type-modal-form.module';
import { PopupMenuModule } from '../../shared/controls/popup-menu/popup-menu.component';
import { SiteModalModule } from '../../shared/modal/gov-modal.module';
import { CheckboxModule } from 'primeng/checkbox';
import { FormsModule } from "@angular/forms";
import { ConfigurationAssetTypeLevelsPageComponent } from './tabs/levels/configuration-asset-type-levels-page.component';
import { AdminLevelListComponent } from './levels/admin-level-list.component';
import { AdminLevelEditorComponent } from './levels/admin-level-editor.component';
import { TableModule } from 'primeng/table';
import { IgMessageBoxModule } from '../../shared/controls/message-box/message-box.module';
import { AssetPreviewModule } from '../../shared/asset-preview/asset-preview.module';

@NgModule({
    imports: [
        RouterModule.forChild(assetTypeConfigurationRoutes),
        CommonModule,
        FormsModule,
        CoreModule,
        TreeTableModule,
        EditorModule,
        DropdownModule,
        ButtonModule,
        PipesModule,
        SidePanelModule,
        AngularSplitModule,
        SearchFieldModule,
        D3SSortIconModule,
        SharedAssetTypeEditorModule,
        AssetTypeDeleteModule,
        SharedFieldDefinitionModule,
        PageHeaderModule,
        TabsModule,
        SharedResponsibilitiesModule,
        AdminModule,
        AdminRelationshipEditorModule,
        AuditModule,
        HeaderModule,
        ConnectorLabelsModule,
        AssetTypeDetailV2Module,
        SharedGridPagingInfoModule,
		PortalsModule,
		AssetTypeModalFormModule,
		PopupMenuModule,
		SiteModalModule,
		CheckboxModule,
        PortalsModule,
		FormsModule,
		TableModule,
		IgMessageBoxModule,
		AssetPreviewModule
    ],
    declarations: [
        ConfigurationAssetTypeListPageComponent,
        ConfigurationAssetTypeListComponent,
        AssetTypeListSidePanelWrapperComponent,
		AssetTypeListHeaderComponent,
        ConfigurationAssetTypeEditorPageComponent,
        ConfigurationAssetTypeDeletePageComponent,
        ConfigurationAssetTypeFieldsPageComponent,
        ConfigurationAssetTypeHeaderComponent,
        ConfigurationAssetTypeTabsComponent,
        ConfigurationAssetTypeOwnersPageComponent,
        ConfigurationAssetTypeAllocationsPageComponent,
        ConfigurationAssetTypeRelationshipsPageComponent,
        ConfigurationAssetTypeLogPageComponent,
        ConfigurationAssetTypeBreadcrumbsComponent,
        ConfigurationAssetTypeListTabsComponent,
        ConfigurationAssetTypeConnectorLabelsPageComponent,
        GovernanceRolesComponent,
		ConfigurationAssetTypeLevelsPageComponent,
		AdminLevelListComponent,
		AdminLevelEditorComponent
    ],
    exports: [],
})
export class AssetTypeConfigurationModule { }
