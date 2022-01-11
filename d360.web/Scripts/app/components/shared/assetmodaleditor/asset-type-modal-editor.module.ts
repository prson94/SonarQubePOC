import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';


import { FormsModule } from '@angular/forms';

import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { SharedModule } from 'primeng/api';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';

import { AssetTypeModalEditorComponent } from './asset-type-modal-editor';
import { SiteModalModule } from '../modal/gov-modal.module';
import { SharedDynamicGridEditorModule } from '../dynamicgrideditor/shared-dynamic-grid-editor.module';

@NgModule({
    imports: [
        CommonModule,

        FormsModule,
        RouterModule,
        SharedDynamicGridEditorModule,
        //d3s
        CoreModule,                
        TilesModule,
        SiteModalModule,
        //prime        
        ButtonModule,
        DropdownModule,
        SharedModule,
    ],
    declarations: [
        AssetTypeModalEditorComponent
    ],
    exports: [
        AssetTypeModalEditorComponent
    ],
    providers: [

    ]
})
export class AssetTypeModalEditorModule { }