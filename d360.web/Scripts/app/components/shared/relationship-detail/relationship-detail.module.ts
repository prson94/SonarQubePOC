import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { PipesModule } from "../../../pipes/pipes.module";
import { CoreModule } from "../core.module";
import { IgColorPickerModule } from "../controls/color-picker/color-picker.module";
import { TagViewModule } from "../tags/d3s-tag-view.module";
import { RelationshipDetailComponent } from "./relationship-detail.component";
import { SharedObjectDetailsModule } from "../objectdetails/shared-object-details.module";
import { PropertyGroupModule } from "../controls/property-group/property-group.component";
import { TilesModule } from "../tiles/tiles.module";
import { TableModule } from "primeng/table";
import { AdvancedFiltersModule } from "../../assets-grid/advanced-filtering/advanced-filtering.module";
import { SearchFieldModule } from "../controls/search-field/search-field.component";
import { PortalsModule } from "../portals/portals.module";
import { SidePanelModule } from '../sidepanel/side-panel.module';
import { PopupMenuModule } from '../controls/popup-menu/popup-menu.component';
import { SharedDynamicGridEditorModule } from '../dynamicgrideditor/shared-dynamic-grid-editor.module';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        CoreModule,
        PipesModule,
        IgColorPickerModule,
        PropertyGroupModule,
        TagViewModule,
        SharedObjectDetailsModule,
        TilesModule,
        TableModule,
        AdvancedFiltersModule,
        SearchFieldModule,
        PortalsModule,
        SidePanelModule,
        PopupMenuModule,
        SharedDynamicGridEditorModule
    ],
    declarations: [
        RelationshipDetailComponent
    ],
    exports: [
        RelationshipDetailComponent
    ],
    providers: []
})
export class RelationshipDetailModule { }