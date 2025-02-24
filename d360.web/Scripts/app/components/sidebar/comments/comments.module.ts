import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';


import { RouterModule } from '@angular/router';

import { ButtonModule } from 'primeng/button';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { CommentsRoutingModule } from './comments.routes';
import { CommentsComponent } from './comments.component';
import { ResourceModule } from '../../resource/resource.module';
import { SocialBoard } from '../../../_shared/components/social-board';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,
        SocialBoard,

        //routing 
        CommentsRoutingModule,

        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        ResourceModule
    ],
    declarations: [
        CommentsComponent
    ],
    providers: [

    ]
})
export class CommentsModule { }