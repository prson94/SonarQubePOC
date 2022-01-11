import { CommonModule } from "@angular/common";
import { HttpClientModule, HTTP_INTERCEPTORS } from "@angular/common/http";
import { NgModule } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { RouterModule } from "@angular/router";
import { TooltipModule } from "primeng/tooltip";
import { DirectivesModule } from "../../../directives/directives.module";

import { SiteModalModule } from "../../shared/modal/gov-modal.module";
import { ResourceApiComponent } from "./resource-api.component";

@NgModule({
    declarations: [
        ResourceApiComponent
    ],
    imports: [
        //angular
        CommonModule,
        FormsModule,

        RouterModule,
        DirectivesModule,

        SiteModalModule,
        TooltipModule
    ],
    exports: [
        ResourceApiComponent,
    ],
    providers: [
        
    ]
})

export class ResourceApiKeyModule { }