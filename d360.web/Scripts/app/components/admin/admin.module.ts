import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { RouterModule } from '@angular/router';

import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';      
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";

import { AdminRoutingModule } from './admin.routes';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedAssetTypeEditorModule } from '../shared/assettypeeditor/shared-asset-type-editor.module';

import { SharedModule } from 'primeng/shared';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { ColorPickerModule } from 'primeng/colorpicker';
import { SpinnerModule } from 'primeng/spinner';
import { EditorModule } from 'primeng/editor';
import { TableModule } from 'primeng/table';

import { AdminAllocationComponent } from './admin-allocation.component';
import { AdminClassificationsComponent } from './admin-classifications.component';
import { AdminComponent } from './admin.component';
import { AdminLevelEditorComponent } from './admin-level-editor.component';
import { AdminLevelListComponent } from './admin-level-list.component';
import { AdminNymAllocationsComponent } from './admin-nym-allocations.component';
import { SimpleAccordionModule } from "../shared/simple-accordion.part";
import { DialogModule } from 'primeng/dialog';


@NgModule({
    declarations: [        
        AdminAllocationComponent,
        AdminClassificationsComponent,
        AdminComponent,                
        AdminLevelListComponent,
        AdminLevelEditorComponent,   
        AdminNymAllocationsComponent     
    ],
    exports: [
        AdminAllocationComponent,
        AdminClassificationsComponent,
        AdminLevelListComponent,
    ],
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
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
        
        //d3s        
        CoreModule,                                      
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,    
        SharedGridPagingInfoModule,
        SharedAssetTypeEditorModule,
        TilesModule,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})

export class AdminModule { }