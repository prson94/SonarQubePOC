import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';


import { ButtonModule } from 'primeng/button';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { RuleResultsRoutingModule } from './rule-results.routes';
import { RuleResultsComponent } from './rule-results.component';
import { GovernRequestInterceptor } from '../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { RuleModule } from '../../rule/rule.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,

        //routing 
        RuleResultsRoutingModule,

        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        RuleModule,
    ],
    declarations: [
        RuleResultsComponent
    ],
    providers: [
        
    ]
})
export class RuleResultsModule { }