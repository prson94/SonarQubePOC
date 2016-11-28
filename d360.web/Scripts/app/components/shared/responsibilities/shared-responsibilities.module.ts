import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    ButtonModule,
    DataTableModule,
    DropdownModule,
    InputTextModule,
    EditorModule,
    MultiSelectModule,
    SharedModule,
} from 'primeng/primeng';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';
import { SharedDeleteFormModule } from '../delete.form';
import { SharedFormMessageModule } from '../form-message.part';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';

import { PeopleResponsibilitiesTile } from './people-responsibilities.tile';
import { ResponsibilityItemForm } from './responsibility-item.form';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        //d3s
        CoreModule,
        TilesModule,
        SharedDeleteFormModule,
        SharedFormMessageModule,
        SharedGridPagingInfoModule,

        //prime
        ButtonModule,
        DataTableModule,
        DropdownModule,
        InputTextModule,
        EditorModule,
        MultiSelectModule,
        SharedModule,
    ],
    declarations: [
        ResponsibilityItemForm,
        PeopleResponsibilitiesTile
    ],
    exports: [
        PeopleResponsibilitiesTile
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SharedResponsibilitiesModule { }