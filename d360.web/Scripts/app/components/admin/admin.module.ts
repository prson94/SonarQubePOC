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

import { AdminAttributeAllocationComponent } from './admin-attribute-allocation.component';
import { AdminGovernanceComponent } from './admin-governance.component';
import { AdminGroupsComponent } from './admin-groups.component';
import { AdminArtifactsComponent } from './admin-artifacts.component';
import { AdminTaxonomiesComponent } from './admin-taxonomies.component';
import { AdminRulesComponent } from './admin-rules.component';
import { AdminPoliciesComponent } from './admin-policies.component';
import { AdminAttributesComponent } from './admin-attributes.component';
import { AdminResourcesComponent } from './admin-resources.component';
import { AdminFusionComponent } from './admin-fusion.component';
import { AdminComponent } from './admin.component';
import { AdminAttributeTypeEditor } from './admin-attribute-type-editor.component';
import { AdminTaxonomyEditorComponent } from './admin-taxonomy-editor.component';
import { AdminTaxonomyDetailComponent } from './admin-taxonomy-detail.component';
import { AdminLevelEditorComponent } from './admin-level-editor.component';
import { AdminRuleDimensionsComponent } from './admin-rule-dimensions.component';
import { AdminLevelListComponent } from './admin-level-list.component';
import { AdminModelClassificationComponent } from './admin-model-classification.component';
import { ArtifactTypeForm } from './artifact-type.form';
import { ClaimsTile } from './claims.tile';
import { ClaimsMatrixPart } from './claims-matrix.part';
import { FusionConfigurationTile } from './fusion-configuration.tile';
import { FusionAttributesTile } from './fusion-attributes.tile';
import { GroupForm } from './group.form';
import { ResponsibilityTypeForm } from './responsibility-type.form';

@NgModule({
    declarations: [
        AdminAttributeAllocationComponent,
        AdminArtifactsComponent,
        AdminComponent,
        AdminAttributesComponent,            
        AdminFusionComponent,
        AdminGovernanceComponent,
        AdminGroupsComponent,           
        AdminPoliciesComponent,        
        AdminResourcesComponent,
        AdminRulesComponent,                    
        AdminTaxonomiesComponent,
        AdminAttributeTypeEditor,
        AdminLevelListComponent,
        AdminTaxonomyDetailComponent,
        AdminTaxonomyEditorComponent,
        AdminLevelEditorComponent,        
        AdminRuleDimensionsComponent,        
        AdminModelClassificationComponent,        
        ArtifactTypeForm,        
        ClaimsTile,
        ClaimsMatrixPart,
        FusionAttributesTile,
        FusionConfigurationTile,
        GroupForm,            
        ResponsibilityTypeForm,        
    ]
    , imports: [CommonModule,
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