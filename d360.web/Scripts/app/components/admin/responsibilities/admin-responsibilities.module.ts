import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

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

import { SharedModule } from 'primeng/shared';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { DropdownModule } from 'primeng/dropdown';
import { EditorModule } from 'primeng/editor';
import { MultiSelectModule } from 'primeng/multiselect';
import { TooltipModule } from 'primeng/tooltip';
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
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class AdminResponsibilitiesModule { }