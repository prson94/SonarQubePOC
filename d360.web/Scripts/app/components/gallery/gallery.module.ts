import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from '../../http-interceptors/govern-request.interceptor';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { IconPickerModule } from '../shared/controls/icon-picker/icon-picker.component';
import { SwitchModule } from '../shared/controls/switch/switch'
import { CoreModule } from '../shared/core.module';
import { IgDateModule } from '../shared/controls/date/date';

import { GalleryComponent } from './gallery.component';
import { GalleryRoutingModule } from './gallery.routes';

import { GallerySwitchComponent } from './gallery.switch.component';
import { GalleryButtonComponent } from './gallery.button.component';
import { GalleryIconPickerComponent } from './gallery.icon-picker.component';
import { GalleryTagPickerComponent } from './gallery.tag-picker.component';
import { GalleryGuard } from '../../guards/gallery.guard';

import { TableModule } from 'primeng/table';
import { TagPickerModule } from '../shared/controls/tag-picker/tag-picker';
import { GalleryInputComponent } from './gallery.input.component';
import { GalleryColorPickerComponent } from './gallery.color-picker.component';
import { IgColorPickerModule } from '../shared/controls/color-picker/color-picker.module';
import { GalleryAutocompleteComponent } from './gallery.autocomplete.component';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { GalleryColorVariablesComponent } from './gallery.color-variables.component';
import { GalleryTooltipComponent } from './gallery.tooltip.component';
import { TooltipModule } from 'primeng/tooltip';
import { GalleryTextAreaComponent } from './gallery.textarea.component';
import { GalleryAutoFocusComponent } from './gallery.autofocus.component';
import { SiteModalModule } from '../shared/modal/gov-modal.module';
import { GalleryDatePickerComponent } from './gallery.date-picker.component';
import { GalleryLoadingComponent } from './gallery.loading.component';
import { GalleryAccordionComponent } from './gallery.accordion.component';
import { GalleryGridPagingInfoComponent } from './gallery.grid-paging-info.component';
import { GalleryGridSelectionInfoComponent } from './gallery.grid-selection-info.component';
import { SimpleAccordionModule } from '../shared/simple-accordion.part';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedGridSelectionInfoModule } from '../shared/grid-selection-info.component';
import { GalleryNumberFieldComponent } from './gallery.number-field.component';
import { IgNumberFieldModule } from '../shared/controls/number-picker/number-input.component';
import { GalleryMessageBoxComponent } from './gallery.message-box.component';
import { IgMessageBoxModule } from '../shared/controls/message-box/message-box.module';
import { CheckboxModule } from 'primeng/checkbox';
import { TriStateCheckboxModule } from 'primeng/tristatecheckbox';
import { GalleryCheckboxComponent } from './gallery.checkbox.component';
import { IgBadgeModule } from '../shared/controls/badge/badge.module';
import { GalleryBadgeComponent } from './gallery.badge.component';
import { GalleryPopupMenuComponent } from './gallery.popup-menu.component';
import { PopupMenuModule } from '../shared/controls/popup-menu/popup-menu.component';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { GalleryDropDownComponent } from './gallery.dropdown.component';
import { DropdownModule } from 'primeng/dropdown';
import { GalleryPropertyGroupComponent } from './gallery.propery-group.component';
import { PropertyGroupModule } from '../shared/controls/property-group/property-group.component';
import { GalleryRadioButtonComponent } from './gallery.radio-button.component';
import { RadioButtonModule } from 'primeng/radiobutton';
import { GalleryFieldConditionGridComponent } from './gallery.field-condition-grid.component';
import { FieldConditionGridModule } from '../shared/controls/field-condition-grid/field-condition-grid.module';
import { SearchFieldModule } from '../shared/controls/search-field/search-field.component';
import { GallerySearchFieldComponent } from './gallery.search-field.component';
import { GalleryMultiInputFieldComponent } from './gallery.multi-input-field.component';
import { MultiInputFieldModule } from '../shared/controls/multi-input-field/multi-input-field.component';
import { GalleryInputGroupComponent } from './gallery.input-group.component';
import { GalleryBackButtonComponent } from './gallery.back-button.component';

@NgModule({
    imports: [
        CommonModule,
        HttpClientModule,
        RouterModule,
        CoreModule,
        FormsModule,
        ReactiveFormsModule,

        GalleryRoutingModule,

        PropertyGroupModule,
        SwitchModule,
        IconPickerModule,
        TagPickerModule,
        IgColorPickerModule,
        IgDateModule,
        SearchFieldModule,
        SimpleAccordionModule,
        SharedGridSelectionInfoModule,
        SharedGridPagingInfoModule,
        IgNumberFieldModule,
        IgMessageBoxModule,
        IgBadgeModule,

        TableModule,
        TooltipModule,
        AutoCompleteModule,
        CheckboxModule,
        TriStateCheckboxModule,
        SiteModalModule,
        PopupMenuModule,
        OverlayPanelModule,
        DropdownModule,
        RadioButtonModule,
        FieldConditionGridModule,
        MultiInputFieldModule
    ],
    declarations: [
        GalleryComponent,
        GallerySwitchComponent,
        GalleryButtonComponent,
        GalleryIconPickerComponent,
        GalleryTagPickerComponent,
        GalleryInputComponent,
        GalleryColorPickerComponent,
        GalleryColorVariablesComponent,
        GalleryTooltipComponent,
        GalleryAutocompleteComponent,
        GalleryTextAreaComponent,
        GalleryAutoFocusComponent,
        GalleryDatePickerComponent,
        GalleryLoadingComponent,
        GalleryAccordionComponent,
        GalleryGridPagingInfoComponent,
        GalleryGridSelectionInfoComponent,
        GalleryNumberFieldComponent,
        GalleryBadgeComponent,
        GalleryMessageBoxComponent,
        GalleryPopupMenuComponent,
        GalleryCheckboxComponent,
        GalleryDropDownComponent,
        GalleryCheckboxComponent,
        GalleryPropertyGroupComponent,
        GalleryFieldConditionGridComponent,
        GalleryRadioButtonComponent,
        GallerySearchFieldComponent,
        GalleryMultiInputFieldComponent,
        GalleryInputGroupComponent,
        GalleryBackButtonComponent
    ],
    providers: [
        GalleryGuard,
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class GalleryModule { }