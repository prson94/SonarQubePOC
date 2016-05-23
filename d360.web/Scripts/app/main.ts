///<reference path="./es6-shim.d.ts"/>
import {bootstrap}  from  'angular2/platform/browser'
import {provide, PLATFORM_DIRECTIVES} from 'angular2/core'
import {AppComponent} from './app.component'
import {ROUTER_PROVIDERS} from 'angular2/router'
import {HTTP_PROVIDERS} from 'angular2/http'
import 'rxjs/Rx'

bootstrap(AppComponent, [ROUTER_PROVIDERS, HTTP_PROVIDERS]);

//bootstrap(AppComponent, [
//    provide(PLATFORM_DIRECTIVES, { useValue: [ROUTER_PROVIDERS], multi: true }),
//    provide(PLATFORM_DIRECTIVES, { useValue: [HTTP_PROVIDERS], multi: true }),
//]);