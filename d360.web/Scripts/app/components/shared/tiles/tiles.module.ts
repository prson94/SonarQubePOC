import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';

import { TileActionsComponent } from './tile-actions.component';

import { MenubarModule } from 'primeng/menubar';
import { TooltipModule } from 'primeng/tooltip';
import { DataCyModule } from '../../../directives/ig-data-cy.directive';

@NgModule({
    imports: [
        CommonModule,
        DataCyModule,
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