import { NgModule } from '@angular/core'
import { CommonModule } from '@angular/common'
import { AssignmentsContainerComponent } from './assignments-container.component'
import { AssignmentListComponent } from './assignment-list/assignment-list.component'
import { RouterModule } from '@angular/router'
import { AssignmentsRoutingModule } from './assignments.routes'
import { AdvancedFiltersModule } from '../assets-grid/advanced-filtering/advanced-filtering.module'
import { AngularSplitModule } from 'angular-split'
import { AssetDetailModule } from '../shared/asset-detail/asset-detail.module'
import { ButtonModule } from '../../directives/ig-button-directive'
import { CheckboxModule } from 'primeng/checkbox'
import { D3SSortIconModule } from '../shared/turbotable-sorticon.component'
import { DirectivesModule } from '../../directives/directives.module'
import { IgBadgeModule } from '../shared/controls/badge/badge.module'
import { PopupMenuModule } from '../shared/controls/popup-menu/popup-menu.component'
import { SearchFieldModule } from '../shared/controls/search-field/search-field.component'
import { SemanticsModule } from '../semantic/semantics.module'
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component'
import { SharedModule } from 'primeng/api'
import { SidePanelModule } from '../shared/sidepanel/side-panel.module'
import { TableModule } from 'primeng/table'
import { TooltipModule } from 'primeng/tooltip'
import { FormsModule } from '@angular/forms'


@NgModule({
	declarations: [
		AssignmentsContainerComponent,
		AssignmentListComponent
	],
    imports: [
        CommonModule,
        RouterModule,
        AssignmentsRoutingModule,
        AdvancedFiltersModule,
        AngularSplitModule,
        AssetDetailModule,
        ButtonModule,
        CheckboxModule,
        D3SSortIconModule,
        DirectivesModule,
        IgBadgeModule,
        PopupMenuModule,
        SearchFieldModule,
        SemanticsModule,
        SharedGridPagingInfoModule,
        SharedModule,
        SidePanelModule,
        TableModule,
        TooltipModule,
        FormsModule
    ]
})
export class AssignmentsModule {
}
