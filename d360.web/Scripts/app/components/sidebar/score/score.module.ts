import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';


import { ButtonModule } from 'primeng/button';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { ScoreRoutingModule } from './score.routes';
import { ScoreComponent } from './score.component';
import { SharedAssetScoreModule } from '../../shared/asset-score/shared-asset-score.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,        
        RouterModule,

        //routing 
        ScoreRoutingModule,

        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        SharedAssetScoreModule,
    ],
    declarations: [
        ScoreComponent
    ],
    providers: [
        
    ]
})
export class ScoreModule { }