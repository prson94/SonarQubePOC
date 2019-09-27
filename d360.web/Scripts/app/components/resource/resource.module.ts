import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { ResourceRoutingModule } from './resource.routes';

import { D3SSharedModule } from '../shared/shared.module';
import { CoreModule } from '../shared/core.module';
import { SocialModule } from '../shared/social/social.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';
import { SharedAssignmentsModule } from '../shared/assignments/shared-assignments.module'
import { ApiKeyUsersGuard } from '../../guards/api-key-users.gurard';


import { ResourceApiComponent } from './resource-api.component';
import { ResourceComponent } from './resource.component';
import { ResourceGroupsComponent} from './resource-groups.component';
import { ResourceItemComponent } from './resource-item.component';
import { ResourceFollowingGridTile } from './resource-following-grid.tile';
import { ResourceFollowingTile } from './resource-following.tile';
import { ResourceListComponent } from './resource-list.component';
import { ResourcePasswordComponent } from './resource-password.component';
import { ResourceKeyComponent } from './resource-key.component';
import { ResourceChangePwdComponent } from './resource-change-pwd.component';

import { MultiSelectModule } from 'primeng/multiselect';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { InputMaskModule } from 'primeng/inputmask';
import { SharedModule } from 'primeng/shared';
import { InputTextModule } from 'primeng/inputtext';
import { TreeTableModule } from 'primeng/treetable';
import { TooltipModule } from 'primeng/tooltip';
import { TableModule } from 'primeng/table';

@NgModule({
    declarations: [
        ResourceComponent,
        ResourceItemComponent,
        ResourceApiComponent, 
        ResourceListComponent,
        ResourceGroupsComponent,    
        ResourceFollowingGridTile,
        ResourceFollowingTile,
        ResourcePasswordComponent,       
        ResourceKeyComponent,
        ResourceChangePwdComponent,
    ],    
    imports: [
        //angular
        CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,        
        ResourceRoutingModule,

        //prime
        ButtonModule,        
        InputTextModule,
        DropdownModule,
        InputMaskModule,
        MultiSelectModule,
        TreeTableModule,
        TooltipModule,
        SharedModule,
        TableModule,


        //d3s
        D3SSharedModule,          
        CoreModule,   
        SocialModule,
        SharedAssignmentsModule,
        PipesModule,
        TilesModule,
        SharedDynamicGridEditorModule,
        SharedGridPagingInfoModule,
        SharedObjectDetailsModule,
    ],
    exports: [
        ResourceFollowingTile,
        ResourceGroupsComponent
    ],
    providers: [
       ApiKeyUsersGuard,
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]

})

export class ResourceModule { }