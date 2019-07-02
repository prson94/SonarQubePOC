import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { AdminModule } from '../admin.module';


import { AdminGovernanceComponent } from './admin-governance.component';
import { ResponsibilityTypeForm } from './responsibility-type.form';
import { ResponsibilityRulesComponent } from './responsibility-rules.component';

import { ResponsibilityRuleForm } from './responsibility-rule.form';

import { AdminResponsibilitiesRoutingModule } from './admin-responsibilities.routes';

import { SimpleAccordionModule } from '../../shared/simple-accordion.part';

import {
    ButtonModule, 
    CheckboxModule,
    DropdownModule,
    EditorModule,
    InputTextModule,
    MultiSelectModule,
    SharedModule,
    TooltipModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [
        CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        AdminResponsibilitiesRoutingModule,

        //prime
        ButtonModule,
        CheckboxModule,
        DropdownModule,
        EditorModule,
        InputTextModule,
        MultiSelectModule,
        SharedModule,
        TooltipModule,
        TableModule,

        //d3s        
        CoreModule,
        PipesModule,
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedObjectDetailsModule,
        SharedResponsibilitiesModule,

        SimpleAccordionModule,
        TilesModule,
        AdminModule,
    ],
    declarations: [
        AdminGovernanceComponent,
        ResponsibilityTypeForm,
        ResponsibilityRulesComponent,
        ResponsibilityRuleForm
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class AdminResponsibilitiesModule { }