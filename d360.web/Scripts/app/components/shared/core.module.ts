import { NgModule }       from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { TooltipComponent } from './tooltip.component';
import { PreviewTooltipComponent } from './preview-tooltip.component';
import { LookupTooltipComponent } from './lookup-tooltip.component';
import { LoadingComponent } from './loading.component';
import { D3STreeTableToggler } from './treetable-toggler.component';
import { D3SColumnFilterModule } from './turbotable-column-filter.component';
import { D3SSortIconModule } from './turbotable-sorticon.component';
import { DirectivesModule } from '../../directives/directives.module';


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
        D3SSortIconModule,
        D3SColumnFilterModule,
    ]
    , imports: [
        CommonModule,
        RouterModule,
        DirectivesModule,
    ]

})

export class CoreModule { }