import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule, ReactiveFormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';


import {
    GrowlModule,
    InputTextModule,
    InputMaskModule,
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    CalendarModule,
    MenuModule,
    MenubarModule,
    AccordionModule,
    SelectButtonModule,
    AutoCompleteModule,
    MultiSelectModule,
    SpinnerModule,
    EditorModule,
    TooltipModule,
    DragDropModule,
    PaginatorModule,
} from 'primeng/primeng';

import { ChartModule } from 'angular2-highcharts';

import { PipesModule } from '../../pipes/pipes.module';
import { D3SFormsModule } from '../forms/d3sforms.module';

import { AuditComponent } from './audit.component';
import { DashboardTabComponent } from './dashboard-tab.component';
import { DeleteForm } from './delete.form';
import { DynamicFieldComponent } from './dynamic-field.component';
import { DynamicGridComponent } from './dynamic-grid.component';
import { DynamicEditorComponent } from './dynamic-editor.component';
import { DynamicLookupGridComponent } from './dynamic-lookup-grid.component';
import { DynamicRelationshipGridComponent } from './dynamic-relationship-grid.component';
import { FollowerGridComponent } from './follower-grid.component';
import { FusionAttributeItemDetailsComponent } from './fusion-attribute-item-details.component';
import { FusionFiltersComponent } from './fusion-filters.component';
import { GroupMembersComponent } from './group-members.component';
import { LineageComponent } from './lineage.component';
import { LoadingComponent } from './loading.component';
import { MessagesComponent } from './messages.component';
import { MessagesBarComponent } from './messages-bar.component';
import { ObjectBoardComponent } from './object-board.component';
import { ObjectChallengeComponent } from './object-challenge.component';
import { ObjectFollowersComponent } from './object-followers.component';
import { ObjectHealthComponent } from './object-health.component';
import { ObjectHealthDetailsComponent } from './object-health-details.component';
import { ObjectIssuesComponent } from './object-issues.component';
import { ObjectRelationshipsComponent } from './object-relationships.component';
import { PageLinksComponent } from './page-links.component';
import { PeopleResponsibilitiesTile } from './people-responsibilities.tile';
import { PowerBIViewerComponent } from './powerbi-viewer.component';
import { RaiseIssueButtonComponent } from './raise-issue-button.component';
import { TagInputComponent } from './tag-input.component';
import { RelationshipTechnicalRelationsComponent } from './relationship-technical-relations.component';
import { ResourceResponsibilityGridComponent } from './resource-responsibility-grid.component';
import { TileActionsComponent } from './tile-actions.component';
import { TooltipComponent } from './tooltip.component';
import { TakeSurveyComponent } from './take-survey.component';


@NgModule({
    declarations: [
        AuditComponent,
        DashboardTabComponent,
        DeleteForm,
        DynamicEditorComponent,
        DynamicFieldComponent,
        DynamicGridComponent,
        DynamicLookupGridComponent,
        DynamicRelationshipGridComponent,
        DashboardTabComponent,
        FollowerGridComponent,
        FusionAttributeItemDetailsComponent,        
        FusionFiltersComponent,
        GroupMembersComponent,
        LineageComponent,
        LoadingComponent,
        MessagesBarComponent,        
        MessagesComponent,        
        ObjectBoardComponent,
        ObjectChallengeComponent,
        ObjectFollowersComponent,
        ObjectHealthComponent,
        ObjectHealthDetailsComponent,
        ObjectIssuesComponent,       
        ObjectRelationshipsComponent, 
        PageLinksComponent,
        PeopleResponsibilitiesTile,
        PowerBIViewerComponent,
        RaiseIssueButtonComponent,
        RelationshipTechnicalRelationsComponent,
        ResourceResponsibilityGridComponent,
        TagInputComponent,
        TakeSurveyComponent,
        TileActionsComponent,
        TooltipComponent,
        
    ],
    exports: [
        AuditComponent,
        DashboardTabComponent,
        DeleteForm,
        DynamicEditorComponent,
        DynamicFieldComponent,
        DynamicGridComponent,
        DynamicLookupGridComponent,
        DynamicRelationshipGridComponent,
        DashboardTabComponent,
        FollowerGridComponent, 
        FusionAttributeItemDetailsComponent,      
        FusionFiltersComponent, 
        GroupMembersComponent,
        LineageComponent,
        LoadingComponent,
        MessagesBarComponent,        
        MessagesComponent,
        ObjectBoardComponent,
        ObjectChallengeComponent,
        ObjectFollowersComponent,
        ObjectHealthComponent,
        ObjectHealthDetailsComponent,
        ObjectIssuesComponent,        
        ObjectRelationshipsComponent,
        PageLinksComponent,
        PeopleResponsibilitiesTile,
        PowerBIViewerComponent,
        RaiseIssueButtonComponent,
        RelationshipTechnicalRelationsComponent,
        ResourceResponsibilityGridComponent,
        TagInputComponent,
        TakeSurveyComponent,
        TileActionsComponent,
        TooltipComponent
        ]
    , imports: [
        CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,
        ReactiveFormsModule,

        //primeng
        GrowlModule,
        InputTextModule,
        InputMaskModule,
        DataTableModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,
        CalendarModule,
        MenuModule,
        MenubarModule,
        AccordionModule,
        SelectButtonModule,
        AutoCompleteModule,
        MultiSelectModule,
        SpinnerModule,
        EditorModule,
        TooltipModule,
        DragDropModule,
        PaginatorModule,       

        //highcharts
        ChartModule,

        //d3s
        PipesModule,             
        D3SFormsModule,   
    ]

})

export class SharedModule { }