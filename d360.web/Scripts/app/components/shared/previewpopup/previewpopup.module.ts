import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { CoreModule } from '../core.module';
import { RouterModule } from '@angular/router';
import { NgxJsonViewModule } from 'ng-json-view';

import { PreviewPopupComponent } from './preview-popup.component';
import { DirectivesModule } from '../../../directives/directives.module';
import { D3SColorPickerModule } from '../small-widgets/color-picker/color-picker.module';

@NgModule({
    imports: [
        CommonModule,
        DirectivesModule,
        RouterModule,
        DialogModule,
        CoreModule,
        NgxJsonViewModule,
        D3SColorPickerModule,
    ],
    declarations: [
        PreviewPopupComponent
    ],
    exports: [
        PreviewPopupComponent
    ],
    providers: [

    ]
})
export class PreviewpopupModule { }