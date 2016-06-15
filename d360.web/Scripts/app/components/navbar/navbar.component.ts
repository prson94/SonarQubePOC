///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { ROUTER_DIRECTIVES } from '@angular/router-deprecated';

@Component({
    selector: 'd3s-navbar',
    directives: [ROUTER_DIRECTIVES],
    template: `
                <ul class="side-nav fixed" style="overflow: auto; transform: translateX(0px);">
                  <li class="logo"></li>        
                  <li><a href="/"><i class="fa fa-pencil"></i> Legacy site</a></li>
                  <li><a href="#!"><i class="fa fa-book"></i> Glossary</a></li>
                  <li><a href="#!"><i class="fa fa-sitemap"></i> Models</a></li>
                  <li><a href="#!"><i class="fa fa-university"></i> Policies</a></li>
                  <li><a href="#!"><i class="fa fa-database"></i> Fusion</a></li>
                  <li><a href="#!"><i class="fa fa-dashboard"></i> Monitor</a></li>
                  <li><a href="#!"><i class="fa fa-group"></i> Community</a></li>
                  <li><a [routerLink]="['./Admin/Settings']"><i class="fa fa-gears"></i> Administration</a></li>                    
                </ul>
              `    
})

export class NavBarComponent { }
