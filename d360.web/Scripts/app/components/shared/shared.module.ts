import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule, ReactiveFormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import {
    GrowlModule,
    InputSwitchModule,
    InputTextModule,    
    TreeTableModule,
    ButtonModule, 
    DropdownModule,        
    SelectButtonModule,
    AutoCompleteModule,
    MultiSelectModule,    
    EditorModule,
    TooltipModule,
    SharedModule
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';


import { PipesModule } from '../../pipes/pipes.module';
import { CoreModule } from './core.module';
import { TilesModule  } from './tiles/tiles.module';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedGridPagingInfoModule } from './grid-paging-info.component';
import { SharedDeleteFormModule } from './delete.form';
import { SimpleAccordionModule } from './simple-accordion.part';
import { SharedObjectDetailsModule } from './objectdetails/shared-object-details.module';

import { AttributesTile } from './attributes.tile';
import { GroupMembersComponent } from './group-members.component';
import { MessagesBarComponent } from './messages-bar.component';
import { ObjectDefinitionTile } from './object-definition.tile';
import { ObjectFollowersComponent } from './object-followers.component';
import { ResourceResponsibilityComponent } from './resource-responsibility.component';
import { ResourceResponsibilityGridComponent } from './resource-responsibility-grid.component';
import { SynonymsTile } from './synonyms.tile';
import { TakeSurveyComponent } from './take-survey.component';
import { UserListComponent } from './user-list.component';
import { ResourceMultiSelectGridComponent } from './resource-multiselect-grid.component';


@NgModule({
    declarations: [     
        AttributesTile,                
        GroupMembersComponent,                            
        MessagesBarComponent,                                        
        ObjectDefinitionTile,
        ObjectFollowersComponent,                          
        ResourceResponsibilityComponent,        
        ResourceResponsibilityGridComponent,                        
        SynonymsTile,
        TakeSurveyComponent,                 
        UserListComponent,
        ResourceMultiSelectGridComponent,
    ],
    exports: [                                                                                                                                        
        GroupMembersComponent,                             
        MessagesBarComponent,                                                  
        ObjectDefinitionTile,
        ObjectFollowersComponent,                                 
        ResourceResponsibilityComponent,
        ResourceResponsibilityGridComponent,                        
        TakeSurveyComponent,              
        UserListComponent,    
        ResourceMultiSelectGridComponent,
        ]
    , imports: [
        CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,
        RouterModule,
        
        //primeng
        GrowlModule,
        InputSwitchModule,
        InputTextModule,        
        TreeTableModule,
        ButtonModule,
        DropdownModule,                
        SelectButtonModule,
        AutoCompleteModule,
        MultiSelectModule,        
        EditorModule,
        TooltipModule,                
        SharedModule,                                    
        TableModule,

        //d3s
        CoreModule,
        PipesModule,                    
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,
        SharedGridPagingInfoModule,
        SharedObjectDetailsModule,
        SimpleAccordionModule,        
        TilesModule,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})

export class D3SSharedModule { }