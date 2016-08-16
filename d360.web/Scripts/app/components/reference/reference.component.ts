///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { ROUTER_DIRECTIVES } from '@angular/router';

@Component({
    selector: 'd3s-reference',
    template: ` <div id="main">
                    <router-outlet></router-outlet>
                </div>
             ` ,
    directives: [ROUTER_DIRECTIVES]
})

export class ReferenceComponent { }
