import {  NgModule } from '@angular/core';
import { routing } from './admin.routes';
import { D3SSharedModule } from '../shared/shared.module';
import { D3SFormsModule } from '../forms/d3sforms.module';
import { TilesModule } from '../tiles/tiles.module';
import { PartsModule } from '../parts/parts.module';

import { BrowserModule } from '@angular/platform-browser';

import {
    TreeTableModule,
    DataTableModule,
    InputTextModule,
    InputMaskModule,
    ButtonModule,
    EditorModule,
    SharedModule,
    DropdownModule,
    MultiSelectModule,
    SpinnerModule,
    CheckboxModule,    
} from 'primeng/primeng';

import {
    AdminArtifactsComponent,
    AdminAttributesComponent,
    AdminComponent,
    AdminDashboardsComponent,
    AdminDomainComponent,
    AdminFusionComponent,
    AdminGovernanceComponent,
    AdminGroupsComponent,
    AdminLoadComponent,
    AdminLookupsComponent,
    AdminPoliciesComponent,
    AdminRelationshipsComponent,
    AdminResourcesComponent,
    AdminRulesComponent,
    AdminSettingsComponent,
    AdminStatisticsComponent,
    AdminSurveysComponent,
    AdminTaxonomiesComponent,
    AdminTemplatesComponent,
    AdminWorkflowComponent
} from './index';

@NgModule({
    declarations: [
        AdminArtifactsComponent,
        AdminAttributesComponent,
        AdminComponent,
        AdminDashboardsComponent,
        AdminDomainComponent,
        AdminFusionComponent,
        AdminGovernanceComponent,
        AdminGroupsComponent,
        AdminLoadComponent,
        AdminLookupsComponent,
        AdminPoliciesComponent,
        AdminRelationshipsComponent,
        AdminResourcesComponent,
        AdminRulesComponent,
        AdminSettingsComponent,
        AdminStatisticsComponent,
        AdminSurveysComponent,
        AdminTaxonomiesComponent, 
        AdminTemplatesComponent,
        AdminWorkflowComponent
    ]
    , imports: [
        routing,

        //prime
        TreeTableModule,
        DataTableModule,
        InputTextModule,
        InputMaskModule,
        ButtonModule,
        EditorModule,
        DropdownModule,
        MultiSelectModule,
        SpinnerModule,
        SharedModule,
        CheckboxModule,

        //d3s
        D3SSharedModule,
        D3SFormsModule,
        TilesModule,
        PartsModule,
        BrowserModule,
    ] 

})

export class AdminModule { }