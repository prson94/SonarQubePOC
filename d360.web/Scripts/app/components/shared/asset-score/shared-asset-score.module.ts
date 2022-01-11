import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';



import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { TreeTableModule } from 'primeng/treetable';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';
import { SocialModule } from '../social/social.module';
import { WorkflowModule } from '../../workflow/workflow.module';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { PipesModule } from '../../../pipes/pipes.module';
import { TooltipModule } from 'primeng/tooltip';
import { DropdownModule } from 'primeng/dropdown';
import { DirectivesModule } from '../directives/directives.module';
import { FormsModule } from '@angular/forms';
import { PopupMenuModule } from '../controls/popup-menu/popup-menu.component';
import { IgBadgeModule } from '../controls/badge/badge.module';
import { CheckboxModule } from 'primeng/checkbox';
import { AssetScoreComponent } from './asset-score.component';
import { ScoreCalculationModule } from './calculation/score-calculation.module';
import { ScoreDefinitionModule } from './definition/score-definition.module';
import { ScoreHistoryModule } from './history/score-history.module';



@NgModule({
    imports: [
        FormsModule,
        CommonModule,
        RouterModule,

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
        CheckboxModule,

        ScoreCalculationModule,
        ScoreDefinitionModule,
        ScoreHistoryModule
    ],
    declarations: [
        AssetScoreComponent
    ],
    exports: [
        AssetScoreComponent
    ],
    providers: [
        
    ]
})
export class SharedAssetScoreModule { }
