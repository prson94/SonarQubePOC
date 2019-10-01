import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../base.component';
import { Shortcut, LinkTarget } from '../../../models/shortcuts.model';
import { ShortcutService } from '../../../services/shortcuts.service';
import { Observable } from 'rxjs';

@Component({
    selector: 'd3s-shortcut-display',
    template: ` 

    <ul class="shortcutlist">
        <li *ngFor="let s of shortcuts | async" (click)="navigate(s)" class="shortcut" [style.background-color]="s.BackgroundColor">
            <div *ngIf="s.Icon != null" class="icon" [style.color]="s.IconColor">
                <i [class]="'fa ' + s.Icon" style="display: block;"></i>
            </div>
            <div *ngIf="s.IconUrl != null" class="custom-icon">
                <img [src]="s.FullURL" style="max-width: 72px; max-height: 72px; "/>
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
    public shortcuts: Observable<Shortcut[]>;

    constructor(private shortcutService: ShortcutService, private router: Router) {
        super();
    }

    ngOnInit() {
        this.shortcuts = this.shortcutService.getShortcuts();
    }

    navigate(shortcut: Shortcut) {
        if (shortcut.LinkTarget == LinkTarget.NewWindow) {
            window.open(shortcut.Url, "_blank");
        }
        else if (shortcut.LinkTarget == LinkTarget.Self) {
            window.open(shortcut.Url, "_self");
        }
        else if (shortcut.LinkTarget == LinkTarget.RouterLink) {
            this.router.navigateByUrl(shortcut.Url);
        }
    }
}