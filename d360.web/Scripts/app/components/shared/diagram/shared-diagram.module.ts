import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';


import { AutoCompleteModule } from 'primeng/autocomplete';
import { CheckboxModule } from 'primeng/checkbox';
import { InputSwitchModule } from 'primeng/inputswitch';
import { ButtonModule } from 'primeng/button';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { SharedModule } from 'primeng/shared';
import { TableModule } from 'primeng/table';
import { EditorModule } from 'primeng/editor';
import { ListboxModule } from 'primeng/listbox';
import { DropdownModule } from 'primeng/dropdown';
import { MenubarModule } from 'primeng/menubar';
import { TabViewModule } from 'primeng/tabview';
import { TreeModule } from 'primeng/tree';

import { ImpactComponent } from './impact.component';
import { LineageComponent } from './lineage/lineage.component';
import { LineageBusinessEditorComponent } from './lineage/lineage-business-editor.component';
import { LineageTechnicalEditorComponent } from './lineage/lineage-technical-editor.component';
import { LineageEditorPreviewComponent } from './lineage/lineage-editor-preview.component';
import { LineageFusionComponent } from './lineage/lineage-fusion.component';
import { LineageMappingRulesComponent } from './lineage/lineage-mapping-rules.component';
import { LineageObjectDetailComponent } from './lineage/lineage-object-detail.component';
import { LineageRelationshipsComponent } from './lineage/lineage-relationships.component';
import { LineageResponsibilitiesComponent } from './lineage/lineage-responsibilities.component';
import { LineageSourceRuleEditorComponent } from './lineage/lineage-source-rule-editor.component';
import { LineageSourceRulesComponent } from './lineage/lineage-source-rules.component';
import { LineageTechnicalRelationshipsComponent } from './lineage/lineage-technical-relationships.component';
import { LineageInfoComponent } from './lineage/lineage-info.component';
import { LineageEditorComponent } from './lineage/lineage-editor.component';

import { AssetBrowserComponent } from './assetbrowser/browser.component';

import { LineageDiagramComponent } from './lineage/lineage-diagram.component';

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

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,
        //d3s
        CoreModule,
        SharedDeleteFormModule,
        SharedFormMessageModule,
        SharedGridPagingInfoModule,
        TilesModule,
        D3SOverlayWindowModule,
        SharedObjectDetailsModule,

        //prime        
        CheckboxModule,
        EditorModule,     
        InputSwitchModule, 
        SharedModule,
        AutoCompleteModule,
        ButtonModule,
        InputTextareaModule,
        ListboxModule,
        DropdownModule,
        MenubarModule,
        TableModule,
        TabViewModule,
        TreeModule,
        //JSON Viewer module
        NgxJsonViewModule,
        TagViewModule
    ],
    declarations: [
        AssetBrowserComponent,
        ImpactComponent,        
        LineageComponent,
        LineageBusinessEditorComponent,
        LineageTechnicalEditorComponent,
        LineageEditorPreviewComponent,
        LineageFusionComponent,
        LineageMappingRulesComponent,
        LineageObjectDetailComponent,
        LineageRelationshipsComponent,
        LineageResponsibilitiesComponent,
        LineageSourceRuleEditorComponent,
        LineageSourceRulesComponent,
        LineageTechnicalRelationshipsComponent,
        LineageInfoComponent,
        LineageEditorComponent,
        ModelDiagramComponent,

        LineageDiagramComponent
    ],
    exports: [
        AssetBrowserComponent,
        LineageComponent,
        ImpactComponent,  
        ModelDiagramComponent,  

        LineageDiagramComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
        IconService
    ]
})
export class SharedDiagramModule { }