import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { HTTP_INTERCEPTORS, HttpClientModule } from "@angular/common/http";

import { ButtonModule } from 'primeng/button';
import { PopupMenuModule } from '../../shared/controls/popup-menu/popup-menu.component';
import { DirectivesModule } from '../../../directives/directives.module';
import { SidePanelComponent } from "./side-panel.component";

@NgModule({
    imports: [        
        CommonModule,            
                
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
                
    ]
})
export class SidePanelModule { }