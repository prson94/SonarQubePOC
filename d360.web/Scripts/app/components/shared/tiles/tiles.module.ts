import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';

import { TileActionsComponent } from './tile-actions.component';

import { MenubarModule } from 'primeng/menubar';
import { TooltipModule } from 'primeng/tooltip';

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