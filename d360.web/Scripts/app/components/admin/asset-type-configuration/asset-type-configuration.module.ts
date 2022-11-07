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
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { SearchFieldModule } from '../../shared/controls/search-field/search-field.component';
import { StubComponent } from './stub.compnoent';
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

@NgModule({
    imports: [
        RouterModule.forChild(assetTypeConfigurationRoutes),
        CommonModule,
        CoreModule,
        TreeTableModule,
        PipesModule,
        SidePanelModule,
        AngularSplitModule,
        SharedObjectDetailsModule,
        SearchFieldModule,
        D3SSortIconModule,
        SharedAssetTypeEditorModule,
        AssetTypeDeleteModule,
        SharedFieldDefinitionModule,
        PageHeaderModule,
        TabsModule,
        SharedResponsibilitiesModule,
        AdminModule,
        AdminRelationshipEditorModule
    ],
    declarations: [
        ConfigurationAssetTypeListPageComponent,
        ConfigurationAssetTypeListComponent,
        AssetTypeListSidePanelWrapperComponent,
        AssetTypeListHeaderComponent,
        StubComponent,
        ConfigurationAssetTypeEditorPageComponent,
        ConfigurationAssetTypeDeletePageComponent,
        ConfigurationAssetTypeFieldsPageComponent,
        ConfigurationAssetTypeHeaderComponent,
        ConfigurationAssetTypeTabsComponent,
        ConfigurationAssetTypeOwnersPageComponent,
        ConfigurationAssetTypeAllocationsPageComponent,
        ConfigurationAssetTypeRelationshipsPageComponent
    ],
    exports: [],
})
export class AssetTypeConfigurationModule { }
