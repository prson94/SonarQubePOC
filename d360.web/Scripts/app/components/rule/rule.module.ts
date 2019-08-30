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
import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';
import { SharedAssetEditorsModule } from '../shared/asseteditors/shared-asset-editor.module';

import { RuleRoutingModule } from './rule.routes';

import { RuleComponent } from './rule.component';
import { RuleListComponent } from './rule-list.component';
import { RuleItemComponent } from './rule-item.component';
import { RuleImplementationComponent } from './rule-implementation.component';
import { RuleResultsGridComponent } from './rule-results-grid.component';
import { RuleColumnFilterComponent } from './rule-column-filter.component';
import { RuleImplementationsGridComponent } from './rule-implementations-grid.component';
import { RuleQualifierGridComponent } from './rule-qualifier-grid.component';
import { RuleQualifierEditorComponent } from './rule-qualifier-editor.component';
import { RuleQualifiersComponent } from './rule-qualifiers.component';
import { RuleImplementationSummaryComponent } from './rule-implementation-summary.component';

import {
    GrowlModule,
    InputTextModule,
    InputMaskModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,    
    SelectButtonModule,
    MultiSelectModule,   
    TabViewModule,
    TooltipModule,
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';
import { SharedObjectGovernanceModule } from '../shared/objectgovernance/shared-object-governance.module';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        RuleRoutingModule,

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
        TabViewModule,
        TooltipModule,                
        SharedModule,
        TableModule,
                
        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,
        TilesModule,
        
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,        
        SharedDynamicGridEditorModule,
        SharedObjectDetailsModule,
        SharedObjectGovernanceModule,
        SharedAssetEditorsModule,
    ],
    declarations: [
        RuleComponent,
        RuleListComponent,
        RuleItemComponent,
        RuleImplementationComponent,
        RuleImplementationsGridComponent,
        RuleImplementationSummaryComponent,
        RuleResultsGridComponent,
        RuleColumnFilterComponent,   
        RuleQualifierGridComponent,
        RuleQualifierEditorComponent,
        RuleQualifiersComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class RuleModule { }