///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, OnInit, Output, EventEmitter } from '@angular/core';
import { MenuPartItem } from './menu.part';


@Component({
    selector: 'd3s-action-bar',
    template: `
            <div class="action-bar" [class.right]="alignRight">
                <template ngFor let-item [ngForOf]="items">
                    <div *ngIf="item.menu == null && item.tooltip != null" class="action-bar-item" [class.disabled]="item.disabled" [pTooltip]="item.tooltip" tooltipPosition="top" (click)="handleClick(item)"><i [class]="'fa fa-'+item.icon"></i></div>
                    <div *ngIf="item.menu == null && item.tooltip == null" class="action-bar-item" [class.disabled]="item.disabled" (click)="handleClick(item)"><i [class]="'fa fa-'+item.icon"></i></div>
                    <d3s-menu *ngIf="item.menu" [items]="item.menu" (onItemClick)="handleMenuClick(item, $event)" [menuPosition]="alignRight ? 'bottom-left' : 'bottom-right'">
                        <div *ngIf="item.tooltip != null" class="action-bar-item" [class.disabled]="item.disabled" [pTooltip]="item.tooltip" tooltipPosition="top" ><i [class]="'fa fa-'+item.icon"></i><sup><i class="fa fa-chevron-down menu-arrow"></i></sup></div>
                        <div *ngIf="item.tooltip == null" class="action-bar-item" [class.disabled]="item.disabled" ><i [class]="'fa fa-'+item.icon"></i><sup><i class="fa fa-chevron-down menu-arrow"></i></sup></div>
                    </d3s-menu>
                </template>
            </div>
    `,
    styles: [
        `
        .action-bar.right {
            position:absolute;
            right:10px;
        }
        .action-bar-item {
            font-size: 1.3rem;
            display: inline-block;
            margin-left: 10px;
            cursor: pointer;
            transition: all .25s ease-in-out;
            transform: scale(1);
        }

        .action-bar-item:hover {
            transform: scale(1.25);    
        }

        .action-bar-item.disabled {
            cursor: default;
            transform: scale(1);
            color: #666;
        }
        .action-bar.disabled:hover {
                    cursor: default;
                    transform: scale(1);
        }
        .menu-arrow {
            font-size: 0.7em;
            margin-left: 2px;
        }
    `
    ]
})

export class ActionBar implements OnInit {
    @Input() items: ActionBarItem[];
    @Input() alignRight: boolean = true;
    @Output() onClick = new EventEmitter<ActionBarItem>();
    @Output() onMenuClick = new EventEmitter<MenuPartItem>();

    constructor() { }
    ngOnInit() { }


    handleClick(item: ActionBarItem) {
        if (item.disabled)
            return;
        this.onClick.emit(item);
    }
    handleMenuClick(item: ActionBarItem, menuItem: MenuPartItem) {
        if (item.disabled)
            return;
        this.onClick.emit(item);
        this.onMenuClick.emit(menuItem);
        return true;
    }

}

export class ActionBarItem {
    icon: string = 'question-circle';
    tooltip: string;
    title: string;
    key: string;
    disabled: boolean = false;
    menu: MenuPartItem[] = null;
    data: any = null;
}

