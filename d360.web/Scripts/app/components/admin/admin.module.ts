import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { RouterModule } from '@angular/router'; 

import { AdminRoutingModule } from './admin.routes';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedAssetTypeEditorModule } from '../shared/assettypeeditor/shared-asset-type-editor.module';

import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { ColorPickerModule } from 'primeng/colorpicker';
import { SpinnerModule } from 'primeng/spinner';
import { EditorModule } from 'primeng/editor';
import { TableModule } from 'primeng/table';

import { AdminBaseComponent } from './admin-base.component';
import { AdminAllocationComponent } from './admin-allocation.component';
import { AdminComponent } from './admin.component';
import { AdminNymAllocationsComponent } from './admin-nym-allocations.component';
import { SimpleAccordionModule } from "../shared/simple-accordion.part";
import { DialogModule } from 'primeng/dialog';
import { CheckboxModule } from 'primeng/checkbox';
import { DirectivesModule } from '../../directives/directives.module';


@NgModule({
    declarations: [        
        AdminAllocationComponent,        
        AdminComponent,                                 
        AdminNymAllocationsComponent,
        AdminBaseComponent,
    ],
    exports: [
        AdminAllocationComponent,        
    ],
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,
        AdminRoutingModule,

        SimpleAccordionModule,

        //primeng       
        InputTextModule,
        ColorPickerModule,
        SpinnerModule,
        DropdownModule,
        EditorModule,
        ButtonModule,
        SharedModule,
        TableModule,
        DialogModule,
        CheckboxModule,
        
        //d3s        
        CoreModule,                                      
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,    
        SharedGridPagingInfoModule,
        SharedAssetTypeEditorModule,
        TilesModule,
        DirectivesModule
    ],
    providers: [
    ]
})

export class AdminModule { }