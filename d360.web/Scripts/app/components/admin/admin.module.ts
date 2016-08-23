///<reference path="../../es6-shim.d.ts"/>

import {  NgModule } from '@angular/core';
//import * as admin from './index'; 
import { routing } from './admin.routes';
//import * as primeng from 'primeng/primeng';
import { SharedModule } from '../shared/shared.module';
import { FormModule } from '../forms/forms.module';
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
        TreeTableModule,
        DataTableModule,
        InputTextModule,
        InputMaskModule,
        ButtonModule,
        EditorModule,
        DropdownModule,
        MultiSelectModule,
        SpinnerModule,
        CheckboxModule,
        SharedModule,
        FormModule,
        TilesModule,
        PartsModule,
        BrowserModule,
    ] 

})

export class AdminModule { }