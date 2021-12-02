import { Input, Component, OnInit } from '@angular/core';
import { BaseComponent } from '../base.component';
import { Shortcut } from '../../../models/shortcuts.model';
import { ShortcutService } from '../../../services/shortcuts.service';
import { FormMode } from '../../../models/form.model';
import * as _ from 'lodash';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-shortcut-list',
    templateUrl: './shortcut-list.component.html',
    providers: [ShortcutService]
})

export class ShortcutListComponent extends BaseComponent implements OnInit {
    private shortcuts: Shortcut[] = [];
    private selectedShortcut: Shortcut = null;
    formMode = FormMode.Default;
    FormMode = FormMode;
    
    constructor(
        private shortcutService: ShortcutService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.shortcutService.getShortcuts()
            .subscribe((r) => this.shortcuts = r);
    }

    add() {
        this.formMode = FormMode.Adding;
        this.selectedShortcut = null;
    }

    edit(id: number) {
        this.formMode = FormMode.Editing;
        this.selectedShortcut = this.shortcuts.find(s => s.ID == id);
    }

    delete(id: number) {
        this.formMode = FormMode.Deleting;
        this.selectedShortcut = this.shortcuts.find(s => s.ID == id);
    }

    confirmDelete() {
        if (this.selectedShortcut == null)
            return;
        this.shortcutService.deleteShortcut(this.selectedShortcut.ID)
            .subscribe(r => {
                this.showMessageForResult(this.messagesService, r);
                this.cancel();
            });
    }

    cancel() {
        this.selectedShortcut = null;
        this.load();
        this.formMode = FormMode.Default;
    }
    moveUp(id: number) {
        this.isLoading = true;
        this.shortcutService.moveShortcutUp(id)
            .subscribe(r => {
                this.shortcutService.getShortcuts()
                    .subscribe((r) => this.shortcuts = r);
            })
    }

    moveDown(id: number) {
        this.isLoading = true;
        this.shortcutService.moveShortcutDown(id)
            .subscribe(r => {
                this.shortcutService.getShortcuts()
                    .subscribe((r) => this.shortcuts = r);
            })
    }
}