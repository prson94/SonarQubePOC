import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { TooltipModule } from "primeng/tooltip";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { DropdownModule } from "primeng/dropdown";
import { AdvancedFilteringComponent } from "./advanced-filtering.component";
import { DirectivesModule } from "../../../directives/directives.module";
import { IgBadgeModule } from "../../shared/controls/badge/badge.module";
import { IgDateModule } from "../../shared/controls/date/date";
import { IgNumberFieldModule } from "../../shared/controls/number-picker/number-input.component";
import { FilterItemComponent } from "./filter-item.component";
import { TableModule } from "primeng/table";
import { SearchFieldModule } from "../../shared/controls/search-field/search-field.component";
import { RadioButtonModule } from "primeng/radiobutton";
import { CoreModule } from "../../shared/core.module";
import { MultiInputFieldModule } from "../../shared/controls/multi-input-field/multi-input-field.component";
import { PopupMenuModule } from "../../shared/controls/popup-menu/popup-menu.component";
import { OverlayPanelModule } from "primeng/overlaypanel";
import { FocusTrapModule } from "primeng/focustrap";
import { DatePipe } from '@angular/common';

@NgModule({
    imports: [
        CommonModule,
        CoreModule,
        TooltipModule,
        FormsModule,
        ReactiveFormsModule,
        IgBadgeModule,
        IgDateModule,
        IgNumberFieldModule,
        TableModule,
        SearchFieldModule,
        RadioButtonModule,

        DropdownModule,
        DirectivesModule,
        MultiInputFieldModule,
        PopupMenuModule,
        OverlayPanelModule,
        FocusTrapModule
    ],
    declarations: [AdvancedFilteringComponent, FilterItemComponent],
    exports: [AdvancedFilteringComponent],
    providers: [DatePipe]
})
export class AdvancedFiltersModule { }
