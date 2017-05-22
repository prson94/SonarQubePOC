import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';


import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { PipesModule } from '../../../pipes/pipes.module';

import {
    ButtonModule,
    DataTableModule,
    EditorModule,
    InputTextModule,
    SharedModule,
    TreeModule,
    TreeTableModule,
} from 'primeng/primeng';


import { FusionRuleStepMappingListComponent } from './fusion-rule-step-mapping-list.component';
import { FusionRuleStepMappingEditorComponent } from './fusion-rule-step-mapping-editor.component';
import { FusionRuleListComponent } from './fusion-rule-list.component';
import { FusionRuleEditorComponent } from './fusion-rule-editor.component';
import { FusionRuleFilterListComponent } from './fusion-rule-filter-list.component';
import { FusionRuleFilterEditorComponent } from './fusion-rule-filter-editor.component';
import { FusionRuleItemListComponent } from './fusion-rule-item-list.component';
import { FusionRuleItemEditorComponent } from './fusion-rule-item-editor.component';
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
        FusionRuleItemListComponent,
        FusionRuleItemEditorComponent,
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
        FormsModule,
        HttpModule,
        RouterModule,

        FusionRuleRoutingModule,

        //primeng                
        InputTextModule,
        DataTableModule,
        EditorModule,
        ButtonModule,
        SharedModule,
        TreeModule,
        TreeTableModule,

        //d3s
        CoreModule,
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,
        TilesModule,
        PipesModule,

    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})

export class FusionRuleModule { }