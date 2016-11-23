import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';

import { GroupRoutingModule } from './group.routes';

import { GroupComponent } from './group.component';
import { GroupItemComponent } from './group-item.component';
import { GroupListComponent } from './group-list.component';
import { GroupResponsibilityComponent } from './group-responsibility.component';

import {
    GrowlModule,    
    DataTableModule,    
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        GroupRoutingModule,

        //primeng
        GrowlModule,        
        DataTableModule,
        SharedModule,
        
        //d3s
        D3SSharedModule,        
        CoreModule,
        PipesModule,
        TilesModule,
    ],
    declarations: [
        GroupComponent,
        GroupItemComponent,
        GroupListComponent,
        GroupResponsibilityComponent,
    ]
})
export class GroupModule { }