import { ButtonModule } from 'primeng/button';
import { CommonModule } from '@angular/common';
import { CoreModule } from '../core.module';
import { DynamicLookupGridComponent } from './dynamic-lookup-grid.component';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { InputTextModule } from 'primeng/inputtext';
import { NgxJsonViewModule } from 'ng-json-view';
import { NgModule } from '@angular/core';
import { ObjectDetailComponent } from './object-detail.component';
import { ObjectDetailFieldComponent } from './object-detail-field.component';
import { PipesModule } from '../../../pipes/pipes.module';
import { RouterModule } from '@angular/router';
import { SharedModule } from 'primeng/api';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SimpleAccordionModule } from '../simple-accordion.part';
import { TableModule } from 'primeng/table';
import { TagViewModule } from '../tags/d3s-tag-view.module';
import { TilesModule } from '../tiles/tiles.module';
import { TooltipModule } from 'primeng/tooltip';
import { IgColorPickerModule } from '../controls/color-picker/color-picker.module';
import { SynonymsTile } from './synonyms.tile';
import { FormsModule } from '@angular/forms';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { SharedDeleteFormModule } from '../delete.form';
import { ObjectDetailCategoryComponent } from './object-detail-category.component';
import { PropertyGroupModule } from '../controls/property-group/property-group.component';
import { DynamicFieldNameModule } from '../dynamic-field-name.component';


@NgModule({
    imports: [
        ButtonModule,
        CommonModule,
        CoreModule,        
        HttpClientModule,
        InputTextModule,
        NgxJsonViewModule,
        PipesModule,
        RouterModule,
        SharedDynamicGridEditorModule,
        SharedGridPagingInfoModule,
        SharedModule,
        SimpleAccordionModule,
        TableModule,
        TagViewModule,
        TilesModule,
        TooltipModule,
        IgColorPickerModule,
        FormsModule,
        AutoCompleteModule,
        SharedDeleteFormModule,
        PropertyGroupModule,
        DynamicFieldNameModule
    ],
    declarations: [
        DynamicLookupGridComponent,
        ObjectDetailComponent, 
        ObjectDetailFieldComponent,
        ObjectDetailCategoryComponent,
        SynonymsTile,
    ],
    exports: [
        ObjectDetailComponent,
        ObjectDetailFieldComponent,
        SynonymsTile,
    ],
    providers: [{
        provide: HTTP_INTERCEPTORS,
        useClass: GovernRequestInterceptor,
        multi: true
    }]
})
export class SharedObjectDetailsModule { }