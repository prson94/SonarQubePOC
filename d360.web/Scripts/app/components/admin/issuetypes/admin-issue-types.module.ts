import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
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

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedAuditModule } from '../../shared/audit/shared-audit.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';

import { AdminIssueTypesComponent } from './admin-issue-types.component';


import { AdminIssueTypesRoutingModule } from './admin-issue-types.routes';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,

        AdminIssueTypesRoutingModule,

        //prime
        ButtonModule,
        DataTableModule,
        DropdownModule,
        InputTextModule,
        SharedModule,

        //d3s                
        CoreModule,
        SharedAuditModule,
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,     
        SharedFieldDefinitionModule,   
        SharedGridPagingInfoModule,
        TilesModule,
    ],
    declarations: [
        AdminIssueTypesComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminIssueTypesModule { }