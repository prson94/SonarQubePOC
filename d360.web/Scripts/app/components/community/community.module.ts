import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';

import { CommunityComponent } from './community.component';
import { CommunitySummaryComponent } from './community-summary.component';


import {
    GrowlModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //primeng
        GrowlModule,
        

        //d3s
        D3SSharedModule,
        CoreModule,
        PipesModule,
    ],
    declarations: [
        CommunityComponent,
        CommunitySummaryComponent,        
    ],
    exports: [
        CommunityComponent,
        CommunitySummaryComponent,        
    ]
})
export class CommunityModule { }