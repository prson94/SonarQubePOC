import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminRelationshipsEditor } from './admin-relationships-editor.component';
import { AdminRelationshipsListComponent } from './admin-relationships-list.component';
import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';
import { CoreModule } from '../../../shared/core.module';
import { SharedDeleteFormModule } from '../../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedGridPagingInfoModule } from '../../../shared/grid-paging-info.component';
import { SearchFieldModule } from '../../../shared/controls/search-field/search-field.component';
import { TilesModule } from '../../../shared/tiles/tiles.module';


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

        //d3s        
        CoreModule,
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,        
        SharedGridPagingInfoModule,
		TilesModule,
		SearchFieldModule
    ],
    declarations: [        
        AdminRelationshipsEditor,        
        AdminRelationshipsListComponent,
    ],
    exports: [
        AdminRelationshipsListComponent,
    ],
    providers: [

    ]
})
export class AdminRelationshipEditorModule { }