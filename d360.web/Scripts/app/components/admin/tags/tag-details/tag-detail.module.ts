import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { PipesModule } from "../../../../pipes/pipes.module";
import { TooltipModule } from "primeng/tooltip";
import { CoreModule } from '../../../shared/core.module';
import { JsonViewerModule } from '../../../shared/controls/json-viewer/json-viewer.component';
import { IgColorPickerModule } from "../../../shared/controls/color-picker/color-picker.module";
import { TagViewModule } from "../../../shared/tags/d3s-tag-view.module";
import { SharedObjectDetailsModule } from "../../../shared/objectdetails/shared-object-details.module";
import { PropertyGroupModule } from "../../../shared/controls/property-group/property-group.component";
import { TilesModule } from "../../../shared/tiles/tiles.module";
import { TableModule } from "primeng/table";
import { SharedGridPagingInfoModule } from "../../../shared/grid-paging-info.component";
import { AdvancedFiltersModule } from "../../../assets-grid/advanced-filtering/advanced-filtering.module";
import { SearchFieldModule } from "../../../shared/controls/search-field/search-field.component";
import { LinkDisplayModule } from "../../../shared/controls/link-display/link-display.component";
import { DynamicFieldNameModule } from "../../../shared/dynamic-field-name.component";
import { PortalsModule } from "../../../shared/portals/portals.module";
import { PropertyGroupComponent } from '../../../shared/controls/property-group/property-group.component';
import { TaggedAssetDetailModule } from '../../../shared/tagged-assets/tagged-assets-detail.module';

@NgModule({
	imports: [
		TaggedAssetDetailModule,
        CommonModule,
        FormsModule,
        CoreModule,
        PipesModule,
        TooltipModule,
        JsonViewerModule,
        IgColorPickerModule,
        PropertyGroupModule,
        TagViewModule,
        SharedObjectDetailsModule,
        TilesModule,
        TableModule,
        SharedGridPagingInfoModule,
        AdvancedFiltersModule,
		SearchFieldModule,
		LinkDisplayModule,
        DynamicFieldNameModule,
		PortalsModule
    ],
	declarations: [
    ],
	exports: [
		PropertyGroupComponent
    ],
    providers: []
})
export class TagDetailModule { }