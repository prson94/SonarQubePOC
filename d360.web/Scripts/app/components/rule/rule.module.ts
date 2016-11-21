import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';


import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';

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
    SpinnerModule,
    TooltipModule,
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

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
        SpinnerModule,
        TooltipModule,                
        SharedModule,
                
        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,
    ],
    declarations: [
        RuleComponent,
        RuleListComponent,
        RuleItemComponent,
        RuleResultsGridComponent,
        RuleColumnFilterComponent,        
    ]
})
export class RuleModule { }