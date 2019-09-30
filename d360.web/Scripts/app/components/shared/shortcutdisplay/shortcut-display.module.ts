import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../core.module';

import { ShortcutDisplayComponent } from './shortcut-display.component';

@NgModule({
    imports: [CommonModule,        
        HttpClientModule,
        RouterModule,
        CoreModule,        
    ],
    declarations: [        
        ShortcutDisplayComponent,
    ],
    exports: [        
        ShortcutDisplayComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class ShortcutDisplayModule { }