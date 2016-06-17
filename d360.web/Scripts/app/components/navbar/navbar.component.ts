///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { ROUTER_DIRECTIVES } from '@angular/router';

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
                  <li><a (click)="showAdminLinks()"><i class="fa fa-gears"></i> Administration</a></li>
                  <ul class="sub" *ngIf="showAdminChildLinks==true">
                        <li><a [routerLink]="['/a/admin/settings']"><i class="fa fa-minus" aria-hidden="true"></i> Settings</a></li>
                        <li><a [routerLink]="['/a/admin/domain']"><i class="fa fa-minus" aria-hidden="true"></i> Reference Types</a></li>
                        <li><a [routerLink]="['/a/admin/workflow']"><i class="fa fa-minus" aria-hidden="true"></i> Workflow</a></li>
                        <li><a [routerLink]="['/a/admin/groups']"><i class="fa fa-minus" aria-hidden="true"></i> Groups</a></li>
                        <li><a [routerLink]="['/a/admin/responsibilities']"><i class="fa fa-minus" aria-hidden="true"></i> Responsibilities</a></li>
                        <li><a [routerLink]="['/a/admin/artifacts']"><i class="fa fa-minus" aria-hidden="true"></i> Artifacts</a></li>
                        <li><a [routerLink]="['/a/admin/templates']"><i class="fa fa-minus" aria-hidden="true"></i> Templates</a></li>
                   </ul>                        
                </ul>
              `    
})

export class NavBarComponent {
    private showAdminChildLinks: boolean = false;

    showAdminLinks() {
        this.showAdminChildLinks = !this.showAdminChildLinks;
    }
}
