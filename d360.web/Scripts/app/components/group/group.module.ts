import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';


import { SharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';

import { GroupComponent } from './group.component';
import { GroupItemComponent } from './group-item.component';
import { GroupListComponent } from './group-list.component';
import { GroupResponsibilityComponent } from './group-responsibility.component';

import {
    GrowlModule,    
    DataTableModule,    
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //primeng
        GrowlModule,        
        DataTableModule,
        

        //d3s
        SharedModule,        
        PipesModule,
    ],
    declarations: [
        GroupComponent,
        GroupItemComponent,
        GroupListComponent,
        GroupResponsibilityComponent,
    ],
    exports: [
        GroupComponent,
        GroupItemComponent,
        GroupListComponent,
        GroupResponsibilityComponent,
    ]
})
export class GroupModule { }