import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AdminRelationshipsEditor } from './admin-relationships-editor.component';
import { AdminRelationshipsListComponent } from './admin-relationships-list.component';
import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';
import { CoreModule } from '../../../shared/core.module';
import { SharedDynamicGridEditorModule } from '../../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedGridPagingInfoModule } from '../../../shared/grid-paging-info.component';
import { SearchFieldModule } from '../../../shared/controls/search-field/search-field.component';
import { TilesModule } from '../../../shared/tiles/tiles.module';
import { PopupMenuModule } from '../../../shared/controls/popup-menu/popup-menu.component';
import { RelationshipTypeDeleteComponent } from '../delete/relationship-type-delete.component';
import { SiteModalModule } from '../../../shared/modal/gov-modal.module';
import { IgMessageBoxModule } from '../../../shared/controls/message-box/message-box.module';
import { PropertyGroupModule } from '../../../shared/controls/property-group/property-group.component';
import { FormFeedbackBadgesModule } from '../../../shared/controls/form-feedback-badges/form-feedback-badges.component';
import { TooltipModule } from 'primeng/tooltip';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,        
        SharedModule,
		TableModule,
		TooltipModule,

        //d3s        
        CoreModule,
        SharedDynamicGridEditorModule,        
        SharedGridPagingInfoModule,
		TilesModule,
		SearchFieldModule,
		PopupMenuModule,
		SiteModalModule,
		IgMessageBoxModule,
		PropertyGroupModule,
		FormFeedbackBadgesModule,
		ReactiveFormsModule
    ],
    declarations: [        
        AdminRelationshipsEditor,        
		AdminRelationshipsListComponent,
		RelationshipTypeDeleteComponent
    ],
    exports: [
        AdminRelationshipsListComponent,
    ],
    providers: [

    ]
})
export class AdminRelationshipEditorModule { }