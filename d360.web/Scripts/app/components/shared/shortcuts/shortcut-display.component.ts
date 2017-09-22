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
        <li *ngFor="let s of shortcuts" (click)="navigate(s.Url)" class="shortcut" [style.background-color]="s.BackgroundColor">
            <div *ngIf="s.Icon != null" class="icon" [style.color]="s.IconColor">
                <i [class]="'fa ' + s.Icon" style="display: block;"></i>
            </div>
            <div *ngIf="s.IconUrl != null" class="custom-icon">
                <img [src]="s.IconUrl" style="max-width: 72px; max-height: 72px; "/>
            </div>
            <div class="shortcut-name" [title]="s.Name" [style.color]="s.TitleColor">
                {{s.Name}}
            </div>
            <div class="shortcut-desc" [title]="s.Description">
                {{s.Description}}
            </div>
        </li>
    </ul>
    `, providers: [ShortcutService]
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