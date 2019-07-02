import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';

import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { IconPickerModule } from '../icon-picker.component';

import { ShortcutListComponent } from './shortcut-list.component';
import { ShortcutItemComponent } from './shortcut-item.component';
import { ShortcutDisplayComponent } from './shortcut-display.component';


import {
    ColorPickerModule,
    ButtonModule,
    SharedModule

} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        CoreModule,
        TilesModule,
        SharedGridPagingInfoModule,
        IconPickerModule,

        //prime
        ColorPickerModule,
        ButtonModule,
        SharedModule,
        TableModule,
    ],
    declarations: [
        ShortcutItemComponent,
        ShortcutListComponent,
        ShortcutDisplayComponent,
    ],
    exports: [
        ShortcutItemComponent,
        ShortcutListComponent,
        ShortcutDisplayComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class ShortcutModule { }