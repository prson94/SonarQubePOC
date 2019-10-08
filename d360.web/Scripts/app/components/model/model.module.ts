import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";
import { RouterModule } from '@angular/router';


import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedAssetEditorsModule } from '../shared/asseteditors/shared-asset-editor.module';
import { ModelRoutingModule } from './model.routes';

import { ModelComponent } from './model.component';
import { ModelListComponent } from './model-list.component';
import { ModelItemComponent } from './model-item.component';
import { ModelItemStructureComponent } from './model-item-structure.component';

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/shared';
import { InputTextModule } from 'primeng/inputtext';
import { InputMaskModule } from 'primeng/inputmask';
import { DropdownModule } from 'primeng/dropdown';
import { MultiSelectModule } from 'primeng/multiselect';
import { TooltipModule } from 'primeng/tooltip';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TreeTableModule } from 'primeng/treetable';
import { GrowlModule } from 'primeng/growl';
import { TreeModule } from 'primeng/tree';
import { TableModule } from 'primeng/table';

import { SharedObjectGovernanceModule } from '../shared/objectgovernance/shared-object-governance.module';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        ModelRoutingModule,

        //primeng
        GrowlModule,
        InputTextModule,
        InputMaskModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,        
        SelectButtonModule,        
        MultiSelectModule,        
        TooltipModule,
        TreeModule,        
        SharedModule,
        TableModule,


        
        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,  
              
        SharedGridPagingInfoModule,        
        SharedDeleteFormModule,
        SharedObjectGovernanceModule,
        SharedDynamicGridEditorModule,
        SharedAssetEditorsModule,
        TilesModule,
    ],
    declarations: [
        ModelComponent,
        ModelListComponent,
        ModelItemComponent,
        ModelItemStructureComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class ModelModule { }