import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { ResourceRoutingModule } from './resource.routes';

import { D3SSharedModule } from '../shared/shared.module';
import { CoreModule } from '../shared/core.module';
import { SocialModule } from '../shared/social/social.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';
import { SharedObjectGovernanceModule } from '../shared/objectgovernance/shared-object-governance.module';
import { SharedAssignmentsModule } from '../shared/assignments/shared-assignments.module'



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
  
import { 
    ButtonModule,    
    InputTextModule, 
    DropdownModule,
    InputMaskModule,
    MultiSelectModule,
    DataTableModule,
    TreeTableModule,
    TooltipModule,
    SharedModule,
} from 'primeng/primeng';

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
        FormsModule,
        HttpModule,
        RouterModule,
        //ResourceFollowingTile,
        ResourceRoutingModule,

        //prime
        ButtonModule,        
        InputTextModule,
        DropdownModule,
        InputMaskModule,
        MultiSelectModule,
        DataTableModule,
        TreeTableModule,
        TooltipModule,
        SharedModule,

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
        SharedObjectGovernanceModule,
    ],
    exports: [
        ResourceFollowingTile,
        ResourceGroupsComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]

})

export class ResourceModule { }