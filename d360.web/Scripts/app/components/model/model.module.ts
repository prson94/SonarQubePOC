import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { ChartModule } from 'angular2-highcharts';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';

import { ModelComponent } from './model.component';
import { ModelListComponent } from './model-list.component';
import { ModelItemComponent } from './model-item.component';
import { ModelItemStructureComponent } from './model-item-structure.component';

import {
    GrowlModule,
    InputTextModule,
    InputMaskModule,
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    CalendarModule,    
    AccordionModule,
    SelectButtonModule,    
    MultiSelectModule,    
    TooltipModule,
    PaginatorModule,
    TreeModule,
    FileUploadModule,
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
        CalendarModule,        
        AccordionModule,
        SelectButtonModule,        
        MultiSelectModule,        
        TooltipModule,
        TreeModule,
        PaginatorModule,
        FileUploadModule,
        SharedModule,

        //highcharts
        ChartModule,

        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,
    ],
    declarations: [
        ModelComponent,
        ModelListComponent,
        ModelItemComponent,
        ModelItemStructureComponent,
    ]
})
export class ModelModule { }