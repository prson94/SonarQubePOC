import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';

import { GroupRoutingModule } from './group.routes';

import { GroupComponent } from './group.component';
import { GroupItemComponent } from './group-item.component';
import { GroupListComponent } from './group-list.component';
import { GroupResponsibilityComponent } from './group-responsibility.component';

import { ToastModule } from 'primeng/toast';
import { SharedModule } from 'primeng/api';
import { TableModule } from 'primeng/table';
import { GroupMembersModule } from '../shared/group/group-members.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        GroupRoutingModule,

        //primeng
        ToastModule,        
        SharedModule,
        TableModule,

        //d3s
        D3SSharedModule,        
        CoreModule,
        PipesModule,
        TilesModule,
        SharedGridPagingInfoModule,
        GroupMembersModule
    ],
    declarations: [
        GroupComponent,
        GroupItemComponent,
        GroupListComponent,
        GroupResponsibilityComponent,
    ],
    providers: [

    ]
})
export class GroupModule { }