import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { AdminModule } from '../admin.module';

import { IgMessageBoxModule } from '../../shared/controls/message-box/message-box.module';

import { CodemirrorModule } from '@ctrl/ngx-codemirror';

import { SharedModule } from 'primeng/api';
import { AdminBrandingComponent } from './admin-branding.component';
import { AdminBrandingRoutingModule } from './admin-branding.routes';
import { SidePanelModule } from '../../shared/sidepanel/side-panel.module';
import { DirectivesModule } from '../../../directives/directives.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        AdminBrandingRoutingModule,

        //code editor
        CodemirrorModule,

        //prime
        SharedModule,

        //d3s        
        CoreModule,
        PipesModule,        
        TilesModule,
        AdminModule,

        IgMessageBoxModule,
        SidePanelModule,
        DirectivesModule
    ],
    declarations: [
        AdminBrandingComponent
    ],
    providers: [
    ]
})
export class AdminBrandingModule { }