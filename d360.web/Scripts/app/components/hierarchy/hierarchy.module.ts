import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";
import { RouterModule } from '@angular/router';


import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedAssetEditorsModule } from '../shared/asseteditors/shared-asset-editor.module';
import { HierarchyRoutingModule } from './hierarchy.routes';

import { HierarchyComponent } from './hierarchy.component';
import { HierarchyListComponent } from './hierarchy-list.component';
import { HierarchyItemComponent } from './hierarchy-item.component';
import { HierarchyItemStructureComponent } from './hierarchy-item-structure.component';

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { InputMaskModule } from 'primeng/inputmask';
import { DropdownModule } from 'primeng/dropdown';
import { MultiSelectModule } from 'primeng/multiselect';
import { TooltipModule } from 'primeng/tooltip';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TreeTableModule } from 'primeng/treetable';
import { ToastModule } from 'primeng/toast';
import { TreeModule } from 'primeng/tree';
import { TableModule } from 'primeng/table';
import { SharedAssetScoreModule } from '../shared/asset-score/shared-asset-score.module';
import { SearchFieldModule } from "../shared/controls/search-field/search-field.component";
import { AdvancedFiltersModule } from '../assets-grid/advanced-filtering/advanced-filtering.module';
import { AssetDetailModule } from '../shared/asset-detail/asset-detail.module';
import { DataProfileModule } from '../shared/dataprofile/dataprofile.module';
import { SidePanelModule } from '../shared/sidepanel/side-panel.module';
import { PopupMenuModule } from '../shared/controls/popup-menu/popup-menu.component';
import { AssetEditorModule } from '../shared/asset-editor/asset-editor.module';
import { SiteModalModule } from '../shared/modal/gov-modal.module';
import { AssetTypeDetailModule } from '../shared/asset-type-detail/asset-type-detail.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        HierarchyRoutingModule,

        //primeng
        ToastModule,
        InputTextModule,
        InputMaskModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        SelectButtonModule,
        MultiSelectModule,
        TooltipModule,
        TreeModule,
        SharedModule,
        TableModule,

        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,

        SharedGridPagingInfoModule,
        SharedDeleteFormModule,
        SharedAssetScoreModule,
        SharedDynamicGridEditorModule,
        SharedAssetEditorsModule,
        AssetEditorModule,
        SiteModalModule,
        TilesModule,
        AssetDetailModule,
        AssetTypeDetailModule,
        DataProfileModule,
        SidePanelModule,
        PopupMenuModule,

        AdvancedFiltersModule,
        SearchFieldModule
    ],
    declarations: [
        HierarchyComponent,
        HierarchyListComponent,
        HierarchyItemComponent,
        HierarchyItemStructureComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class HierarchyModule { }