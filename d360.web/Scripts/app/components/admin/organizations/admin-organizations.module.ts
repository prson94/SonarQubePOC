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
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedAuditModule } from '../../shared/audit/shared-audit.module';
import { AdminModule } from '../admin.module';

import { AdminOrganizationsComponent } from './admin-organizations.component';
import { AdminOrganizationContractsComponent } from './admin-organization-contracts.component';
import { AdminOrganizationDomainsComponent } from './admin-organization-domains.component';
import { AdminOrganizationInvitationsComponent } from './admin-organization-invitations.component';
import { AdminOrganizationResourcesComponent } from './admin-organization-resources.component';
import { AdminContractsComponent } from './admin-contracts.component';

import { AdminOrganizationsRoutingModule } from './admin-organizations.routes';

import {
    ButtonModule,
    DropdownModule,
    InputTextModule,
    SharedModule,
    DataTableModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,

        AdminOrganizationsRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        DataTableModule,

        //d3s       
        AdminModule,
        CoreModule,
        PipesModule,
        SharedAuditModule,
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedDynamicGridEditorModule,
        TilesModule,
    ],
    declarations: [
        AdminOrganizationsComponent,     
        AdminOrganizationContractsComponent,
        AdminOrganizationDomainsComponent,
        AdminOrganizationInvitationsComponent,
        AdminOrganizationResourcesComponent,
        AdminContractsComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminOrganizationsModule { }