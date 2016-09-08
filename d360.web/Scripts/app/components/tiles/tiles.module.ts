import {  NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
//import * as tiles from './index';
//import * as primeng from 'primeng/primeng';
import { SharedModule } from '../shared/shared.module';
import { AdminModule } from '../admin/admin.module';
import { PartsModule } from '../parts/parts.module';
import { InputFormsModule } from '../forms/input-forms.module';

import {
    AttributesTile,
    ClaimsTile,
    FieldDefinitionTile,
    FusionAttributesTile,
    FusionConfigurationTile,
    FusionFiltersTile,
    GroupMembersTile,
    LoadItemTile,
    MenuBarItem,
    ModelLevelTile,
    ObjectDefinitionTile,
    ObjectDetailTile,
    ObjectGovernanceTile,
    ObjectRelationshipsTile,
    PeopleResponsibilitiesTile,
    PredicatesTile,
    RelationshipsTile,
    ReportItemsTile,
    ReportLayoutTile,
    RuleDimensionsTile,
    StructureTile,
    SurveyQuestionsTile,
    SynonymsTile,    
} from './index';

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
        ClaimsTile, 
        FieldDefinitionTile,
        FusionAttributesTile,
        FusionConfigurationTile,
        FusionFiltersTile,
        GroupMembersTile,
        LoadItemTile,
        //MenuBarItem,
        ModelLevelTile,
        ObjectDefinitionTile,
        ObjectDetailTile,
        ObjectGovernanceTile,
        ObjectRelationshipsTile,
        PeopleResponsibilitiesTile,
        PredicatesTile,
        RelationshipsTile,
        ReportItemsTile,
        ReportLayoutTile,
        RuleDimensionsTile,
        StructureTile,
        SurveyQuestionsTile,
        SynonymsTile,        
    ],
    exports: [
        AttributesTile,
        ClaimsTile,
        FieldDefinitionTile,
        FusionAttributesTile,
        FusionConfigurationTile,
        FusionFiltersTile,
        GroupMembersTile,
        LoadItemTile,
        //MenuBarItem,
        ModelLevelTile,
        ObjectDefinitionTile,
        ObjectDetailTile,
        ObjectGovernanceTile,
        ObjectRelationshipsTile,
        PeopleResponsibilitiesTile,
        PredicatesTile,
        RelationshipsTile,
        ReportItemsTile,
        ReportLayoutTile,
        RuleDimensionsTile,
        StructureTile,
        SurveyQuestionsTile,
        SynonymsTile,        
        ]
    , imports: [
        ButtonModule,
        EditorModule,
        InputTextModule,
        DropdownModule,
        InputMaskModule,
        MultiSelectModule,
        DataTableModule,
        TreeTableModule,
        TooltipModule,
        BrowserModule,
        SharedModule,
        //AdminModule,
        InputFormsModule,
        PartsModule,
    ]

})

export class TilesModule { }