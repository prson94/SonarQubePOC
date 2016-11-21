import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';


import { D3SSharedModule } from '../shared/shared.module';
import { D3SFormsModule } from '../forms/d3sforms.module';

import { AttributesTile } from './attributes.tile';
import { ObjectDefinitionTile } from './object-definition.tile';
import { StructureTile } from './structure.tile';
import { SynonymsTile } from './synonyms.tile';
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
        AttributesTile,                           
        ObjectDefinitionTile,                             
        ResourceFollowingGridTile,
        ResourceFollowingTile,               
        StructureTile,        
        SynonymsTile,        
    ],
    exports: [                
        AttributesTile,                                                  
        ObjectDefinitionTile,                        
        ResourceFollowingGridTile,
        ResourceFollowingTile,                      
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