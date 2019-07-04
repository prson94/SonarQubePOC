import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';


import { AuthenticationConnectionBackend } from '../../../../authentication-connection-backend';
import { DynamicPercentageComponent } from './dynamic-percentage-component';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,
    ],
    declarations: [
        DynamicPercentageComponent
    ],
    exports: [
        DynamicPercentageComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class DynamicPercentageModule { }