
import { Component, OnInit, Input, Output, EventEmitter, ElementRef, animate, style, transition, state, trigger  } from '@angular/core';
import { NavBarMode, NavBarItem } from '../../models/nav-bar.model';

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

    .nav-item-leaf {
        color: #ddd;
    }

    .expanded {
        box-shadow: 0px 2px 7px 0px rgba(0,0,0,1);
    }

    .top-background {
        background-color:#383127;
    }
    
  `],
    template: `
                <div *ngIf="!item.subItems || item.subItems.length <= 0" [class.router-link-active]="item.active" >                    
                    <span *ngIf="item.isRootItem()" style="cursor: pointer;" class="nav-item inactive top-background" [class.expanded]="item.expanded" (click)="expandClick(item);item.expanded = !item.expanded" ><i [class]="'fa fa-' + (item.icon || (item.expanded ? 'caret-down' : 'caret-right'))"></i>
                        <a *ngIf="item.route" [routerLink]="[item.route]" class="topgroup truncate">{{item.name}}</a>
                    </span>
                    <a *ngIf="!item.isRootItem() && item.route" [routerLink]="[item.route]" style="font-size:small;" class="nav-item active truncate nav-item-leaf"><i [class]="'fa fa-' + item.icon"></i>&#9642; {{item.name}}</a>                    
                    <a *ngIf="!item.isRootItem() && item.url" [href]="[item.url]" style="font-size:small;" class="nav-item active truncate"><i [class]="'fa fa-' + item.icon"></i>&#9642; {{item.name}}</a>                    
                </div>
                <div *ngIf="item.subItems && item.subItems.length > 0" [class.router-link-active]="item.active">
                    <span style="cursor: pointer;" class="nav-item inactive" [class.expanded]="item.expanded && item.isRootItem()" [class.top-background]="item.isRootItem()" (click)="expandClick(item);item.expanded = !item.expanded"><i *ngIf="item.isRootItem()" [class]="'fa fa-' + (item.icon || (item.expanded ? 'caret-down' : 'caret-right'))"></i>                                                
                        <a class="truncate" *ngIf="item.route && item.isRootItem()" [routerLink]="[item.route]" [ngClass]="{'topgroup': (item.isRootItem() && item.route), 'group': (item.route && !item.isRootItem())}">{{item.name}}</a>
                        <a class="truncate" *ngIf="item.route && !item.isRootItem()" [routerLink]="[item.route]" [ngClass]="{'topgroup': (item.isRootItem() && item.route), 'group': (item.route && !item.isRootItem())}">&#9632; {{item.name}}</a>
                        <span *ngIf="!item.route" [ngClass]="{'topitem':item.isRootItem()}">{{item.name}}</span>
                        <span class="right"><i [class]="'fa fa-' + (item.expanded ? 'caret-up' : 'caret-down')"></i></span>
                    </span>
                </div>
                <ul *ngIf="item.subItems && item.subItems.length > 0" [hidden]="!item.expanded" style="padding-left:15px; font-size:small">
                    <li *ngFor="let sub of item.subItems" style="font-size:small;">                        
                        <d3s-navbar-item [item]="sub" [class.router-link-active]="sub.active" style="display:list-item;"></d3s-navbar-item> 
                    </li>
                </ul>
`,

    animations: [
        trigger('expanded', [
            state('expanded', style({
                transform: 'scaleY(1)',
                opacity: '1',
                transformOrigin: 'top',
                height: '100%'
            })),
            state('collapsed', style({
                transform: 'scaleY(0)',
                opacity: '0',
                transformOrigin: 'top',
                height: '0'
            })),
            transition('expanded <=> collapsed', animate('100ms ease-in'))
        ]),
        trigger('flyInOut', [
            state('in', style({ transform: 'translateX(-100%)' })),
            transition('void => *', [
                style({ transform: 'translateX(0)' }),
                animate(100)
            ]),
            transition('* => void', [
                animate(100, style({ transform: 'translateX(100%)' }))
            ])
        ])
    ]
})

export class NavBarItemComponent implements OnInit {
    @Input() item: NavBarItem;
    @Output() onExpanded = new EventEmitter();
    @Input() level: number = 0;

    constructor(private elementRef: ElementRef) {
    }

    ngOnInit() {
    }

    expandClick(selItem) {
        this.onExpanded.emit({ item: selItem });
    }


}
