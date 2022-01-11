import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';



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
import { TooltipModule } from 'primeng/tooltip';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        CoreModule,
        TilesModule,
        SharedGridPagingInfoModule,
        IconPickerModule,
        PopupMenuModule,
        TooltipModule,

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
        
    ]
})
export class HelpMenuModule { }