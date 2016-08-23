///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit, Input, Output, EventEmitter, ElementRef } from '@angular/core';

@Component({
    selector: 'd3s-navbar-item',   
    styles: [`
    a.group, a.topgroup {
        display:inline;        
        padding:0;
    }
    a.group {        
        font-size:small;        
    }
    .topgroup, .topitem {
        margin-left: 15px;
    }
    
  `],
    template: `
                <div *ngIf="!item.subItems || item.subItems.length <= 0" [class.router-link-active]="item.active" >                    
                    <span *ngIf="item.isRootItem()" style="cursor: pointer;" class="nav-item inactive" (click)="expandClick(item);item.expanded = !item.expanded"><i [class]="'fa fa-' + (item.icon || (item.expanded ? 'caret-down' : 'caret-right'))"></i>
                        <a *ngIf="item.route" [routerLink]="[item.route]" class="topgroup truncate">{{item.name}}</a>
                    </span>
                    <a *ngIf="!item.isRootItem() && item.route" [routerLink]="[item.route]" style="font-size:small;" class="nav-item active truncate"><i [class]="'fa fa-' + item.icon"></i>{{item.name}}</a>                    
                    <a *ngIf="!item.isRootItem() && item.url" [href]="[item.url]" style="font-size:small;" class="nav-item active truncate"><i [class]="'fa fa-' + item.icon"></i><span *ngIf="!item.icon">-</span> {{item.name}}</a>                    
                </div>
                <div *ngIf="item.subItems && item.subItems.length > 0" [class.router-link-active]="item.active">
                    <span style="cursor: pointer;" class="nav-item inactive" (click)="expandClick(item);item.expanded = !item.expanded"><i *ngIf="item.isRootItem()" [class]="'fa fa-' + (item.icon || (item.expanded ? 'caret-down' : 'caret-right'))"></i>
                        <a class="group truncate" *ngIf="item.route" [routerLink]="[item.route]">{{item.name}}</a>
                        <span *ngIf="!item.route" [ngClass]="{'topitem':item.isRootItem()}">{{item.name}}</span>
                        <span class="right"><i [class]="'fa fa-' + (item.expanded ? 'caret-up' : 'caret-down')"></i></span>
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

    public isRootItem(): boolean
    {        
        return this.parent == undefined;
    }
}
