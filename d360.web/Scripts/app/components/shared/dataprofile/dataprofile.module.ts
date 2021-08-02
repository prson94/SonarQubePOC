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


@NgModule({
    imports: [        
        CommonModule,
        HttpClientModule,        
        PropertyGroupModule,
        DirectivesModule,
        TooltipModule,
        PipesModule,
        SiteModalModule
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