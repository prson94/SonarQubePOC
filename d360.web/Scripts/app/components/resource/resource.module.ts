
import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';


import { SharedModule } from '../shared/shared.module';
import { D3SFormsModule } from '../forms/d3sforms.module';
import { TilesModule } from '../tiles/tiles.module';

import { ResourceComponent } from './resource.component';
import { ResourceItemComponent } from './resource-item.component';
import { ResourceApiComponent } from './resource-api.component';
import { ResourceListComponent } from './resource-list.component';
import { ResourceGroupsComponent} from './resource-groups.component';
import { ResourceResponsibilityTile } from './resource-responsibility.tile';
import { ResourceFollowingGridTile } from './resource-following-grid.tile';
import { ResourceFollowingTile } from './resource-following.tile';
  
import { 
    ButtonModule,
    EditorModule,
    InputTextModule, 
    DropdownModule,
    InputMaskModule,
    MultiSelectModule,
    DataTableModule,
    TreeTableModule,
    TooltipModule,
} from 'primeng/primeng';

@NgModule({
    declarations: [
    ResourceComponent,
    ResourceItemComponent,
    ResourceApiComponent, 
    ResourceListComponent,
    ResourceGroupsComponent,
    ResourceResponsibilityTile,
    ResourceFollowingGridTile,
    ResourceFollowingTile, 
    ],
    exports: [
        ResourceComponent,
        ResourceItemComponent,
        ResourceApiComponent,
        ResourceListComponent,
        ResourceGroupsComponent,
        ResourceResponsibilityTile,
        ResourceFollowingGridTile,
        ResourceFollowingTile, 
    ]
    , imports: [
        //angular
        CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //prime
        ButtonModule,
        EditorModule,
        InputTextModule,
        DropdownModule,
        InputMaskModule,
        MultiSelectModule,
        DataTableModule,
        TreeTableModule,
        TooltipModule,

        //d3s
        SharedModule,
        D3SFormsModule,
        TilesModule,
    ]

})

export class ResourceModule { }