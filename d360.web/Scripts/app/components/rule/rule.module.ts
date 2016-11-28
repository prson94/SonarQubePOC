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
import { SharedAuditModule } from '../shared/audit/shared-audit.module';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDiagramModule } from '../shared/diagram/shared-diagram.module';
import { SharedResponsibilitiesModule } from '../shared/responsibilities/shared-responsibilities.module';

import { RuleRoutingModule } from './rule.routes';

import { RuleComponent } from './rule.component';
import { RuleListComponent } from './rule-list.component';
import { RuleItemComponent } from './rule-item.component';
import { RuleResultsGridComponent } from './rule-results-grid.component';
import { RuleColumnFilterComponent } from './rule-column-filter.component';

import {
    GrowlModule,
    InputTextModule,
    InputMaskModule,
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    AccordionModule,
    SelectButtonModule,
    MultiSelectModule,    
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
        AccordionModule,
        SelectButtonModule,        
        MultiSelectModule,        
        TooltipModule,                
        SharedModule,
                
        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,
        TilesModule,
        SharedAuditModule,
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,
        SharedDiagramModule,
        SharedResponsibilitiesModule,
    ],
    declarations: [
        RuleComponent,
        RuleListComponent,
        RuleItemComponent,
        RuleResultsGridComponent,
        RuleColumnFilterComponent,        
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class RuleModule { }