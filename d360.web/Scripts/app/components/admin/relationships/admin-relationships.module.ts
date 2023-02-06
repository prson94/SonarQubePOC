import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { AdminRelationshipsComponent } from './admin-relationships.component';
import { AdminRelationshipsRoutingModule } from './admin-relationships.routes';
import { SharedModule } from 'primeng/api';
import { TableModule } from 'primeng/table';
import { AdminRelationshipsSidePanelWrapperModule } from './sidepanel-wrapper/admin-relationships-sidepanel-wrapper.module';
import { AdminRelationshipEditorModule } from './list/admin-relationship-editor.module';

@NgModule({
    imports: [
        CommonModule,        
        FormsModule,


        AdminRelationshipsRoutingModule,

        //prime        
        SharedModule,
        TableModule,

        //d3s        
        AdminRelationshipEditorModule,
        CoreModule,
		PipesModule,
		AdminRelationshipsSidePanelWrapperModule,
        
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,
        SharedFieldDefinitionModule,
        SharedGridPagingInfoModule,
		TilesModule
    ],
    declarations: [
		AdminRelationshipsComponent
    ],    
    providers: [
	],
	exports: [
		AdminRelationshipsComponent
	]
})
export class AdminRelationshipsModule { }