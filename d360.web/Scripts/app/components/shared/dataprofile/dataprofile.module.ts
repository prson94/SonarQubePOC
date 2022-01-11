import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { HTTP_INTERCEPTORS, HttpClientModule } from "@angular/common/http";
import { DataProfileComponent } from "./dataprofile.component";

import { PropertyGroupModule } from "../controls/property-group/property-group.component";
import { DirectivesModule } from '../../../directives/directives.module';
import { TooltipModule } from 'primeng/tooltip';
import { PipesModule } from '../../../pipes/pipes.module';
import { MatchDetectionComponent } from "./match-detection.component";
import { SiteModalModule } from "../modal/gov-modal.module";
import { TableModule } from "primeng/table";
import { SharedGridPagingInfoModule } from "../grid-paging-info.component";
import { PopupMenuModule } from "../controls/popup-menu/popup-menu.component";
import { SearchFieldModule } from "../controls/search-field/search-field.component";
import { FormsModule } from "@angular/forms";
import { TagViewModule } from '../tags/d3s-tag-view.module';
import { AdvancedFiltersModule } from "../../assets-grid/advanced-filtering/advanced-filtering.module";
import { SidePanelModule } from "../sidepanel/side-panel.module";
import { CoreModule } from "../core.module";
import { AssetDetailModule } from "../asset-detail/asset-detail.module";
import { ModalDrawerModule } from '../modal-drawer/gov-modal-drawer.module';
import { TagPickerModule } from '../controls/tag-picker/tag-picker';
import { DropdownModule } from 'primeng/dropdown';
import { DataProfileTimeSeriesComponent } from "./dataprofile-time-series.component";

@NgModule({
    imports: [        
        CommonModule,
        FormsModule,
        
        PropertyGroupModule,
        DirectivesModule,
        TooltipModule,
        PipesModule,
        SiteModalModule,
        TableModule,
        SharedGridPagingInfoModule,
        PopupMenuModule,
        SearchFieldModule,
        TagViewModule,
        AdvancedFiltersModule,
        SidePanelModule,
        CoreModule,
        AssetDetailModule,
        ModalDrawerModule,
        TagPickerModule,
        DropdownModule
    ],
    declarations: [
        DataProfileComponent,
        MatchDetectionComponent,
        DataProfileTimeSeriesComponent
    ],
    exports: [
        DataProfileComponent,
    ],
    providers: [
                
    ]
})
export class DataProfileModule { }