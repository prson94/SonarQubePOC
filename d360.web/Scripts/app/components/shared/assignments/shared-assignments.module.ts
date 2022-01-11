import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';



import { SharedModule } from 'primeng/api';
import { TableModule } from 'primeng/table';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';

import { AssignmentsComponent } from './assignments.component';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { SearchFieldModule } from '../controls/search-field/search-field.component';


@NgModule({
    imports: [
        CommonModule,


        //d3s
        CoreModule,        
        TilesModule,
        SharedGridPagingInfoModule,

        //prime        
        SharedModule,
        TableModule,
        SearchFieldModule
    ],
    declarations: [
        AssignmentsComponent
    ],
    exports: [
        AssignmentsComponent
    ],
    providers: [

    ]
})
export class SharedAssignmentsModule { }