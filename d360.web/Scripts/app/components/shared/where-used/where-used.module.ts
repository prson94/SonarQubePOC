import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { WhereUsedComponent } from './where-used.component';
import { TableModule } from 'primeng/table';
import { D3SSortIconModule } from '../turbotable-sorticon.component';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { SiteModalModule } from '../modal/gov-modal.module';
import { DirectivesModule } from '../../../directives/directives.module';

@NgModule({
    imports: [
        CommonModule,
        TableModule,
        D3SSortIconModule,
        SharedGridPagingInfoModule,
        SiteModalModule,
        DirectivesModule
    ],
    declarations: [
        WhereUsedComponent
    ],
    exports: [
        WhereUsedComponent
    ]
})
export class WhereUsedModule { }