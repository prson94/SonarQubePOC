import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { HTTP_INTERCEPTORS, } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { AssetDetailFieldComponent } from "./asset-detail-field.component";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { PipesModule } from "../../../pipes/pipes.module";
import { CoreModule } from "../core.module";
import { TooltipModule } from "primeng/tooltip";
import { NgxJsonViewModule } from "ng-json-view";
import { IgColorPickerModule } from "../controls/color-picker/color-picker.module";
import { TagViewModule } from "../tags/d3s-tag-view.module";

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        CoreModule,
        PipesModule,
        TooltipModule,
        NgxJsonViewModule,
        IgColorPickerModule,
        TagViewModule,
    ],
    declarations: [
        AssetDetailFieldComponent,
    ],
    exports: [
        AssetDetailFieldComponent,
    ],
    providers: [{
        provide: HTTP_INTERCEPTORS,
        useClass: GovernRequestInterceptor,
        multi: true
    }]
})
export class AssetDetailModule { }