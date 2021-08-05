import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { HTTP_INTERCEPTORS, HttpClientModule } from "@angular/common/http";
import { DataProfileComponent } from "./dataprofile.component";
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
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


@NgModule({
    imports: [        
        CommonModule,
        FormsModule,
        HttpClientModule,        
        PropertyGroupModule,
        DirectivesModule,
        TooltipModule,
        PipesModule,
        SiteModalModule,
        TableModule,
        SharedGridPagingInfoModule,
        PopupMenuModule,
        SearchFieldModule
    ],
    declarations: [
        DataProfileComponent,
        MatchDetectionComponent
    ],
    exports: [
        DataProfileComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },        
    ]
})
export class DataProfileModule { }