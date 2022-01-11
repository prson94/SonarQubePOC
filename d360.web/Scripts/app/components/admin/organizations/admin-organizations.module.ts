import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedAssetTypeEditorModule } from '../../shared/assettypeeditor/shared-asset-type-editor.module';

import { AdminModule } from '../admin.module';

import { AdminOrganizationTypesComponent } from './admin-organization-types.component';
import { AdminOrganizationsComponent } from './admin-organizations.component';
import { AdminOrganizationContractsComponent } from './admin-organization-contracts.component';
import { AdminOrganizationContractEditorComponent } from './admin-organization-contract-editor.component';
import { AdminOrganizationDomainsComponent } from './admin-organization-domains.component';
import { AdminOrganizationInvitationsComponent } from './admin-organization-invitations.component';
import { AdminOrganizationResourcesComponent } from './admin-organization-resources.component';
import { AdminContractsComponent } from './admin-contracts.component';
import { AdminOrganizationListComponent } from "./admin-organization-list.component";
import { AdminOrganizationContractHistoryComponent } from "./admin-organization-contract-history.component";

import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';

import { AdminOrganizationsRoutingModule } from './admin-organizations.routes';

import { SharedModule } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { EditorModule } from 'primeng/editor';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,


        AdminOrganizationsRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        EditorModule,
        TableModule,

        //d3s       
        AdminModule,
        CoreModule,
        PipesModule,

        SharedFieldDefinitionModule,
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedDynamicGridEditorModule,
        SharedAssetTypeEditorModule,
        TilesModule,
    ],
    declarations: [
        AdminOrganizationTypesComponent,
        AdminOrganizationListComponent,
        AdminOrganizationsComponent,     
        AdminOrganizationContractsComponent,
        AdminOrganizationContractEditorComponent,
        AdminOrganizationDomainsComponent,
        AdminOrganizationInvitationsComponent,
        AdminOrganizationResourcesComponent,
        AdminContractsComponent,
        AdminOrganizationContractHistoryComponent
    ],
    providers: [
    ]
})
export class AdminOrganizationsModule { }