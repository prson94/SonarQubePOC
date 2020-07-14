import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from '../../http-interceptors/govern-request.interceptor';
import { RouterModule } from '@angular/router';

import { D3SCheckboxModule } from '../shared/controls/gov-checkbox'
import { CoreModule } from '../shared/core.module';


import { GalleryComponent } from './gallery.component';
import { GalleryRoutingModule } from './gallery.routes';

import { GalleryBooleanComponent } from './gallery.boolean.component';
import { GalleryButtonComponent } from './gallery.button.component';
import { GalleryGuard } from '../../guards/gallery.guard';

import { TableModule } from 'primeng/table';


@NgModule({
    imports: [
        CommonModule,
        HttpClientModule,
        RouterModule,
        CoreModule,

        GalleryRoutingModule,

        D3SCheckboxModule,  

        TableModule,

    ],
    declarations: [
        GalleryComponent,
        GalleryBooleanComponent,
        GalleryButtonComponent,
    ],
    providers: [
        GalleryGuard,
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class GalleryModule { }