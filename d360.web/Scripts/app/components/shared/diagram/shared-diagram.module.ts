import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { RouterModule } from '@angular/router';


import { AutoCompleteModule } from 'primeng/autocomplete';
import { CheckboxModule } from 'primeng/checkbox';
import { InputSwitchModule } from 'primeng/inputswitch';
import { ButtonModule } from 'primeng/button';
import { ContextMenuModule } from 'primeng/contextmenu';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { SharedModule } from 'primeng/api';
import { TableModule } from 'primeng/table';
import { EditorModule } from 'primeng/editor';
import { ListboxModule } from 'primeng/listbox';
import { DropdownModule } from 'primeng/dropdown';
import { MenubarModule } from 'primeng/menubar';
import { MenuModule } from 'primeng/menu';
import { ScrollPanelModule } from 'primeng/scrollpanel';
import { TabViewModule } from 'primeng/tabview';
import { TreeModule } from 'primeng/tree';
import { ScoreBadgeModule } from '../small-widgets/score-badge/score-badge.module';
import { AccordionModule } from 'primeng/accordion';

import { AssetBrowserAddPanelComponent } from './assetbrowser/panels/addpanel.component';
import { AssetBrowserAlertPanelComponent } from './assetbrowser/panels/alertpanel.component';
import { AssetBrowserFilterPanelComponent } from './assetbrowser/panels/filterpanel.component';
import { AssetBrowserInfoPanelComponent } from './assetbrowser/panels/infopanel.component';
import { AssetBrowserSettingsPanelComponent } from './assetbrowser/panels/settingspanel.component';

import { AssetBrowserOverviewComponent } from './assetbrowser/tools/overview.component';
import { AssetBrowserPanelsComponent } from './assetbrowser/tools/panels.component';
import { AssetBrowserSavedFilterComponent } from './assetbrowser/tools/savedfilter.component';
import { AssetBrowserSearchComponent } from './assetbrowser/tools/search.component';
import { AssetBrowserViewChangeComponent } from './assetbrowser/tools/viewchange.component';
import { AssetBrowserZoomComponent } from './assetbrowser/tools/zoom.component';

import { AssetBrowserComponent } from './assetbrowser/browser.component';

import { ModelDiagramComponent } from './model-diagram.component';
import { D3SOverlayWindowModule } from '../overlay-window.component';

import { CoreModule } from '../core.module';
import { TilesModule  } from '../tiles/tiles.module';
import { SharedDeleteFormModule } from '../delete.form';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { SharedFormMessageModule } from '../form-message.part'
import { SharedObjectDetailsModule } from '../objectdetails/shared-object-details.module';
import { NgxJsonViewModule } from 'ng-json-view';
import { IconService } from '../../../services/icon.service';
import { TagViewModule } from '../tags/d3s-tag-view.module';
import { AssetTypeModalEditorModule } from '../assetmodaleditor/asset-type-modal-editor.module';
import { SiteModalModule } from '../modal/gov-modal.module';
import { AssetSearchModule } from '../search/generic-asset-search/generic-asset-search.module';
import { SharedAssetTypeEditorModule } from '../assettypeeditor/shared-asset-type-editor.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { ProcessDiagramComponent } from './process-diagram/process-diagram.component';
import { DragDropModule } from 'primeng/dragdrop';
import { IgColorPickerModule } from '../controls/color-picker/color-picker.module';
import { ProcessDiagramSidePanelComponent } from './process-diagram/process-diagram-side-panel.component';
import { SharedRelationshipModule } from '../relationship/shared-relationship.module';
import { SharedDynamicGridEditorModuleV2 } from '../dynamicgrideditor-v2/shared-dynamic-grid-editor-v2.module';
import { ProcessDiagramLabelEditorComponent } from './process-diagram/process-diagram-label-editor.component';
import { DynEditorService } from '../../../services/dyn-editor.service';
import { ProcessDiagramListViewComponent } from './process-diagram/process-diagram-list-view.component';
import { TooltipModule } from 'primeng/tooltip';
import { PopupMenuModule } from '../controls/popup-menu/popup-menu.component';
import { SearchFieldModule } from '../controls/search-field/search-field.component';
import { DirectivesModule } from '../../../directives/directives.module';
import { SharedResponsibilitiesModule } from '../responsibilities/shared-responsibilities.module';
import { AssetDetailModule } from '../asset-detail/asset-detail.module';
import { SidePanelModule } from '../sidepanel/side-panel.module';
import { AssetEditorModule } from '../asset-editor/asset-editor.module';
import { AssetTypeDetailModule } from '../asset-type-detail/asset-type-detail.module';
import { TaggedAssetDetailModule } from '../tagged-assets/tagged-assets-detail.module';


@NgModule({
    imports: [CommonModule,
        FormsModule,

        RouterModule,
        //d3s
        CoreModule,
        PipesModule,
        SharedDeleteFormModule,
        SharedFormMessageModule,
        SharedGridPagingInfoModule,
        TilesModule,
        D3SOverlayWindowModule,
        SharedObjectDetailsModule,
        SharedAssetTypeEditorModule,
        AssetTypeModalEditorModule,
        SiteModalModule,
        ScoreBadgeModule,
        IgColorPickerModule,
        SharedDynamicGridEditorModuleV2,
        SiteModalModule,
        SharedRelationshipModule,
        SharedObjectDetailsModule,
        SharedResponsibilitiesModule,
        AssetDetailModule,
        SidePanelModule,
        AssetEditorModule,
        DirectivesModule,
        AssetTypeDetailModule,
        TaggedAssetDetailModule,
        //prime        
        CheckboxModule,
        ContextMenuModule,
        EditorModule,     
        InputSwitchModule, 
        SharedModule,
        AutoCompleteModule,
        ButtonModule,
        InputTextareaModule,
        ListboxModule,
        DropdownModule,
        MenubarModule,
        ScrollPanelModule,
        MenuModule,
        TableModule,
        TabViewModule,
        TreeModule,
        DragDropModule,
        AccordionModule,
        TooltipModule,
        //JSON Viewer module
        NgxJsonViewModule,
        AssetSearchModule,
        TagViewModule,
        PopupMenuModule,
        SearchFieldModule,
        DirectivesModule
    ],
    declarations: [
        AssetBrowserAddPanelComponent,
        AssetBrowserAlertPanelComponent,
        AssetBrowserFilterPanelComponent,
        AssetBrowserInfoPanelComponent,
        AssetBrowserSettingsPanelComponent,

        AssetBrowserOverviewComponent,
        AssetBrowserPanelsComponent,
        AssetBrowserSavedFilterComponent,
        AssetBrowserSearchComponent,
        AssetBrowserViewChangeComponent,
        AssetBrowserZoomComponent,
        
        AssetBrowserComponent,
        ModelDiagramComponent,
        ProcessDiagramComponent,
        ProcessDiagramSidePanelComponent,
        ProcessDiagramLabelEditorComponent,
        ProcessDiagramListViewComponent
    ],
    exports: [
        AssetBrowserComponent, 
        ModelDiagramComponent,  
    ],
    providers: [
        
        IconService,
        DynEditorService
    ]
})
export class SharedDiagramModule { }