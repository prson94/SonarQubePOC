import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { AdminModule } from '../admin.module';
import { SharedAuditModule } from '../../shared/audit/shared-audit.module';

import { AdminGovernanceComponent } from './admin-governance.component';
import { ResponsibilityTypeForm } from './responsibility-type.form';

import { AdminResponsibilitiesRoutingModule } from './admin-responsibilities.routes';

import {
    ButtonModule,
    DropdownModule,
    EditorModule,
    InputTextModule,
    MultiSelectModule,
    SharedModule,
    DataTableModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,

        AdminResponsibilitiesRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        EditorModule,
        InputTextModule,
        MultiSelectModule,
        SharedModule,
        DataTableModule,

        //d3s        
        CoreModule,
        PipesModule,
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedObjectDetailsModule,
        SharedResponsibilitiesModule,
        SharedAuditModule,
        TilesModule,
        AdminModule,
    ],
    declarations: [
        AdminGovernanceComponent,
        ResponsibilityTypeForm,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminResponsibilitiesModule { }