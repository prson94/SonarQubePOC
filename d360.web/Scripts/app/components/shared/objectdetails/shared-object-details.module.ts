import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { RouterModule }    from '@angular/router';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    ButtonModule,
    DataTableModule,
    InputTextModule,
    SharedModule,
    TooltipModule,
} from 'primeng/primeng';

import { CoreModule } from '../core.module';
import { SimpleAccordionModule } from '../simple-accordion.part';
import { TilesModule  } from '../tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';

import { DynamicLookupGridComponent } from './dynamic-lookup-grid.component';
import { ObjectDetailComponent } from './object-detail.component';
import { ObjectDetailField } from './object-detail-field.part';

@NgModule({
    imports: [CommonModule,
        RouterModule,
        HttpModule,
        //d3s
        CoreModule,
        SharedGridPagingInfoModule,
        SimpleAccordionModule,
        TilesModule,
        //prime
        ButtonModule,
        DataTableModule,
        InputTextModule,
        SharedModule,
        TooltipModule,
    ],
    declarations: [
        DynamicLookupGridComponent,
        ObjectDetailComponent,
        ObjectDetailField,
    ],
    exports: [
        ObjectDetailComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SharedObjectDetailsModule { }