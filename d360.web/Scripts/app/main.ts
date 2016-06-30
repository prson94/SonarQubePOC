///<reference path="./es6-shim.d.ts"/>
import {bootstrap}  from  '@angular/platform-browser-dynamic'
import {provide, PLATFORM_DIRECTIVES, enableProdMode} from '@angular/core'
import { disableDeprecatedForms, provideForms } from '@angular/forms';
import {AppComponent} from './app.component'
import {APP_ROUTER_PROVIDERS} from './app.routes'
import {HTTP_PROVIDERS} from '@angular/http'
import 'rxjs/Rx'

//enableProdMode();

bootstrap(AppComponent, [
    APP_ROUTER_PROVIDERS,
    HTTP_PROVIDERS, 
    disableDeprecatedForms(),
    provideForms()
])
.catch(err => console.error(err));
