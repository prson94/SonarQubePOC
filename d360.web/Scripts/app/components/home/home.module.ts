import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { SocialModule } from '../social/social.module';
import { WorkflowModule } from '../workflow/workflow.module';
import { SearchModule } from '../search/search.module';

import { HomeComponent} from './home.component';
import { ActivityTile } from './activity-tile.component';
import { ActivityDetailsTile } from './activity-details-tile.component';
import { BoardTile} from './board-tile.component';

import {
    GrowlModule,
    InputTextModule,
    DataTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    AccordionModule,
    SelectButtonModule,
    MultiSelectModule,
    TooltipModule,
    EditorModule,
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //primeng  
        GrowlModule,
        InputTextModule,        
        DataTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,        
        AccordionModule,
        SelectButtonModule,
        MultiSelectModule,        
        TooltipModule,        
        EditorModule,        
        SharedModule,

        //d3s
        D3SSharedModule,
        CoreModule,
        SearchModule,
        SocialModule,
        WorkflowModule,
    ],
    declarations: [
        ActivityDetailsTile,
        ActivityTile,
        BoardTile,
        HomeComponent,        
    ],
})
export class HomeModule { }