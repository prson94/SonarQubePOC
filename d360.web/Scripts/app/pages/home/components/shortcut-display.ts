import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { LinkTarget, Shortcut } from '../../../models/shortcuts.model';
import { ShortcutService } from '../../../services/shortcuts.service';
import { Observable } from 'rxjs';
import { CompanySettingsService } from '../../../services/settings.service';
import { BaseComponent } from '../../../components/shared/base.component';
import { AsyncPipe } from '@angular/common';

@Component({
	selector: 'shortcut-display',
	standalone: true,
	imports: [AsyncPipe],
	templateUrl: 'shortcut-display.html'//,
	//providers: [ShortcutService]
})
export class ShortcutDisplay extends BaseComponent implements OnInit {
    public shortcuts: Observable<Shortcut[]>;

    constructor(
        protected settingsService: CompanySettingsService,
        private shortcutService: ShortcutService,
        private router: Router) {
        super(settingsService);
    }

    ngOnInit() {
        this.shortcuts = this.shortcutService.getShortcuts();
    }

    navigate(shortcut: Shortcut) {
        if (shortcut.LinkTarget === LinkTarget.NewWindow) {
            window.open(shortcut.Url, "_blank");
        }
        else if (shortcut.LinkTarget === LinkTarget.Self) {
            window.open(shortcut.Url, "_self");
        }
        else if (shortcut.LinkTarget === LinkTarget.RouterLink) {
			this.router.navigateByUrl(this.federateUrl(shortcut.Url));
        }
    }
}