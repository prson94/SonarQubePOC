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
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedAssetEditorsModule } from '../shared/asseteditors/shared-asset-editor.module';
import { SharedAssetTypeEditorModule } from '../shared/assettypeeditor/shared-asset-type-editor.module';

import { PolicyRoutingModule } from './policy.routes';

import { PolicyComponent } from './policy.component';
import { PolicyItemComponent } from './policy-item.component';
import { PolicyItemStructureComponent } from './policy-item-structure.component';
import { PolicyListComponent } from './policy-list.component';

import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/shared';
import { InputTextModule } from 'primeng/inputtext';
import { InputMaskModule } from 'primeng/inputmask';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';
import { MultiSelectModule } from 'primeng/multiselect';
import { TooltipModule } from 'primeng/tooltip';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TreeTableModule } from 'primeng/treetable';
import { GrowlModule } from 'primeng/growl';
import { TreeModule } from 'primeng/tree';

import { SharedObjectGovernanceModule } from '../shared/objectgovernance/shared-object-governance.module';
import { AssetEditorModule } from '../shared/asseteditors/asset-editor.module';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        PolicyRoutingModule,

        //primeng
        GrowlModule,
        InputTextModule,
        InputMaskModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,                
        SelectButtonModule,        
        MultiSelectModule,        
        TooltipModule,
        TreeModule,                
        SharedModule,
        TableModule,

        //d3s
        D3SSharedModule,
        CoreModule,        
        PipesModule,  
              
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,        
        SharedDynamicGridEditorModule,        
        SharedAssetTypeEditorModule,     
        SharedAssetEditorsModule,
        TilesModule,
        SharedObjectGovernanceModule,
        AssetEditorModule
    ],
    declarations: [
        PolicyComponent,
        PolicyItemComponent,
        PolicyItemStructureComponent,
        PolicyListComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class PolicyModule { }
