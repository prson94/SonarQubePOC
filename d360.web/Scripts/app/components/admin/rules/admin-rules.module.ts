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
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module'; 
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';
import { AdminModule } from '../admin.module';

import { AdminRulesComponent } from './admin-rules.component';

import { AdminRulesRoutingModule } from './admin-rules.routes';

import {
    ButtonModule,
    DropdownModule,
    InputTextModule,
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        AdminRulesRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        TableModule,

        //d3s        
        CoreModule,
        PipesModule,
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,    
        SharedObjectDetailsModule,
        SharedFieldDefinitionModule,
        SharedDynamicGridEditorModule,
        SharedResponsibilitiesModule,    
        TilesModule,
        AdminModule,

    ],
    declarations: [
        AdminRulesComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class AdminRulesModule { }