import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';


import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';

import { HomeSearchComponent} from './home-search.component'
import { HeroSearchInputComponent } from './hero-search-input';
import { SearchResultItemComponent } from './search-result-item.component'
import { SearchComponent } from './search.component'
import { DynamicPercentageModule } from '../shared/small-widgets/dynamic-percentage/dynamic-percentage-module';
import { SimpleBadgeModule } from '../shared/small-widgets/simple-badge/simple-badge.module';
import { ScoreBadgeModule } from '../shared/small-widgets/score-badge/score-badge.module';
import { PaginatorModule } from '../shared/small-widgets/paginator/paginator-bar-module';
import { CheckTreeModule } from '../shared/small-widgets/check-tree/check-tree.module';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { ChipsFilterModule } from '../shared/small-widgets/chips-filter/chips-filter-module';
import { SearchRoutingModule } from './search.routes';
import { ExplainWidgetModule } from './explain-widget/explain-widget.module';
import { AssetPathWidgetModule } from './asset-path-widget/asset-path-widget.module';

import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { MenuModule } from 'primeng/menu';
import { IgBadgeModule } from '../shared/controls/badge/badge.module';
import { PopupMenuModule } from '../shared/controls/popup-menu/popup-menu.component';

import { AdvancedFiltersModule } from "../assets-grid/advanced-filtering/advanced-filtering.module";
import { TypeaheadSearchModule } from '../shared/search/typeahead-search.component';
import { SearchStateService } from './search-state.service';
import { TagViewModule } from '../shared/tags/d3s-tag-view.module';
import { PreviewpopupModule } from '../shared/previewpopup/previewpopup.module';
import { SidePanelModule } from '../shared/sidepanel/side-panel.module';
import { AssetDetailModule } from "../shared/asset-detail/asset-detail.module";
import { DataProfileModule } from '../shared/dataprofile/dataprofile.module';
import { SiteModalModule } from '../shared/modal/gov-modal.module';
import { AssetEditorModule } from '../shared/asset-editor/asset-editor.module';
import { SemanticsModule } from "../semantic/semantics.module";


@NgModule({
    imports: [
        CommonModule,
        FormsModule,                

        RouterModule,

        SearchRoutingModule,

        //primeng         
        InputTextModule,                  
        ButtonModule,
        DropdownModule,
        CheckboxModule,                        
        MultiSelectModule,        
        TooltipModule,        
        PaginatorModule,
        SharedModule,
        CheckTreeModule,
        MenuModule,
        PreviewpopupModule,

        //d3s        
        CoreModule,
        TilesModule,
        DynamicPercentageModule,
        SimpleBadgeModule,
        ScoreBadgeModule,
        SharedDynamicGridEditorModule,
        PaginatorModule,
        ChipsFilterModule,
        TagViewModule,
        TypeaheadSearchModule,
        ExplainWidgetModule,
        IgBadgeModule,
        AdvancedFiltersModule,
        AssetPathWidgetModule,
        SidePanelModule,
        AssetDetailModule,
        DataProfileModule,
        PopupMenuModule,
        SiteModalModule,
        AssetEditorModule,
        SemanticsModule
    ],
    declarations: [
        HomeSearchComponent,
        SearchResultItemComponent,
        SearchComponent,
        HeroSearchInputComponent
    ],
    exports: [
        HomeSearchComponent,
        HeroSearchInputComponent,
    ],
    providers: [        
        
        SearchStateService
    ]
})
export class SearchModule { }