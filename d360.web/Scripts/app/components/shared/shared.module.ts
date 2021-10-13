import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";
import { RouterModule } from '@angular/router';

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';
import { EditorModule } from 'primeng/editor';
import { MultiSelectModule } from 'primeng/multiselect';
import { TooltipModule } from 'primeng/tooltip';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TreeTableModule } from 'primeng/treetable';
import { InputSwitchModule } from 'primeng/inputswitch';
import { ToastModule } from 'primeng/toast';


import { PipesModule } from '../../pipes/pipes.module';
import { CoreModule } from './core.module';
import { TilesModule  } from './tiles/tiles.module';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedGridPagingInfoModule } from './grid-paging-info.component';
import { SharedDeleteFormModule } from './delete.form';
import { SimpleAccordionModule } from './simple-accordion.part';
import { SharedObjectDetailsModule } from './objectdetails/shared-object-details.module';
import { AdvancedFiltersModule } from "../assets-grid/advanced-filtering/advanced-filtering.module";
import { SearchFieldModule } from "../shared/controls/search-field/search-field.component";

import { GroupMembersComponent } from './group-members.component';
import { MessagesBarComponent } from './messages-bar.component';
import { ObjectDefinitionTile } from './object-definition.tile';
import { ObjectFollowersComponent } from './object-followers.component';
import { ResourceResponsibilityComponent } from './resource-responsibility.component';
import { ResourceResponsibilityGridComponent } from './resource-responsibility-grid.component';
import { UserListComponent } from './user/user-list.component';
import { ResourceMultiSelectGridComponent } from './resource-multiselect-grid.component';
import { SiteModalModule } from './modal/gov-modal.module';
import { AssetDetailModule } from './asset-detail/asset-detail.module';

@NgModule({
    declarations: [                           
        GroupMembersComponent,                            
        MessagesBarComponent,                                        
        ObjectDefinitionTile,
        ObjectFollowersComponent,                          
        ResourceResponsibilityComponent,        
        ResourceResponsibilityGridComponent,      
        UserListComponent,
        ResourceMultiSelectGridComponent
    ],
    exports: [                                                                                                                                        
        GroupMembersComponent,                             
        MessagesBarComponent,                                                  
        ObjectDefinitionTile,
        ObjectFollowersComponent,                                 
        ResourceResponsibilityComponent,
        ResourceResponsibilityGridComponent,               
        UserListComponent,    
        ResourceMultiSelectGridComponent
        ]
    , imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,
        
        //primeng
        ToastModule,
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
        SiteModalModule,
        AssetDetailModule,
        AdvancedFiltersModule,
        SearchFieldModule,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})

export class D3SSharedModule {}