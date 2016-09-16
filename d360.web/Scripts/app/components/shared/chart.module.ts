import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';

import { CHART_DIRECTIVES } from 'angular2-highcharts';

@NgModule({
    declarations: [        
        CHART_DIRECTIVES,
    ],
    exports: [
        CHART_DIRECTIVES,
    ]
    , imports: [
        CommonModule,
        FormsModule,
    ]

})

export class ChartModule { }