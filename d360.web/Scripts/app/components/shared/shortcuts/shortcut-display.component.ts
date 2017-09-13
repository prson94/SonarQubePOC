import { Input, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../base.component';
import { Shortcut } from '../../../models/shortcuts.model';
import { ShortcutService } from '../../../services/shortcuts.service';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-shortcut-display',
    template: ` 
    <ul>
        <li *ngFor="let s of shortcuts" (click)="navigate(s.Url)">
            <div *ngIf="s.Icon != null" style="width: 72px; height: 72px; font-size:64px; text-align: center;">
                <i [class]="'fa ' + s.Icon" style="display: block;"></i>
            </div>
            <div *ngIf="s.IconUrl != null" style="width: 72px; height: 72px;">
                <img [src]="s.IconUrl" style="max-width: 72px; max-height: 72px; "/>
            </div>
            <div class="shortcut-name" style="font-size: 1.25em; font-weight: 600; text-align: center; padding-top: 5px;">
                {{s.Name}}
            </div>
        </li>
    </ul>
                `,
    styles: [
        `
        li {
            display: inline-block; 
            padding: 10px 30px 10px 30px;
            cursor: pointer;
            width: 135px;
            vertical-align: top;
            word-break: break-word;
        }

        li:hover {
            background-color: #DCDBDB;
        }
`   
    ]
    , providers: [ShortcutService]
})

export class ShortcutDisplayComponent extends BaseComponent implements OnInit {
    private shortcuts: Shortcut[] = [];

    constructor(private shortcutService: ShortcutService, private router: Router) {
        super();
    }

    ngOnInit() {
        this.shortcutService.getShortcuts()
            .then(r => {
                this.shortcuts = r;
            });
    }

    navigate(url: string) {
        window.open(url, "_blank");
    }
}