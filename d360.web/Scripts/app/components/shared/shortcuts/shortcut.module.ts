import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';



import { RouterModule } from '@angular/router';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';

import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { IconPickerModule } from '../controls/icon-picker/icon-picker.component';

import { ShortcutListComponent } from './shortcut-list.component';
import { ShortcutItemComponent } from './shortcut-item.component';

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { ColorPickerModule } from 'primeng/colorpicker';
import { TableModule } from 'primeng/table';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

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
    ],
    exports: [
        ShortcutItemComponent,
        ShortcutListComponent,        
    ],
    providers: [

    ]
})
export class ShortcutModule { }