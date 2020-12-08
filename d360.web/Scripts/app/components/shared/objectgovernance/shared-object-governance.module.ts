import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { TreeTableModule } from 'primeng/treetable';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';
import { SocialModule } from '../social/social.module';
import { WorkflowModule } from '../../workflow/workflow.module';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';

import { ObjectBoardComponent } from './object-board.component';
import { ObjectHealthDetailsComponent } from './object-health-details.component';
import { PipesModule } from '../../../pipes/pipes.module';
import { ObjectHealthDetailsItemComponent } from './object-health-details-item.component';
import { TooltipModule } from 'primeng/tooltip';
import { DropdownModule } from 'primeng/dropdown';
import { DirectivesModule } from '../directives/directives.module';
import { FormsModule } from '@angular/forms';
import { PopupMenuModule } from '../controls/popup-menu/popup-menu.component';
import { IgBadgeModule } from '../controls/badge/badge.module';
import { ScoreHistoryComponent } from './score-history.component';
import { CheckboxModule } from 'primeng/checkbox';
import { ScoreCalculationComponent } from './score-calculation.component';
import { ScoreDefinitionComponent } from './score-definition.component';

@NgModule({
    imports: [
        FormsModule,
        CommonModule,
        RouterModule,
        HttpClientModule,
        DirectivesModule,
        //d3s
        CoreModule,
        SharedGridPagingInfoModule,
        SocialModule,
        TilesModule,
        WorkflowModule,
        PipesModule,
        PopupMenuModule,
        IgBadgeModule,
        //prime        
        ButtonModule,
        SharedModule,
        TreeTableModule,
        TooltipModule,
        DropdownModule,
        CheckboxModule

    ],
    declarations: [
        ObjectBoardComponent,
        ObjectHealthDetailsComponent,
        ObjectHealthDetailsItemComponent,
        ScoreHistoryComponent,
        ScoreCalculationComponent,
        ScoreDefinitionComponent
    ],
    exports: [
        ObjectBoardComponent,
        ObjectHealthDetailsComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        }
    ]
})
export class SharedObjectGovernanceModule { }
