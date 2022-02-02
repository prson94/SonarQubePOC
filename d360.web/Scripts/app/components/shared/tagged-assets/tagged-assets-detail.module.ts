import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { HTTP_INTERCEPTORS, } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { PipesModule } from "../../../pipes/pipes.module";
import { CoreModule } from "../core.module";
import { TooltipModule } from "primeng/tooltip";
import { NgxJsonViewModule } from "ng-json-view";
import { IgColorPickerModule } from "../controls/color-picker/color-picker.module";
import { TagViewModule } from "../tags/d3s-tag-view.module";
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
import { ReferenceModule } from "../../reference/reference.module";
import { TaggedAssetDetailComponent } from "./tagged-assets-detail.component";
import { DirectivesModule } from "../../../directives/directives.module";


@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        CoreModule,
        PipesModule,
        TooltipModule,
        DirectivesModule,
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
        ReferenceModule
    ],
    declarations: [
        TaggedAssetDetailComponent
    ],
    exports: [
        TaggedAssetDetailComponent
    ],
    providers: [{
        provide: HTTP_INTERCEPTORS,
        useClass: GovernRequestInterceptor,
        multi: true
    }]
})
export class TaggedAssetDetailModule { }