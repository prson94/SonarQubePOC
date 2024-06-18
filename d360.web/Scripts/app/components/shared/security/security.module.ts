import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { SharedModule } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { CoreModule } from '../core.module';
import { FormFeedbackBadgesModule } from '../controls/form-feedback-badges/form-feedback-badges.component';
import { IgMessageBoxModule } from '../controls/message-box/message-box.module';
import { SiteModalModule } from '../modal/gov-modal.module';

import { OwnerForm } from './modals/owner-form';
import { OwnerList } from './owner-list';
import { TableModule } from 'primeng/table';
import { SearchFieldModule } from '../controls/search-field/search-field.component';
import { PopupMenuModule } from '../controls/popup-menu/popup-menu.component';
import { OwnerDelete } from './modals/owner-delete';
import { OwnersSidePanelWrapper } from './components/owners-sidepanel-wrapper';
import { OwnerDetail } from './components/owner-detail';
import { SidePanelModule } from '../sidepanel/side-panel.module';
import { AngularSplitModule } from 'angular-split';
import { PropertyGroupModule } from '../controls/property-group/property-group.component';


@NgModule({
    imports: [
        CommonModule,
		FormsModule,
		ReactiveFormsModule,

        //prime
        ButtonModule,
		DropdownModule,
		TableModule,
		SharedModule,

		CoreModule,
		FormFeedbackBadgesModule,
		IgMessageBoxModule,
		PopupMenuModule,
		SearchFieldModule,
		SidePanelModule,
		SiteModalModule,
		PropertyGroupModule,
		AngularSplitModule
    ],
    declarations: [
		OwnerDelete,
		OwnerDetail,
		OwnerForm,
		OwnerList,
		OwnersSidePanelWrapper
    ],
    exports: [
		OwnerDelete,
		OwnerDetail,
		OwnerForm,
		OwnerList,
		OwnersSidePanelWrapper
    ],
    providers: [

    ]
})
export class SecurityModule { }