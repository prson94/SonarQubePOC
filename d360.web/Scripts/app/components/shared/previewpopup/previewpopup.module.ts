import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { CoreModule } from '../core.module';
import { RouterModule } from '@angular/router';
import { NgxJsonViewModule } from 'ng-json-view';

import { PreviewPopupComponent } from './preview-popup.component';
import { DirectivesModule } from '../../../directives/directives.module';
import { IgColorPickerModule } from '../controls/color-picker/color-picker.module';

@NgModule({
    imports: [
        CommonModule,
        DirectivesModule,
        RouterModule,
        DialogModule,
        CoreModule,
        NgxJsonViewModule,
        IgColorPickerModule,
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