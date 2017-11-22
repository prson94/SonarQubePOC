import { NgModule }       from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { TooltipComponent } from './tooltip.component';
import { PreviewTooltipComponent } from './preview-tooltip.component';
import { LookupTooltipComponent } from './lookup-tooltip.component';
import { LoadingComponent } from './loading.component';

@NgModule({
    declarations: [        
        TooltipComponent,
        PreviewTooltipComponent,
        LoadingComponent,
        LookupTooltipComponent,
    ],
    exports: [        
        TooltipComponent,        
        PreviewTooltipComponent,
        LoadingComponent,
        LookupTooltipComponent,
    ]
    , imports: [
        CommonModule,
        RouterModule      
    ]

})

export class CoreModule { }