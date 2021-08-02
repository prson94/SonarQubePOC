import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { HTTP_INTERCEPTORS, HttpClientModule } from "@angular/common/http";
import { DataProfileComponent } from "./dataprofile.component";
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { PropertyGroupModule } from "../controls/property-group/property-group.component";
import { DirectivesModule } from '../../../directives/directives.module';
import { TooltipModule } from 'primeng/tooltip';
import { PipesModule } from '../../../pipes/pipes.module';


@NgModule({
    imports: [        
        CommonModule,
        HttpClientModule,        
        PropertyGroupModule,
        DirectivesModule,
        TooltipModule,
        PipesModule,
    ],
    declarations: [
        DataProfileComponent,        
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