import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

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
        HttpModule,
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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class ChildrenModule { }