///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit, Input } from '@angular/core';
import { ROUTER_DIRECTIVES } from '@angular/router';

@Component({
    selector: 'd3s-navbar-item',
    directives: [ROUTER_DIRECTIVES, NavBarItemComponent],
    template: `
                <div [class.router-link-active]="item.active">
                    <a *ngIf="item.route" [routerLink]="[item.route]" style="font-size:small;" class="nav-item active"><i [class]="'fa fa-' + item.icon"></i>- {{item.name}}</a>
                    <a *ngIf="item.url" [href]="[item.url]" style="font-size:small;" class="nav-item active"><i [class]="'fa fa-' + (item.icon || 'minus')"></i> {{item.name}}</a>
                    <span *ngIf="!item.route && !item.url" style="cursor: pointer;" class="nav-item inactive" (click)="item.expanded = !item.expanded"><i [class]="'fa fa-' + (item.icon || (item.expanded ? 'caret-down' : 'caret-right'))"></i>&nbsp;&nbsp;{{item.name}}</span>
                </div>
                <ul *ngIf="item.subItems && item.subItems.length > 0" [hidden]="!item.expanded" style="padding-left:15px; font-size:small">
                    <li *ngFor="let sub of item.subItems" style="font-size:small;">
                        <d3s-navbar-item [item]="sub" [class.router-link-active]="sub.active"></d3s-navbar-item> 
                    </li>
                </ul>
`
})

export class NavBarItemComponent implements OnInit {
    @Input() item: NavBarItem;

    constructor() {
    }

    ngOnInit() {
    }

}
export class NavBarItem {
    icon: string;
    name: string;
    route: string;
    expanded = false;
    active = false;
    subItems: NavBarItem[];
    parent: NavBarItem;
    url: string;
}
