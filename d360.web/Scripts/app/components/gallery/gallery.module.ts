import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from '../../http-interceptors/govern-request.interceptor';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { IconPickerModule } from '../shared/controls/icon-picker/icon-picker.component';
import { SwitchModule } from '../shared/controls/switch/switch'
import { CoreModule } from '../shared/core.module';


import { GalleryComponent } from './gallery.component';
import { GalleryRoutingModule } from './gallery.routes';

import { GallerySwitchComponent } from './gallery.switch.component';
import { GalleryButtonComponent } from './gallery.button.component';
import { GalleryIconPickerComponent } from './gallery.icon-picker.component';
import { GalleryTagPickerComponent } from './gallery.tag-picker.component';
import { GalleryGuard } from '../../guards/gallery.guard';

import { TableModule } from 'primeng/table';
import { TagPickerModule } from '../shared/controls/tag-picker/tag-picker';
import { GalleryInputComponent } from './gallery.input.component';
import { GalleryColorPickerComponent } from './gallery.color-picker.component';
import { IgColorPickerModule } from '../shared/controls/color-picker/color-picker.module';
import { GalleryColorVariablesComponent } from './gallery.color-variables.component';
import { GalleryTooltipComponent } from './gallery.tooltip.component';
import { TooltipModule } from 'primeng/tooltip';
import { GalleryTextAreaComponent } from './gallery.textarea.component';

@NgModule({
    imports: [
        CommonModule,
        HttpClientModule,
        RouterModule,
        CoreModule,
        FormsModule,

        GalleryRoutingModule,

        SwitchModule,
        IconPickerModule,
        TagPickerModule,
        IgColorPickerModule,

        TableModule,
        TooltipModule

    ],
    declarations: [
        GalleryComponent,
        GallerySwitchComponent,
        GalleryButtonComponent,
        GalleryIconPickerComponent,
        GalleryTagPickerComponent,
        GalleryInputComponent,
        GalleryColorPickerComponent,
        GalleryColorVariablesComponent,
        GalleryTooltipComponent,
        GalleryTextAreaComponent,
    ],
    providers: [
        GalleryGuard,
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class GalleryModule { }