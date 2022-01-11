import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';



import { RouterModule } from '@angular/router';

import { PipesModule } from '../../../pipes/pipes.module';

import { TooltipModule } from 'primeng/tooltip';
import { FocusTrapModule } from 'primeng/focustrap';
import { D3SModalDrawer } from './gov-modal-drawer.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,
        PipesModule,

        //prime
        TooltipModule,
        FocusTrapModule
    ],
    declarations: [
        D3SModalDrawer
    ],
    exports: [
        D3SModalDrawer,
    ],
    providers: [
        
    ]
})
export class ModalDrawerModule { }