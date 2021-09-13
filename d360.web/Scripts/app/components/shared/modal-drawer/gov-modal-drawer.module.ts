import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { PipesModule } from '../../../pipes/pipes.module';

import { TooltipModule } from 'primeng/tooltip';
import { FocusTrapModule } from 'primeng/focustrap';
import { D3SModalDrawer } from './gov-modal-drawer.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
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
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class ModalDrawerModule { }