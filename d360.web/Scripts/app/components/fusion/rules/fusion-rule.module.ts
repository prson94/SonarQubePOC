import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";
import { RouterModule } from '@angular/router';


import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { PipesModule } from '../../../pipes/pipes.module';

import {
    ButtonModule,
    EditorModule,
    InputTextModule,
    SharedModule,
    TreeModule,
    TreeTableModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';
import { FusionRuleStepMappingListComponent } from './fusion-rule-step-mapping-list.component';
import { FusionRuleStepMappingEditorComponent } from './fusion-rule-step-mapping-editor.component';
import { FusionRuleListComponent } from './fusion-rule-list.component';
import { FusionRuleEditorComponent } from './fusion-rule-editor.component';
import { FusionRuleFilterListComponent } from './fusion-rule-filter-list.component';
import { FusionRuleFilterEditorComponent } from './fusion-rule-filter-editor.component';
import { FusionRulesComponent } from './fusion-rules.component';
import { FusionRuleStepListComponent } from './fusion-rule-step-list.component';
import { FusionRuleStepHistoryComponent } from './fusion-rule-step-history.component';
import { FusionRuleStepComponent } from './fusion-rule-step.component';
import { FusionRuleStepFindComponent } from './fusion-rule-step-find.component';
import { FusionRuleStepFindViaRelationComponent } from './fusion-rule-step-findviarelation.component';
import { FusionRuleStepLineageComponent } from './fusion-rule-step-lineage.component';
import { FusionRuleStepPromoteComponent } from './fusion-rule-step-promote.component';
import { FusionRuleStepRelateComponent } from './fusion-rule-step-relate.component';
import { FusionRuleStepUpdateComponent } from './fusion-rule-step-update.component';
import { D3SSortIconModule } from '../../shared/turbotable-sorticon.component';
import { D3SColumnFilterModule } from '../../shared/turbotable-column-filter.component';

import { FusionRuleRoutingModule } from './fusion-rule.routes';

@NgModule({
    declarations: [
        FusionRuleStepMappingListComponent,
        FusionRuleStepMappingEditorComponent,
        FusionRulesComponent,
        FusionRuleListComponent,
        FusionRuleEditorComponent,
        FusionRuleFilterListComponent,
        FusionRuleFilterEditorComponent,
        FusionRuleStepListComponent,
        FusionRuleStepHistoryComponent,
        FusionRuleStepComponent,
        FusionRuleStepFindComponent,
        FusionRuleStepFindViaRelationComponent,
        FusionRuleStepLineageComponent,
        FusionRuleStepPromoteComponent,
        FusionRuleStepRelateComponent,
        FusionRuleStepUpdateComponent,
    ],    
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        FusionRuleRoutingModule,

        //primeng                
        InputTextModule,
        EditorModule,
        ButtonModule,
        SharedModule,
        TreeModule,
        TreeTableModule,
        TableModule,

        //d3s
        CoreModule,
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,
        TilesModule,
        PipesModule,

    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})

export class FusionRuleModule { }