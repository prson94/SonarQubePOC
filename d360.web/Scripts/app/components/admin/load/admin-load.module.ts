import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';


import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';

import { BulkLoadItemComponent } from './bulk-load-item.component';
import { LoadForm } from './load.form';
import { AdminLoadComponent } from './admin-load.component';

import { AdminLoadRoutingModule } from './admin-load.routes';

import { SharedModule } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { InputTextModule } from 'primeng/inputtext';
import { SearchFieldModule } from "../../shared/controls/search-field/search-field.component";

import { TableModule } from 'primeng/table';
import { SiteModalModule } from '../../shared/modal/gov-modal.module';
import { IgMessageBoxModule } from '../../shared/controls/message-box/message-box.module';
import { DirectivesModule } from '../../../directives/directives.module';
import { CheckboxModule } from 'primeng/checkbox';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,


        AdminLoadRoutingModule,

        SearchFieldModule,

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
		TableModule,
		CheckboxModule,

        //d3s        
        CoreModule,
        PipesModule,        
        SharedDeleteFormModule,                
        SharedGridPagingInfoModule,
        SharedObjectDetailsModule,
		TilesModule,

		SiteModalModule,
		IgMessageBoxModule,
		DirectivesModule
    ],
    declarations: [
        BulkLoadItemComponent,
        LoadForm,
        AdminLoadComponent,
    ],
    providers: [
    ]
})
export class AdminLoadModule { }