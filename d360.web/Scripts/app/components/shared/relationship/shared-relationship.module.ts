import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule }    from '@angular/router';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    ButtonModule,
    DataTableModule,
    InputTextModule,
    SharedModule,
    TooltipModule,
} from 'primeng/primeng';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedFusionAttributeItemDetailsModule } from '../fusion-attribute-item-details.component';
import { SharedObjectDetailsModule } from '../objectdetails/shared-object-details.module';

import { ObjectRelationshipsComponent } from './object-relationships.component';
import { RelationshipTechnicalRelationsComponent } from './relationship-technical-relations.component';
import { DynamicRelationshipGridComponent } from './dynamic-relationship-grid.component';

@NgModule({
    imports: [CommonModule,
        RouterModule,
        FormsModule,
        HttpModule,
        //d3s
        CoreModule,
        DeprecatedI18NPipesModule,
        PipesModule,
        SharedDynamicGridEditorModule,
        SharedFusionAttributeItemDetailsModule,
        SharedGridPagingInfoModule,    
        SharedObjectDetailsModule,    
        TilesModule,

        //prime
        ButtonModule,
        DataTableModule,        
        InputTextModule,
        SharedModule,
        TooltipModule,
    ],
    declarations: [
        ObjectRelationshipsComponent,
        RelationshipTechnicalRelationsComponent,
        DynamicRelationshipGridComponent,
    ],
    exports: [
        ObjectRelationshipsComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SharedRelationshipModule { }