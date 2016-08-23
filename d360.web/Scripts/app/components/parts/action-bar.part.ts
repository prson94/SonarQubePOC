///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, OnInit } from '@angular/core';
import { ActionBarItem } from '../../models/action-bar.model';
import { MenuItem } from 'primeng/primeng';

@Component({
    selector: 'd3s-action-bar',
    template: `
            <ul>
                <li *ngFor="let action of actions">
                    <div *ngIf="action.menuItems">
                  
                    </div>                    
                    <div>        
                        <i [class]="'fa fa-2x ' + action.icon"></i>
                        <span *ngIf="action.menuItems"><i class="fa fa-chevron-down"></i></span>
                    </div>
                </li>
            </ul>
    `
})

export class ActionBar implements OnInit {
    @Input() actions: ActionBarItem[];

    constructor() {
    }

    ngOnInit() {
        console.log(this.actions[0].menuItems);
        this.load();
    }

    load() {

    }

    save() {
    }

}

