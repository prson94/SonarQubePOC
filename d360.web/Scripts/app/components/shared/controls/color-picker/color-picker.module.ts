import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { ColorPickerComponent } from './color-picker.component';
import { DropdownModule } from 'primeng/dropdown';
import { ColorDisplayComponent } from './color-display.component';
import { DirectivesModule } from '../../../../directives/directives.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule, 
        DropdownModule,
        DirectivesModule

    ],
    declarations: [
        ColorPickerComponent,
        ColorDisplayComponent,       
    ],
    exports: [
        ColorPickerComponent,
        ColorDisplayComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class IgColorPickerModule { }