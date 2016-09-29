import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';

import { TooltipComponent } from './tooltip.component';
import { LoadingComponent } from './loading.component';

@NgModule({
    declarations: [        
        TooltipComponent,
        LoadingComponent,        
    ],
    exports: [        
        TooltipComponent,        
        LoadingComponent,
    ]
    , imports: [
        CommonModule        
    ]

})

export class CoreModule { }