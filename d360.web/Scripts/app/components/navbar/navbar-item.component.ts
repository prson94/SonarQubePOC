///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit, Input, Output, EventEmitter, ElementRef } from '@angular/core';
import { ROUTER_DIRECTIVES } from '@angular/router';

@Component({
    selector: 'd3s-navbar-item',
    host: {
        '(document:click)': 'onClick($event)',
    },
    directives: [ROUTER_DIRECTIVES, NavBarItemComponent],
    styles: [`
    a.group {
        display:inline;
        font-size:small;
        padding:0;
    }
  `],
    template: `
                <div *ngIf="!item.subItems || item.subItems.length <= 0" [class.router-link-active]="item.active">
                    <a *ngIf="item.route" [routerLink]="[item.route]" style="font-size:small;" class="nav-item active"><i [class]="'fa fa-' + item.icon"></i><span *ngIf="!item.icon">-</span> {{item.name}}</a>
                    <a *ngIf="item.url" [href]="[item.url]" style="font-size:small;" class="nav-item active"><i [class]="'fa fa-' + item.icon"></i><span *ngIf="!item.icon">-</span> {{item.name}}</a>                    
                </div>
                <div *ngIf="item.subItems && item.subItems.length > 0" [class.router-link-active]="item.active">
                    <span style="cursor: pointer;" class="nav-item inactive" (click)="expandClick(item);item.expanded = !item.expanded"><i [class]="'fa fa-' + (item.icon || (item.expanded ? 'caret-down' : 'caret-right'))"></i>
                        <a class="group" *ngIf="item.route" [routerLink]="[item.route]">&nbsp;&nbsp;{{item.name}}</a>
                        <span *ngIf="!item.route">&nbsp;&nbsp;{{item.name}}</span>
                    </span>
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
    @Output() onExpanded = new EventEmitter();

    constructor(private elementRef: ElementRef) {
    }

    ngOnInit() {
    }

    expandClick(selItem) {
        this.onExpanded.emit({ item: selItem });
    }

    onClick(event) {
        if (this.item && this.item.expanded && !this.elementRef.nativeElement.contains(event.target)) { // or some similar check
            this.item.expanded = false;
        }
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
