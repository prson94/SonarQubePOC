///<reference path="./es6-shim.d.ts"/>
import {bootstrap}  from  '@angular/platform-browser-dynamic'
import {provide, PLATFORM_DIRECTIVES, enableProdMode} from '@angular/core'
import {AppComponent} from './app.component'
import {ROUTER_PROVIDERS} from '@angular/router-deprecated'
import {HTTP_PROVIDERS} from '@angular/http'
import 'rxjs/Rx'

//enableProdMode();

bootstrap(AppComponent, [ROUTER_PROVIDERS, HTTP_PROVIDERS]);

//bootstrap(AppComponent, [
//    provide(PLATFORM_DIRECTIVES, { useValue: [ROUTER_PROVIDERS], multi: true }),
//    provide(PLATFORM_DIRECTIVES, { useValue: [HTTP_PROVIDERS], multi: true }),
//]);