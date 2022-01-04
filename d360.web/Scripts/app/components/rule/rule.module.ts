import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';
import { SharedAssetEditorsModule } from '../shared/asseteditors/shared-asset-editor.module';

import { RuleRoutingModule } from './rule.routes';

import { RuleComponent } from './rule.component';
import { RuleListComponent } from './rule-list.component';
import { RuleItemComponent } from './rule-item.component';
import { RuleResultsGridComponent } from './rule-results-grid.component';
import { RuleColumnFilterComponent } from './rule-column-filter.component';


import { TabViewModule } from 'primeng/tabview';
import { CheckboxModule } from 'primeng/checkbox';
import { SelectButtonModule } from 'primeng/selectbutton';
import { ToastModule } from 'primeng/toast';
import { MultiSelectModule } from 'primeng/multiselect';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { InputMaskModule } from 'primeng/inputmask';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { TreeTableModule } from 'primeng/treetable';
import { TooltipModule } from 'primeng/tooltip';
import { TableModule } from 'primeng/table';
import { CalendarModule } from 'primeng/calendar';
import { AssetGridModule } from '../assets-grid/asset-grid.module';
import { SharedAssetScoreModule } from '../shared/asset-score/shared-asset-score.module';
import { SearchFieldModule } from '../shared/controls/search-field/search-field.component';
import { AdvancedFiltersModule } from '../assets-grid/advanced-filtering/advanced-filtering.module';
import { SidePanelModule } from '../shared/sidepanel/side-panel.module';
import { AssetDetailModule } from '../shared/asset-detail/asset-detail.module';
import { DataProfileModule } from '../shared/dataprofile/dataprofile.module';
import { AssetTypeDetailModule } from '../shared/asset-type-detail/asset-type-detail.module';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        RuleRoutingModule,

        //primeng
        ToastModule,
        InputTextModule,
        InputMaskModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,                        
        SelectButtonModule,        
        MultiSelectModule, 
        TabViewModule,
        TooltipModule,                
        SharedModule,
        CalendarModule,
        TableModule,
                
        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,
        TilesModule,
        SidePanelModule,
        AssetDetailModule,
        DataProfileModule,
        
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,        
        SharedDynamicGridEditorModule,
        SharedObjectDetailsModule,
        SharedAssetScoreModule,
        SharedAssetEditorsModule,
        AssetGridModule,
        SearchFieldModule,
        AdvancedFiltersModule,
        AssetTypeDetailModule
    ],
    declarations: [
        RuleComponent,
        RuleListComponent,
        RuleItemComponent,        
        RuleResultsGridComponent,
        RuleColumnFilterComponent
    ],
    exports: [
        RuleResultsGridComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class RuleModule { }