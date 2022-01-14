import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule } from "@angular/router";
import { ScoreCalculationComponent } from "./score-calculation.component";
import { FormsModule } from "@angular/forms";
import { HttpClientModule, HTTP_INTERCEPTORS } from "@angular/common/http";
import { DirectivesModule } from "../../../../directives/directives.module";

import { MeasureRuleResultsComponent } from "./measure-rule-results.component";
import { SiteModalModule } from "../../modal/gov-modal.module";
import { CoreModule } from "../../core.module";
import { TableModule } from "primeng/table";
import { SharedGridPagingInfoModule } from "../../grid-paging-info.component";
import { SearchFieldModule } from "../../controls/search-field/search-field.component";
import { PipesModule } from "../../../../pipes/pipes.module";

@NgModule({
    imports: [
        FormsModule,
        CommonModule,
        RouterModule,

        DirectivesModule,
        CoreModule,
        PipesModule,
        SiteModalModule,
        TableModule,
        SharedGridPagingInfoModule,
        SearchFieldModule
    ],
    declarations: [
        ScoreCalculationComponent,
        MeasureRuleResultsComponent
    ],
    exports: [
        ScoreCalculationComponent,
        MeasureRuleResultsComponent
    ],
    providers: [
        
    ]
})
export class ScoreCalculationModule { }
