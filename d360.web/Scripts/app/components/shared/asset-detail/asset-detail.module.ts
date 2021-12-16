import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { HTTP_INTERCEPTORS, } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { AssetDetailFieldComponent } from "./asset-detail-field.component";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { PipesModule } from "../../../pipes/pipes.module";
import { CoreModule } from "../core.module";
import { TooltipModule } from "primeng/tooltip";
import { NgxJsonViewModule } from "ng-json-view";
import { IgColorPickerModule } from "../controls/color-picker/color-picker.module";
import { TagViewModule } from "../tags/d3s-tag-view.module";
import { AssetDetailCategoryComponent } from "./asset-detail-category.component";
import { AssetDetailComponent } from "./asset-detail.component";
import { AssetLookupGridComponent } from "./asset-lookup-grid.component";
import { AssetLookupListComponent } from "./asset-lookup-list.component";
import { SharedObjectDetailsModule } from "../objectdetails/shared-object-details.module";
import { PropertyGroupModule } from "../controls/property-group/property-group.component";
import { TilesModule } from "../tiles/tiles.module";
import { TableModule } from "primeng/table";
import { SharedGridPagingInfoModule } from "../grid-paging-info.component";
import { OwnershipListModule } from "../small-widgets/ownership-list/ownership-list.component";
import { AdvancedFiltersModule } from "../../assets-grid/advanced-filtering/advanced-filtering.module";
import { SearchFieldModule } from "../controls/search-field/search-field.component";
import { ScoreBadgeModule } from "../small-widgets/score-badge/score-badge.module";
import { PeopleResponsibilitiesModule } from "../responsibilities/people-responsibilities.tile";
import { DynamicFieldNameModule } from "../dynamic-field-name.component";
import { PortalsModule } from "../portals/portals.module";
import { SharedAssignmentsModule } from "../assignments/shared-assignments.module";


@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        CoreModule,
        PipesModule,
        TooltipModule,
        NgxJsonViewModule,
        IgColorPickerModule,
        PropertyGroupModule,
        TagViewModule,
        SharedObjectDetailsModule,
        TilesModule,
        TableModule,
        SharedGridPagingInfoModule,
        OwnershipListModule,
        AdvancedFiltersModule,
        SearchFieldModule,
        ScoreBadgeModule,
        PeopleResponsibilitiesModule,
        DynamicFieldNameModule,
        PortalsModule,
        SharedAssignmentsModule
    ],
    declarations: [
        AssetDetailFieldComponent,
        AssetDetailCategoryComponent,
        AssetDetailComponent,
        AssetLookupGridComponent,
        AssetLookupListComponent,
    ],
    exports: [
        AssetDetailFieldComponent,
        AssetDetailCategoryComponent,
        AssetDetailComponent,
        AssetLookupGridComponent,
        AssetLookupListComponent,
    ],
    providers: [{
        provide: HTTP_INTERCEPTORS,
        useClass: GovernRequestInterceptor,
        multi: true
    }]
})
export class AssetDetailModule { }