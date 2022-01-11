import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { HTTP_INTERCEPTORS } from "@angular/common/http";

import { TreeTableModule } from "primeng/treetable";
import { TooltipModule } from 'primeng/tooltip';
import { CoreModule } from "../../shared/core.module";
import { AdminSearchComponent } from "./admin-search.component";
import { AdminSearchRoutingModule } from "./admin-search.routes";

@NgModule({
    imports: [
        CommonModule,
        AdminSearchRoutingModule,
        //primeng
        TreeTableModule,
        TooltipModule,
        //d3s                
        CoreModule,
    ],
    declarations: [
        AdminSearchComponent,
    ],
    providers: [
    ]

})
export class AdminSearchModule { }