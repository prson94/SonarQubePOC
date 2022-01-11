import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { RouterModule } from '@angular/router';

import { SharedModule } from 'primeng/api';
import { TableModule } from 'primeng/table';
import { TooltipModule } from 'primeng/tooltip';

import { CoreModule } from '../../shared/core.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { AdvancedFiltersModule } from "../../assets-grid/advanced-filtering/advanced-filtering.module";

import { AuditRoutingModule } from './audit.routes';

import { AuditComponent } from './audit.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //routing 
        AuditRoutingModule,

        //d3s        
        CoreModule,
        SharedGridPagingInfoModule,
        TilesModule,
        AdvancedFiltersModule,

        //prime        
        SharedModule,
        TableModule,
        TooltipModule,
    ],
    declarations: [
        AuditComponent,
    ],
    providers: [

    ]
})
export class AuditModule { }