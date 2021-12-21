import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { RouterModule } from '@angular/router';

import { D3SModal } from './gov-modal.component';
import { PipesModule } from '../../../pipes/pipes.module';

import { TooltipModule } from 'primeng/tooltip';
import { FocusTrapModule } from 'primeng/focustrap';

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
        D3SModal
    ],
    exports: [
        D3SModal,        
    ],
    providers: [

    ]
})
export class SiteModalModule { }