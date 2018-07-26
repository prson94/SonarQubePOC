import { NgModule }       from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { TooltipComponent } from './tooltip.component';
import { PreviewTooltipComponent } from './preview-tooltip.component';
import { LookupTooltipComponent } from './lookup-tooltip.component';
import { LoadingComponent } from './loading.component';
import { D3STreeTableToggler } from './treetable-toggler.component';

@NgModule({
    declarations: [        
        TooltipComponent,
        PreviewTooltipComponent,
        LoadingComponent,
        LookupTooltipComponent,
        D3STreeTableToggler,
    ],
    exports: [        
        TooltipComponent,        
        PreviewTooltipComponent,
        LoadingComponent,
        LookupTooltipComponent,
        D3STreeTableToggler,
    ]
    , imports: [
        CommonModule,
        RouterModule      
    ]

})

export class CoreModule { }