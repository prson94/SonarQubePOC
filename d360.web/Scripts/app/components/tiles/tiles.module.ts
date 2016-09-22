import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';


import { SharedModule } from '../shared/shared.module';
//import { AdminModule } from '../admin/admin.module';
//import { PartsModule } from '../parts/parts.module';
import { D3SFormsModule } from '../forms/d3sforms.module';

import { AttributesTile } from './attributes.tile';
import { ClaimsTile } from './claims.tile';
import { FieldDefinitionTile } from './field-definition.tile';
import { FusionAttributesTile } from './fusion-attributes.tile';
import { FusionConfigurationTile } from './fusion-configuration.tile';
import { GroupMembersTile } from './group-members.tile';
import { LoadItemTile } from './load-item.tile';
import { ModelLevelTile } from './model-level.tile';
import { ObjectDefinitionTile } from './object-definition.tile';
import { ObjectDetailTile } from './object-detail.tile';
import { ObjectGovernanceTile } from './object-governance-tile';
import { PredicatesTile } from './predicates.tile';
import { RelationshipsTile } from './relationships.tile';
import { ReportItemsTile } from './report-items.tile';
import { ReportLayoutTile } from './report-layout.tile';
import { RuleDimensionsTile } from './rule-dimensions.tile';
import { StructureTile } from './structure.tile';
import { SurveyQuestionsTile } from './survey-questions.tile';
import { SynonymsTile } from './synonyms.tile';
import { ActivityTile } from './activity-tile.component';
import { AssignmentsTile } from './assignments-tile.component';
import { BoardTile} from './board-tile.component';
import { ActivityDetailsTile} from './activity-details-tile.component';
import { ResourceResponsibilityTile } from './resource-responsibility.tile';
import { ResourceFollowingTile } from './resource-following.tile';
import { ResourceResponsibilityGridTile } from './resource-responsibility-grid.tile';
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
        FieldDefinitionTile,
        FusionAttributesTile,
        FusionConfigurationTile,        
        GroupMembersTile,
        LoadItemTile,
        ModelLevelTile,
        ObjectDefinitionTile,
        ObjectDetailTile,
        ObjectGovernanceTile,         
        PredicatesTile,
        RelationshipsTile,
        ReportItemsTile,
        ReportLayoutTile,
        ResourceFollowingGridTile,
        ResourceFollowingTile,
        ResourceResponsibilityGridTile,
        ResourceResponsibilityTile,
        RuleDimensionsTile,
        StructureTile,
        SurveyQuestionsTile,
        SynonymsTile,        
    ],
    exports: [
        ActivityDetailsTile,
        ActivityTile,
        AssignmentsTile,
        AttributesTile,
        BoardTile,
        ClaimsTile,
        FieldDefinitionTile,
        FusionAttributesTile,
        FusionConfigurationTile,        
        GroupMembersTile,
        LoadItemTile,        
        ModelLevelTile,
        ObjectDefinitionTile,
        ObjectDetailTile,
        ObjectGovernanceTile,          
        PredicatesTile,
        RelationshipsTile,
        ReportItemsTile,
        ReportLayoutTile,
        ResourceFollowingGridTile,
        ResourceFollowingTile,
        ResourceResponsibilityGridTile,
        ResourceResponsibilityTile,
        RuleDimensionsTile,
        StructureTile,
        SurveyQuestionsTile,
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
        SharedModule,        
        D3SFormsModule,        
    ]

})

export class TilesModule { }