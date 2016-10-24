import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';


import { D3SSharedModule } from '../shared/shared.module';
import { D3SFormsModule } from '../forms/d3sforms.module';

import { AttributesTile } from './attributes.tile';
import { ClaimsTile } from './claims.tile';
import { FusionAttributesTile } from './fusion-attributes.tile';
import { FusionConfigurationTile } from './fusion-configuration.tile';
import { LoadItemTile } from './load-item.tile';
import { ObjectDefinitionTile } from './object-definition.tile';
import { StructureTile } from './structure.tile';
import { SynonymsTile } from './synonyms.tile';
import { ActivityTile } from './activity-tile.component';
import { AssignmentsTile } from './assignments-tile.component';
import { BoardTile} from './board-tile.component';
import { ActivityDetailsTile} from './activity-details-tile.component';
import { ResourceResponsibilityTile } from './resource-responsibility.tile';
import { ResourceFollowingTile } from './resource-following.tile';
import { ResourceFollowingGridTile } from './resource-following-grid.tile';

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
        ActivityDetailsTile,
        ActivityTile,
        AssignmentsTile,
        AttributesTile,
        BoardTile,
        ClaimsTile,         
        FusionAttributesTile,
        FusionConfigurationTile,                
        LoadItemTile,        
        ObjectDefinitionTile,                             
        ResourceFollowingGridTile,
        ResourceFollowingTile,       
        ResourceResponsibilityTile,        
        StructureTile,        
        SynonymsTile,        
    ],
    exports: [
        ActivityDetailsTile,
        ActivityTile,
        AssignmentsTile,
        AttributesTile,
        BoardTile,
        ClaimsTile,        
        FusionAttributesTile,
        FusionConfigurationTile,                
        LoadItemTile,                
        ObjectDefinitionTile,                        
        ResourceFollowingGridTile,
        ResourceFollowingTile,        
        ResourceResponsibilityTile,        
        StructureTile,        
        SynonymsTile,        
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
        D3SSharedModule,        
        D3SFormsModule,        
    ]

})

export class TilesModule { }