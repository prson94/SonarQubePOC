import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';


import { ButtonModule } from 'primeng/button';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { ActionsRoutingModule } from './actions.routes';
import { ActionsComponent } from './actions.component';
import { WorkflowModule } from '../../workflow/workflow.module';
import { GovernRequestInterceptor } from '../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,

        //routing 
        ActionsRoutingModule,

        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        WorkflowModule,
    ],
    declarations: [
        ActionsComponent
    ],
    providers: [
        
    ]
})
export class ActionsModule { }