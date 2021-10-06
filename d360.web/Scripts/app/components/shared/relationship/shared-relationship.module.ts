import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule }    from '@angular/router';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { TableModule } from 'primeng/table';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../objectdetails/shared-object-details.module';

import { ObjectRelationshipsComponent } from './object-relationships.component';
import { DynamicRelationshipGridComponent } from './dynamic-relationship-grid.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { DirectivesModule } from '../../../directives/directives.module';
import { SiteModalModule } from '../modal/gov-modal.module';
import { RelationshipsModalComponent } from './relationships-modal.component';
import { IgCheckboxModule } from '../../../directives/ig-checkbox-directive';
import { CheckboxModule } from 'primeng/checkbox';

@NgModule({
    imports: [
        CommonModule,
        RouterModule,
        FormsModule,
        HttpClientModule,
        //d3s
        CoreModule,
        PipesModule,
        SharedDynamicGridEditorModule,
        SharedGridPagingInfoModule,    
        SharedObjectDetailsModule,    
        TilesModule,
        SharedDeleteFormModule,
        IgCheckboxModule,
        //prime
        ButtonModule,
        InputTextModule,
        SharedModule,
        TooltipModule,
        TableModule,
        DirectivesModule,
        TooltipModule,
        SiteModalModule,
        CheckboxModule
    ],
    declarations: [
        ObjectRelationshipsComponent,
        DynamicRelationshipGridComponent,
        RelationshipsModalComponent,
    ],
    exports: [
        ObjectRelationshipsComponent,
        RelationshipsModalComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class SharedRelationshipModule { }