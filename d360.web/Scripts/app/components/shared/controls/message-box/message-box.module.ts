import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { MessageBoxComponent } from './message-box.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

    ],
    declarations: [
        MessageBoxComponent,
    ],
    exports: [
        MessageBoxComponent,
    ],
    providers: [
        
    ]
})
export class IgMessageBoxModule { }