import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { PipesModule } from "../../../pipes/pipes.module";
import { CoreModule } from "../core.module";
import { IgColorPickerModule } from "../controls/color-picker/color-picker.module";
import { TagViewModule } from "../tags/d3s-tag-view.module";
import { RelationshipGridComponent } from "./relationship-grid.component";
import { RelationshipFilterComponent } from "./relationship-filter.component";
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
import { CheckboxModule } from 'primeng/checkbox';
import { DirectivesModule } from '../directives/directives.module';
import { IgBadgeModule } from '../controls/badge/badge.module';
import { AssetDetailModule } from '../asset-detail/asset-detail.module';
import { AssetTypeDetailModule } from '../asset-type-detail/asset-type-detail.module';
import { TaggedAssetDetailModule } from '../tagged-assets/tagged-assets-detail.module';
import { SiteModalModule } from '../modal/gov-modal.module';
import { AssetEditorModule } from '../asset-editor/asset-editor.module';


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
        SharedDynamicGridEditorModule,
        CheckboxModule,
        DirectivesModule,
        IgBadgeModule,
        AssetDetailModule,
        AssetTypeDetailModule,
        TaggedAssetDetailModule,
        SiteModalModule,
        AssetEditorModule
    ],
    declarations: [
        RelationshipGridComponent,
        RelationshipFilterComponent
    ],
    exports: [
        RelationshipGridComponent
    ],
    providers: []
})
export class RelationshipGridModule { }