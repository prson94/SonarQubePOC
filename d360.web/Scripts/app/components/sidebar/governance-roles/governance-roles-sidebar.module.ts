import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { RouterModule } from '@angular/router';

import { SharedModule } from 'primeng/api';
import { TableModule } from 'primeng/table';

import { CoreModule } from '../../shared/core.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { GovernanceRolesComponent } from './governance-roles-sidebar.component';
import { GovernanceRolesRoutingModule } from './governance-roles-sidebar.routes';
import { EditorModule } from 'primeng/editor';
import { DropdownModule } from 'primeng/dropdown';
import { ButtonModule } from 'primeng/button';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //routing 
        GovernanceRolesRoutingModule,

        //d3s        
        CoreModule,
        SharedGridPagingInfoModule,
        TilesModule,

        //prime     
        EditorModule,
        DropdownModule,
        ButtonModule,
        SharedModule,
        TableModule,
    ],
    declarations: [
        GovernanceRolesComponent,
    ],
    providers: [

    ]
})
export class GovernanceRolesModule { }