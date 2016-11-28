import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {    
    DataTableModule,
    EditorModule,
    SharedModule,

} from 'primeng/primeng';

import { ImpactComponent } from './impact.component';
import { LineageComponent } from './lineage.component';
import { LineageFusionComponent } from './lineage-fusion.component';
import { LineageMappingRulesComponent } from './lineage-mapping-rules.component';
import { LineageObjectDetailComponent } from './lineage-object-detail.component';
import { LineageRelationshipsComponent } from './lineage-relationships.component';
import { LineageResponsibilitiesComponent } from './lineage-responsibilities.component';
import { LineageSourceRuleEditorComponent } from './lineage-source-rule-editor.component';
import { LineageSourceRulesComponent } from './lineage-source-rules.component';
import { LineageTechnicalRelationshipsComponent } from './lineage-technical-relationships.component';
import { ModelDiagramComponent } from './model-diagram.component';
import { OverlayWindowComponent } from './overlay-window.component';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';
import { SharedDeleteFormModule } from '../delete.form';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { SharedFormMessageModule } from '../form-message.part'



@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        //d3s
        CoreModule,
        SharedDeleteFormModule,
        SharedFormMessageModule,
        SharedGridPagingInfoModule,
        TilesModule,

        //prime        
        DataTableModule,  
        EditorModule,      
        SharedModule,
    ],
    declarations: [
        ImpactComponent,        
        LineageComponent,
        LineageFusionComponent,
        LineageMappingRulesComponent,
        LineageObjectDetailComponent,
        LineageRelationshipsComponent,
        LineageResponsibilitiesComponent,
        LineageSourceRuleEditorComponent,
        LineageSourceRulesComponent,
        LineageTechnicalRelationshipsComponent,
        ModelDiagramComponent,
        OverlayWindowComponent,
    ],
    exports: [
        LineageComponent,   
        ImpactComponent,  
        ModelDiagramComponent,      
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class SharedDiagramModule { }