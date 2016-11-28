import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule, ReactiveFormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';


import {
    GrowlModule,
    InputTextModule,    
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    CalendarModule,     
    AccordionModule,
    SelectButtonModule,
    AutoCompleteModule,
    MultiSelectModule,    
    EditorModule,
    TooltipModule,        
    SharedModule,                
} from 'primeng/primeng';

import { ChartModule } from 'angular2-highcharts';

import { SocialModule } from '../shared/social/social.module';
import { WorkflowModule } from '../workflow/workflow.module';
import { PipesModule } from '../../pipes/pipes.module';
import { CoreModule } from './core.module';
import { TilesModule  } from './tiles/tiles.module';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedGridPagingInfoModule } from './grid-paging-info.component';
import { SharedDeleteFormModule } from './delete.form';
import { SimpleAccordionModule } from './simple-accordion.part';
import { SharedObjectDetailsModule } from './objectdetails/shared-object-details.module';

import { ColorPickerModule } from 'angular2-color-picker';

import { ActionBar } from './action-bar.part';
import { ArtifactStatusComponent } from './artifact-status.component';
import { AttributesTile } from './attributes.tile';
import { FollowerGridComponent } from './follower-grid.component';
import { FusionFiltersComponent } from './fusion-filters.component';
import { GroupMembersComponent } from './group-members.component';
import { MenuPart } from './menu.part';
import { MessagesBarComponent } from './messages-bar.component';
import { ObjectBoardComponent } from './object-board.component';
import { ObjectDefinitionTile } from './object-definition.tile';
import { ObjectFollowersComponent } from './object-followers.component';
import { ObjectGovernanceComponent } from './object-governance.component';
import { ObjectHealthComponent } from './object-health.component';
import { ObjectHealthDetailsComponent } from './object-health-details.component';
import { ObjectIssuesComponent } from './object-issues.component';
import { ResourceResponsibilityComponent } from './resource-responsibility.component';
import { ResourceResponsibilityGridComponent } from './resource-responsibility-grid.component';
import { StructureTile } from './structure.tile';
import { SynonymsTile } from './synonyms.tile';
import { TakeSurveyComponent } from './take-survey.component';
import { UserListComponent } from './user-list.component';


@NgModule({
    declarations: [
        ActionBar,
        ArtifactStatusComponent,        
        AttributesTile,                                                                            
        FollowerGridComponent,                 
        FusionFiltersComponent,        
        GroupMembersComponent,                         
        MenuPart,        
        MessagesBarComponent,                                
        ObjectBoardComponent,        
        ObjectDefinitionTile,        
        ObjectFollowersComponent,
        ObjectGovernanceComponent,
        ObjectHealthComponent,
        ObjectHealthDetailsComponent,
        ObjectIssuesComponent,                   
        ResourceResponsibilityComponent,        
        ResourceResponsibilityGridComponent,                
        StructureTile,
        SynonymsTile,
        TakeSurveyComponent,                 
        UserListComponent,        
    ],
    exports: [                                                                                                               
        FollowerGridComponent,          
        FusionFiltersComponent,         
        GroupMembersComponent,                             
        MessagesBarComponent,                                  
        ObjectBoardComponent,        
        ObjectDefinitionTile,        
        ObjectFollowersComponent,
        ObjectGovernanceComponent,
        ObjectHealthComponent,
        ObjectHealthDetailsComponent,                              
        ResourceResponsibilityComponent,
        ResourceResponsibilityGridComponent,                        
        TakeSurveyComponent,              
        UserListComponent,         
        ]
    , imports: [
        CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,
        
        //primeng
        GrowlModule,
        InputTextModule,        
        DataTableModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,
        CalendarModule,           
        AccordionModule,
        SelectButtonModule,
        AutoCompleteModule,
        MultiSelectModule,        
        EditorModule,
        TooltipModule,                
        SharedModule,                                    

        //highcharts
        ChartModule,

        ColorPickerModule,

        //d3s
        CoreModule,
        PipesModule,                    
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,
        SharedGridPagingInfoModule,
        SharedObjectDetailsModule,
        SimpleAccordionModule,
        SocialModule,
        TilesModule,
        WorkflowModule,        
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})

export class D3SSharedModule { }