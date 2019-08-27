import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { RouterModule }    from '@angular/router';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import {
    ButtonModule,
    InputTextModule,
    SharedModule,
    TooltipModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

import { CoreModule } from '../core.module';
import { SimpleAccordionModule } from '../simple-accordion.part';
import { TilesModule  } from '../tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../dynamicgrideditor/shared-dynamic-grid-editor.module';

import { DynamicLookupGridComponent } from './dynamic-lookup-grid.component';
import { ObjectDetailComponent } from './object-detail.component';
import { ObjectDetailFieldComponent } from './object-detail-field.component';
import { PipesModule } from '../../../pipes/pipes.module';
import { NgxJsonViewModule } from 'ng-json-view';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        RouterModule,
        HttpClientModule,
        //d3s
        CoreModule,
        SharedGridPagingInfoModule,
        SimpleAccordionModule,
        TilesModule,
        SharedDynamicGridEditorModule,
        PipesModule,
        //prime
        ButtonModule,
        InputTextModule,
        SharedModule,
        TooltipModule,
        TableModule,
        //JSON Viewer module
        NgxJsonViewModule,
    ],
    declarations: [
        DynamicLookupGridComponent,
        ObjectDetailComponent,
        ObjectDetailFieldComponent,
    ],
    exports: [
        ObjectDetailComponent,
        ObjectDetailFieldComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class SharedObjectDetailsModule { }