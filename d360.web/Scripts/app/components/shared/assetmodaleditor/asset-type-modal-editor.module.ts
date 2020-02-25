import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { FormsModule } from '@angular/forms';

import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { SharedModule } from 'primeng/shared';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';

import { AssetTypeModalEditorComponent } from './asset-type-modal-editor';
import { SiteModalModule } from '../modal/gov-modal.module';
import { SharedDynamicGridEditorModule } from '../dynamicgrideditor/shared-dynamic-grid-editor.module';

@NgModule({
    imports: [
        CommonModule,
        HttpClientModule,
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
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class AssetTypeModalEditorModule { }