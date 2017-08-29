import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectGovernanceModule } from '../shared/objectgovernance/shared-object-governance.module';

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
    DataTableModule,
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

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        RuleRoutingModule,

        //primeng
        GrowlModule,
        InputTextModule,
        InputMaskModule,
        DataTableModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,                        
        SelectButtonModule,        
        MultiSelectModule, 
        TabViewModule,
        TooltipModule,                
        SharedModule,
                
        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,
        TilesModule,
        
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,        
        SharedDynamicGridEditorModule,
        SharedObjectGovernanceModule,        
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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class RuleModule { }