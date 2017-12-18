import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';


import { TileActionsComponent } from './tile-actions.component';


import {
    MenubarModule,
    TooltipModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
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