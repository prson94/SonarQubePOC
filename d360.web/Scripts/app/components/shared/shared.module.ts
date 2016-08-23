
import {  NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
//import * as shared from './index';
//import * as primeng from 'primeng/primeng';
import { FormModule } from '../forms/forms.module';
import { PartsModule } from '../parts/parts.module';
import { TilesModule } from '../tiles/tiles.module';

import {
    ButtonModule,
    EditorModule,
    InputTextModule,
    DropdownModule,
    InputMaskModule,
    MultiSelectModule,
    DataTableModule,
} from 'primeng/primeng';

import {
    AuditComponent,
    DashboardTabComponent,
    DynamicEditorComponent,
    DynamicFieldComponent,
    DynamicGridComponent,
    DynamicRelationshipGridComponent,
    LineageComponent,
    MessagesComponent,
    ObjectBoardComponent,
    ObjectChallengeComponent,
    ObjectHealthComponent,
    ObjectIssuesComponent,
    OwnershipTabComponent,
    PageLinksComponent,
    PowerBIViewerComponent,
    TooltipComponent
} from './index';


//import * as primeng from 'primeng/primeng';

@NgModule({
    declarations: [
        AuditComponent,
        DashboardTabComponent,
        DynamicEditorComponent,
        DynamicFieldComponent,
        DynamicGridComponent,
        DynamicRelationshipGridComponent,
        LineageComponent,
        //MessagesComponent,
        ObjectBoardComponent,
        ObjectChallengeComponent,
        ObjectHealthComponent,
        ObjectIssuesComponent,
        OwnershipTabComponent,
        PageLinksComponent,
        PowerBIViewerComponent,
        TooltipComponent
    ],
    exports: [
        AuditComponent,
        DashboardTabComponent,
        DynamicEditorComponent,
        DynamicFieldComponent,
        DynamicGridComponent,
        DynamicRelationshipGridComponent,
        LineageComponent,
        //MessagesComponent,
        ObjectBoardComponent,
        ObjectChallengeComponent,
        ObjectHealthComponent,
        ObjectIssuesComponent,
        OwnershipTabComponent,
        PageLinksComponent,
        PowerBIViewerComponent,
        TooltipComponent
        ]
    , imports: [
        ButtonModule,
        EditorModule,
        InputTextModule,
        DropdownModule,
        InputMaskModule,
        MultiSelectModule,
        DataTableModule,
        BrowserModule,
        //FormModule,
        PartsModule,
        //TilesModule,
    ]

})

export class SharedModule { }