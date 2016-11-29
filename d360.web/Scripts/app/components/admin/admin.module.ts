import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { ColorPickerModule } from 'angular2-color-picker';

import { AdminRelationshipEditorModule } from '../shared/relationshipeditor/admin-relationship-editor.module';
import { AdminRoutingModule } from './admin.routes';
import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedAuditModule } from '../shared/audit/shared-audit.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedFieldDefinitionModule } from '../shared/fielddefinition/shared-field-definition.module';
import { SharedResponsibilitiesModule } from '../shared/responsibilities/shared-responsibilities.module';
import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';

import {        
    InputTextModule,    
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,                
    SelectButtonModule,    
    MultiSelectModule,    
    EditorModule,
    TooltipModule,            
    GrowlModule,
    SharedModule,
} from 'primeng/primeng';

import { AdminArtifactsComponent } from './admin-artifacts.component';
import { AdminComponent } from './admin.component';
import { AdminLevelEditorComponent } from './admin-level-editor.component';
import { AdminLevelListComponent } from './admin-level-list.component';

import { ArtifactTypeForm } from './artifact-type.form';
import { ClaimsTile } from './claims.tile';
import { ClaimsMatrixPart } from './claims-matrix.part';

@NgModule({
    declarations: [        
        AdminArtifactsComponent,
        AdminComponent,                
        AdminLevelListComponent,
        AdminLevelEditorComponent,        
        ArtifactTypeForm,
        ClaimsMatrixPart,
        ClaimsTile,                          
    ],
    exports: [
        ClaimsTile,
        AdminLevelListComponent,
    ],
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,
        AdminRoutingModule,
        
        //primeng                
        InputTextModule,        
        DataTableModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,                
        SelectButtonModule,        
        MultiSelectModule,        
        EditorModule,
        TooltipModule,                        
        SharedModule,
        GrowlModule,

        //color picker
        ColorPickerModule,

        //d3s
        AdminRelationshipEditorModule,
        CoreModule,
        D3SSharedModule,                
        PipesModule,    
        SharedAuditModule,     
        SharedDeleteFormModule,
        SharedFieldDefinitionModule,
        SharedGridPagingInfoModule, 
        SharedDynamicGridEditorModule,
        SharedObjectDetailsModule,
        SharedResponsibilitiesModule,
        TilesModule,  
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})

export class AdminModule { }