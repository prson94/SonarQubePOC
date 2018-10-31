import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    ButtonModule,
    DataTableModule,
    DropdownModule,
    InputTextModule,
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';

import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';

import { AdminIssueTypesComponent } from './admin-issue-types.component';
import { AdminIssueTypeAllocationComponent } from './admin-issue-type-allocation.component';


import { AdminIssueTypesRoutingModule } from './admin-issue-types.routes';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,

        AdminIssueTypesRoutingModule,

        //prime
        ButtonModule,
        DataTableModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        TableModule,

        //d3s                
        CoreModule,
        
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,     
        SharedFieldDefinitionModule,   
        SharedGridPagingInfoModule,
        TilesModule,
    ],
    declarations: [
        AdminIssueTypesComponent,
        AdminIssueTypeAllocationComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminIssueTypesModule { }