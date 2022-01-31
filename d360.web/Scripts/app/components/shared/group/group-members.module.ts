import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { RouterModule } from '@angular/router';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';

import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { IconPickerModule } from '../controls/icon-picker/icon-picker.component';
import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { TableModule } from 'primeng/table';
import { GroupMembersComponent } from './group-members.component';
import { DirectivesModule } from '../../../directives/directives.module';
import { SearchFieldModule } from '../controls/search-field/search-field.component';
import { ResourceMultiSelectGridModule } from '../resource-multiselect-grid.component';
import { SiteModalModule } from '../modal/gov-modal.module';
import { SharedDeleteFormModule } from '../delete.form';
import { TooltipModule } from 'primeng/tooltip';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        CoreModule,
        TilesModule,
        SharedGridPagingInfoModule,
        IconPickerModule,
        SearchFieldModule,
        ResourceMultiSelectGridModule,
        SharedDeleteFormModule,
        TooltipModule,
        //prime
        DirectivesModule,
        ButtonModule,
        SharedModule,
        TableModule,
        SiteModalModule
    ],
    declarations: [
        GroupMembersComponent
    ],
    exports: [
        GroupMembersComponent
    ]
})
export class GroupMembersModule { }