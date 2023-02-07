import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';


import { SharedModule } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { InputTextModule } from 'primeng/inputtext';
import { MultiSelectModule } from "primeng/multiselect";

import { TableModule } from 'primeng/table';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';

import { AdminIssueTypesComponent } from './admin-issue-types.component';
import { AdminIssueTypeAllocationComponent } from './admin-issue-type-allocation.component';
import { AdminIssueTypeAllocationEditorComponent } from "./admin-issue-type-allocation-editor.component";

import { AdminIssueTypesRoutingModule } from './admin-issue-types.routes';
import { SidePanelModule } from '../../shared/sidepanel/side-panel.module';
import { AngularSplitModule } from 'angular-split';
import { AssetPreviewModule } from '../../shared/asset-preview/asset-preview.module';
import { IssueTypeSidePanelWrapperComponent } from './issuetypes-sidepanel-wrapper/issuetype-sidepanel-wrapper.component';
import { ConfigurationIssueTypeFieldsPageComponent } from './tabs/fields/configuration-issue-type-fields-page.component';
import { ConfigurationIssueTypeHeaderComponent } from './tabs/shared/issue-type-header/issue-type-header.component';
import { PageHeaderModule } from '../../shared/page-header/page-header.module';
import { PortalsModule } from '../../shared/portals/portals.module';
import { TabsModule } from '../../shared/tabs/tabs.module';
import { ConfigurationIssueTypeAllocationsPageComponent } from './tabs/allocations/configuration-issue-type-allocations-page.component';
import { AuditModule } from '../../sidebar/audit/audit.module';
import { ConfigurationIssueTypeLogPageComponent } from './tabs/log/configuration-issue-type-log-page.component';
import { SearchFieldModule } from '../../shared/controls/search-field/search-field.component';
import { IssueTypeDefinitionModule } from './definition/issue-type-definition.module';
import { TooltipModule } from 'primeng/tooltip';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        AdminIssueTypesRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        TableModule,
		MultiSelectModule,

        //d3s                
        CoreModule,
        
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,     
        SharedFieldDefinitionModule,   
        SharedGridPagingInfoModule,
		TilesModule,
		AngularSplitModule,
		AssetPreviewModule,
		SidePanelModule,
		PageHeaderModule,
		PortalsModule,
		TabsModule,
		AuditModule,
		SearchFieldModule,
		IssueTypeDefinitionModule,
		TooltipModule
    ],
    declarations: [
        AdminIssueTypesComponent,
        AdminIssueTypeAllocationComponent,
		AdminIssueTypeAllocationEditorComponent,
		IssueTypeSidePanelWrapperComponent,
		ConfigurationIssueTypeFieldsPageComponent,
		ConfigurationIssueTypeLogPageComponent,
		ConfigurationIssueTypeAllocationsPageComponent,
		ConfigurationIssueTypeHeaderComponent
    ],
    providers: [
    ]
})
export class AdminIssueTypesModule { }