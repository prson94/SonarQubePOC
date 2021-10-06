import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { HTTP_INTERCEPTORS, HttpClientModule } from "@angular/common/http";
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { ButtonModule } from 'primeng/button';
import { PopupMenuModule } from '../../shared/controls/popup-menu/popup-menu.component';
import { DirectivesModule } from '../../../directives/directives.module';
import { SidePanelComponent } from "./side-panel.component";

@NgModule({
    imports: [        
        CommonModule,            
        HttpClientModule,                
        ButtonModule,
        PopupMenuModule,
        DirectivesModule,
    ],
    declarations: [
        SidePanelComponent,        
    ],
    exports: [
        SidePanelComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },        
    ]
})
export class SidePanelModule { }