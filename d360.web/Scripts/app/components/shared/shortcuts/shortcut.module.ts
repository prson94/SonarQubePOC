import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';
import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { IconPickerModule } from '../icon-picker.component';

import { ShortcutListComponent } from './shortcut-list.component';
import { ShortcutItemComponent } from './shortcut-item.component';
import { ShortcutDisplayComponent } from './shortcut-display.component';


import {
    DataTableModule,
    ButtonModule,
    SharedModule

} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        CoreModule,
        TilesModule,
        SharedGridPagingInfoModule,
        IconPickerModule,

        //prime
        DataTableModule,
        ButtonModule,
        SharedModule,
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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class ShortcutModule { }