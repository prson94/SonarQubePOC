///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnInit, EventEmitter } from '@angular/core';


@Component({
    selector: 'd3s-menu',
    styles: [
        `
        ul {
            position: absolute;
            left: 0;
            top: 0;
            z-index: 1000;
            background-color: #fff;
            box-shadow: 5px 5px 10px 0px rgba(0,0,0,0.25);
            margin: 0;
        }
        
        li {
            cursor: pointer;
            padding: 5px 15px 5px 15px
        }
        
        li:hover {
            background-color: #ddd;
        }
        
        .menu-anchor {
            position: relative;
            left: 0;
            top: 0;
        }

        .menu-button {
            padding: 5px;
            cursor: pointer;
        }
        
        .menu-button:hover {
            background-color: #fff;
            color:black;
        }

        .menu-button.active {
            box-shadow: 5px 5px 10px 0px rgba(0,0,0,0.25);
            background-color: #fff;
            color:black;
        }
        
        `
    ],
    template: `
        <div>
            <span class="menu-button" [class.active]="showMenu" (click)="toggle()" >
                <ng-content></ng-content>
            </span>
            <div *ngIf="showMenu" class="menu-anchor" (mouseleave)="showMenu = false">
                <ul>
                    <li *ngFor="let item of items" (click)="handleClick(item)">
                        <span *ngIf="item.icon != ''"><i [class]="'fa fa-' + item.icon"></i></span> {{item.text}}
                    </li>
                </ul>
            </div>

        </div>
    `
})

export class MenuPart implements OnInit {
    @Input() items: MenuPartItem[];
    @Output() onItemClick: EventEmitter<MenuPartItem> = new EventEmitter<MenuPartItem>();

    showMenu = false;

    constructor() {
    }

    ngOnInit() {
    }

    toggle() {
        this.showMenu = !this.showMenu;
    }

    handleClick(item: MenuPartItem) {
        if (item.enabled) {
            this.showMenu = false;
            this.onItemClick.emit(item);
        }
            
    }
}

export class MenuPartItem {
    icon: string;
    text: string;
    data: any;
    enabled: boolean = true;
}
