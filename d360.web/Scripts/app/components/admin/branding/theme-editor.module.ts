import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { AdminModule } from '../admin.module';

import { IgMessageBoxModule } from '../../shared/controls/message-box/message-box.module';

import { CodemirrorModule } from '@ctrl/ngx-codemirror';

import { SharedModule } from 'primeng/api';
import { AdminBrandingRoutingModule } from './admin-branding.routes';
import { SidePanelModule } from '../../shared/sidepanel/side-panel.module';
import { DirectivesModule } from '../../../directives/directives.module';
import { IgBadgeModule } from '../../shared/controls/badge/badge.module';
import { PopupMenuModule } from '../../shared/controls/popup-menu/popup-menu.component';
import { PropertyGroupModule } from '../../shared/controls/property-group/property-group.component';
import { ThemeEditorComponent } from './theme-editor.component';
import { SiteModalModule } from '../../shared/modal/gov-modal.module';
import { ImagePickerModule } from '../../shared/controls/image-picker/image-picker.component';
import { TooltipModule } from 'primeng/tooltip';
import { CodeAreaModule } from '../../shared/controls/codearea/codearea.component';
import { ColorSelectorModule } from '../../shared/controls/color-selector/color-selector.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule, 

        AdminBrandingRoutingModule,

        //code editor
        CodemirrorModule,

        //prime
        SharedModule,

        //d3s        
        CoreModule,
        PipesModule,
        TilesModule,
        AdminModule,

        IgMessageBoxModule,
        SidePanelModule,
        DirectivesModule,
        IgBadgeModule,
        PopupMenuModule,
        PropertyGroupModule,
        SiteModalModule,
        ImagePickerModule,
        ColorSelectorModule,
        TooltipModule,
        CodeAreaModule
    ],
    declarations: [
        ThemeEditorComponent
    ],
    exports: [
        ThemeEditorComponent
    ],
    providers: [
    ]
})
export class ThemeEditorModule { }