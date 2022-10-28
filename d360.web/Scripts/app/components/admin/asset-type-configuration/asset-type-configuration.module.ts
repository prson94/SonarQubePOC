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
        SharedAssetTypeEditorModule
    ],
    declarations: [
        ConfigurationAssetTypeListPageComponent,
        ConfigurationAssetTypeListComponent,
        AssetTypeListSidePanelWrapperComponent,
        AssetTypeListHeaderComponent,
        StubComponent,
        ConfigurationAssetTypeEditorPageComponent
    ],
    exports: [],
})
export class AssetTypeConfigurationModule { }
