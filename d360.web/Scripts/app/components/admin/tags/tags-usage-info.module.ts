import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { RouterModule } from '@angular/router';

import { TagUsageInfoBox } from './tags-usage-info.component';
import { PipesModule } from '../../../pipes/pipes.module';
import { TooltipModule } from 'primeng/tooltip';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,
        PipesModule, 

        //prime
        TooltipModule,
    ],
    declarations: [
        TagUsageInfoBox
    ],
    exports: [
        TagUsageInfoBox,        
    ],
    providers: [

    ]
})
export class TagUsageInfoModule { }