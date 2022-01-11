import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';

import { CommunityComponent } from './community.component';
import { CommunityResponsibilityCountComponent } from './community-responsibility-count.component';

import { CommunityRoutingModule } from './community.routes';

import { ToastModule } from 'primeng/toast';
import { SharedModule } from 'primeng/api';
import { TableModule } from 'primeng/table';



@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        CommunityRoutingModule,


        //prime
        SharedModule,
        ToastModule,
        TableModule,

        //d3s
        D3SSharedModule,
        CoreModule,
        PipesModule,
        SharedGridPagingInfoModule,
    ],
    declarations: [
        CommunityComponent,
        CommunityResponsibilityCountComponent,
    ],
    providers: [
                
    ]
})
export class CommunityModule { }