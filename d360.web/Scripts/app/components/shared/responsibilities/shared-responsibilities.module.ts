import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/shared';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';
import { EditorModule } from 'primeng/editor';
import { MultiSelectModule } from 'primeng/multiselect';
import { CheckboxModule } from 'primeng/checkbox';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';
import { SharedDeleteFormModule } from '../delete.form';
import { SharedFormMessageModule } from '../form-message.part';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';

import { PeopleResponsibilitiesTile } from './people-responsibilities.tile';
import { ResponsibilityItemForm } from './responsibility-item.form';
import { D3SSharedModule } from '../shared.module';
import { ResponsibilityRelationsComponent } from './responsibility-relations.component';
import { ResponsibilityRelationForm } from './responsibility-relation.form';
import { PipesModule } from '../../../pipes/pipes.module';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        //d3s
        CoreModule,
        TilesModule,
        SharedDeleteFormModule,
        SharedFormMessageModule,
        SharedGridPagingInfoModule,
        D3SSharedModule,
        PipesModule,

        //prime
        ButtonModule,
        CheckboxModule,
        DropdownModule,
        InputTextModule,
        EditorModule,
        MultiSelectModule,
        SharedModule,
        TableModule,
    ],
    declarations: [
        ResponsibilityItemForm,
        PeopleResponsibilitiesTile,
        ResponsibilityRelationsComponent,
        ResponsibilityRelationForm
    ],
    exports: [
        PeopleResponsibilitiesTile,
        ResponsibilityRelationsComponent,
        ResponsibilityRelationForm
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class SharedResponsibilitiesModule { }