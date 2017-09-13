import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule, ReactiveFormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';


import {
    GrowlModule,
    InputSwitchModule,
    InputTextModule,    
    DataTableModule,
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


import { PipesModule } from '../../pipes/pipes.module';
import { CoreModule } from './core.module';
import { TilesModule  } from './tiles/tiles.module';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedGridPagingInfoModule } from './grid-paging-info.component';
import { SharedDeleteFormModule } from './delete.form';
import { SimpleAccordionModule } from './simple-accordion.part';
import { SharedObjectDetailsModule } from './objectdetails/shared-object-details.module';

import { AttributesTile } from './attributes.tile';
import { FusionFiltersComponent } from './fusion-filters.component';
import { GroupMembersComponent } from './group-members.component';
import { MessagesBarComponent } from './messages-bar.component';
import { ObjectDefinitionTile } from './object-definition.tile';
import { ObjectFollowersComponent } from './object-followers.component';
import { ResourceResponsibilityComponent } from './resource-responsibility.component';
import { ResourceResponsibilityGridComponent } from './resource-responsibility-grid.component';
import { StructureTile } from './structure.tile';
import { SynonymsTile } from './synonyms.tile';
import { TakeSurveyComponent } from './take-survey.component';
import { UserListComponent } from './user-list.component';


@NgModule({
    declarations: [     
        AttributesTile,        
        FusionFiltersComponent,        
        GroupMembersComponent,                            
        MessagesBarComponent,                                        
        ObjectDefinitionTile,
        ObjectFollowersComponent,                          
        ResourceResponsibilityComponent,        
        ResourceResponsibilityGridComponent,                
        StructureTile,
        SynonymsTile,
        TakeSurveyComponent,                 
        UserListComponent,        
    ],
    exports: [                                                                                                                       
        FusionFiltersComponent,         
        GroupMembersComponent,                             
        MessagesBarComponent,                                                  
        ObjectDefinitionTile,
        ObjectFollowersComponent,                                 
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
        InputSwitchModule,
        InputTextModule,        
        DataTableModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,                
        SelectButtonModule,
        AutoCompleteModule,
        MultiSelectModule,        
        EditorModule,
        TooltipModule,                
        SharedModule,                                    
        
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