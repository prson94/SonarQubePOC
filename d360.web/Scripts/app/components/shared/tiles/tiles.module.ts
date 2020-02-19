import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';

import { TileActionsComponent } from './tile-actions.component';

import { MenubarModule } from 'primeng/menubar';
import { TooltipModule } from 'primeng/tooltip';

@NgModule({
    imports: [
        CommonModule,
        //prime
        MenubarModule,
        TooltipModule,
    ],
    declarations: [
        TileActionsComponent
    ],
    exports: [
        TileActionsComponent
    ]
})
export class TilesModule { }