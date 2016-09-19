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

import { PipesModule } from '../../pipes/pipes.module';
import { ChartModule} from './chart.module';
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
import { LineageComponent } from './lineage.component';
import { MessagesComponent } from './messages.component';
import { ObjectBoardComponent } from './object-board.component';
import { ObjectChallengeComponent } from './object-challenge.component';
import { ObjectFollowersComponent } from './object-followers.component';
import { ObjectHealthComponent } from './object-health.component';
import { ObjectHealthDetailsComponent } from './object-health-details.component';
import { ObjectIssuesComponent } from './object-issues.component';
import { PageLinksComponent } from './page-links.component';
import { PeopleResponsibilitiesTile } from './people-responsibilities.tile';
import { PowerBIViewerComponent } from './powerbi-viewer.component';
import { TagInputComponent } from './tag-input.component';
import { RelationshipTechnicalRelationsComponent } from './relationship-technical-relations.component';
import { TileActionsComponent } from './tile-actions.component';
import { TooltipComponent } from './tooltip.component';


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
        LineageComponent,
        MessagesComponent,
        ObjectBoardComponent,
        ObjectChallengeComponent,
        ObjectFollowersComponent,
        ObjectHealthComponent,
        ObjectHealthDetailsComponent,
        ObjectIssuesComponent,        
        PageLinksComponent,
        PeopleResponsibilitiesTile,
        PowerBIViewerComponent,
        RelationshipTechnicalRelationsComponent,
        TagInputComponent,
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
        LineageComponent,
        MessagesComponent,
        ObjectBoardComponent,
        ObjectChallengeComponent,
        ObjectFollowersComponent,
        ObjectHealthComponent,
        ObjectHealthDetailsComponent,
        ObjectIssuesComponent,        
        PageLinksComponent,
        PeopleResponsibilitiesTile,
        PowerBIViewerComponent,
        RelationshipTechnicalRelationsComponent,
        TagInputComponent,
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

        //d3s
        PipesModule,
        ChartModule,     
        D3SFormsModule,   
    ]

})

export class SharedModule { }