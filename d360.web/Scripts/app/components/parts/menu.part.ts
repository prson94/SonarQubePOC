///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnInit, EventEmitter } from '@angular/core';


@Component({
    selector: 'd3s-menu',
    styles: [
        `
        ul.bottom-right {
            left: 0;
            top: 0;
        }
        
        ul.bottom-left {
            right: 0;
            top: 0;
        }

        ul.top-right {
            left: 0;
            bottom: 0;
        }
        ul.top-left{
            right: 0;
            bottom: 0;
        }

        ul {
            position: absolute;
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
        
        .menu-item {
            cursor: pointer;
        }


        `
    ],
    template: `
        <div>
            <div class="menu-item" (click)="toggle()" >
                <ng-content></ng-content>
            </div>
            <div *ngIf="showMenu" class="menu-anchor" (mouseleave)="showMenu = false">
                <ul [class]="menuPosition" [style.width]="width">
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
    @Input() menuPosition: string = "bottom-right";
    @Input() width: string = "200px";
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
