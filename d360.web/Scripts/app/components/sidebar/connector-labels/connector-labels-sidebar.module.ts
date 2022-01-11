import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';



import { RouterModule } from '@angular/router';

import { SharedModule } from 'primeng/api';
import { TableModule } from 'primeng/table';

import { CoreModule } from '../../shared/core.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { EditorModule } from 'primeng/editor';
import { DropdownModule } from 'primeng/dropdown';
import { ButtonModule } from 'primeng/button';
import { ConnectorLabelsRoutingModule } from './connector-labels-sidebar.routes';
import { ConnectorLabelsComponent } from './connector-labels-sidebar.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { SiteModalModule } from '../../shared/modal/gov-modal.module';
import { WhereUsedModule } from '../../shared/where-used/where-used.module';
import { ConnectorLabelsFormComponent } from './connector-label-form.component';
import { DirectivesModule } from '../../../directives/directives.module';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { ConnectorLabelFormModule } from './connector-label-form.module';
import { PopupMenuModule } from '../../shared/controls/popup-menu/popup-menu.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,

        RouterModule,

        //routing 
        ConnectorLabelsRoutingModule,

        //d3s        
        CoreModule,
        SharedGridPagingInfoModule,
        TilesModule,
        DirectivesModule,

        //prime     
        EditorModule,
        DropdownModule,
        ButtonModule,
        SharedModule,
        TableModule,
        CoreModule,
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,
        SharedObjectDetailsModule,
        SharedGridPagingInfoModule,
        TilesModule,
        SiteModalModule,
        WhereUsedModule,
        AutoCompleteModule,

        ConnectorLabelFormModule,
        PopupMenuModule
    ],
    declarations: [
        ConnectorLabelsComponent
    ],
    providers: [
            ]
})
export class ConnectorLabelsModule { }