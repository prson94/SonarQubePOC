import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';

import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { IconPickerModule } from '../controls/icon-picker/icon-picker.component';

import { HelpMenuListComponent } from './helpmenu-list.component';

import { PopupMenuModule } from "../controls/popup-menu/popup-menu.component";

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { ColorPickerModule } from 'primeng/colorpicker';
import { TableModule } from 'primeng/table';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        CoreModule,
        TilesModule,
        SharedGridPagingInfoModule,
        IconPickerModule,
        PopupMenuModule,

        //prime
        ColorPickerModule,
        ButtonModule,
        SharedModule,
        TableModule,
    ],
    declarations: [
        HelpMenuListComponent
    ],
    exports: [
        HelpMenuListComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class HelpMenuModule { }