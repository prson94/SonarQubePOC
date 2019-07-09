import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import {
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

import { CoreModule } from '../../shared/core.module';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { ChildrenRoutingModule } from './children.routes';

import { ChildrenComponent } from './children.component';
import { ArtifactItemChildGridComponent } from './artifact-item-child-grid.component';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //routing 
        ChildrenRoutingModule,

        //d3s        
        CoreModule,        
        TilesModule,
        SharedGridPagingInfoModule,
        SharedDynamicGridEditorModule,

        //prime        
        SharedModule,
        TableModule,
    ],
    declarations: [
        ChildrenComponent,
        ArtifactItemChildGridComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class ChildrenModule { }