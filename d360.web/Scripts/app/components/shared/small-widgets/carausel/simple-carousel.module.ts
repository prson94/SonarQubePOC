import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PipesModule } from '../../../../pipes/pipes.module';
import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { SimpleCarouselComponent } from './simple-carousel.component';

@NgModule({
    imports: [
        CommonModule,
        PipesModule
    ],
    declarations: [
        SimpleCarouselComponent
    ],
    exports: [
        SimpleCarouselComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class SimpleCarouselModule { }