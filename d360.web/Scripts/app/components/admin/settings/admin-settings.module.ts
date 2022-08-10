import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { ShortcutModule } from '../../shared/shortcuts/shortcut.module';
import { HelpMenuModule } from '../../shared/helpmenu/helpmenu.module';
import { IconPickerModule } from '../../shared/controls/icon-picker/icon-picker.component';
import { PipesModule } from '../../../pipes/pipes.module';
import { DayOfWeekInputModule } from "../../shared/small-widgets/dayofweek-input/dayofweek-input.component";
import { IgMessageBoxModule } from '../../shared/controls/message-box/message-box.module';
import { IgNumberFieldModule } from "../../shared/controls/number-picker/number-input.component";

import { AdminSettingsComponent } from './admin-settings.component';
import { AdminSiteMenuComponent } from './admin-site-menu.component';
import { AdminIpRestrictionComponent } from './admin-ip-restriction.component';
import { AdminSiteMenuFolderEditorComponent } from './admin-site-menu-folder-editor.component';
import { AdminSiteMenuDeleteDialogComponent } from './admin-site-menu-delete-dialog.component';
import { AdminHomeComponent } from './admin-home.component';

import { AdminSettingsRoutingModule } from './admin-settings.routes';
import { D3SSharedModule } from '../../shared/shared.module';

import { SharedModule } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { ColorPickerModule } from 'primeng/colorpicker';
import { TableModule } from 'primeng/table';
import { CheckboxModule } from 'primeng/checkbox';
import { IgCheckboxModule } from '../../../directives/ig-checkbox-directive';
import { ResourceMultiSelectGridModule } from '../../shared/resource-multiselect-grid.component';
import { PopupMenuModule } from "../../shared/controls/popup-menu/popup-menu.component";
import { SiteModalModule } from "../../shared/modal/gov-modal.module";
import { TooltipModule } from 'primeng/tooltip';
import { SearchFieldModule } from '../../shared/controls/search-field/search-field.component';
import { AdminSiteMenuAssetTypeEditorComponent } from './admin-site-menu-asset-type.component';
import { PropertyGroupModule } from '../../shared/controls/property-group/property-group.component';
import { AssetPreviewModule } from '../../shared/asset-preview/asset-preview.module';

@NgModule({
    imports: [CommonModule,
        FormsModule,


        AdminSettingsRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        HelpMenuModule,
        ColorPickerModule,
        TableModule,
        CheckboxModule,
        TooltipModule,

        //d3s        
        CoreModule,        
        SharedGridPagingInfoModule,        
        TilesModule,
        ShortcutModule,
        IconPickerModule,
        DayOfWeekInputModule,
        D3SSharedModule,
        ResourceMultiSelectGridModule,
        PipesModule,
        IgCheckboxModule,
		IgMessageBoxModule,
        IgNumberFieldModule,
        PopupMenuModule,
        SiteModalModule,
		IgNumberFieldModule,
		TooltipModule,
		SiteModalModule,
		TableModule,
		SearchFieldModule,
		PropertyGroupModule,
		AssetPreviewModule
    ],
    declarations: [
        AdminSettingsComponent,
        AdminSiteMenuComponent,
        AdminIpRestrictionComponent,
		AdminSiteMenuFolderEditorComponent,
		AdminSiteMenuAssetTypeEditorComponent,
		AdminSiteMenuDeleteDialogComponent,
		AdminHomeComponent
    ],
    providers: [
    ]
})
export class AdminSettingsModule { }