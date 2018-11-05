import { Input, Component, OnInit } from '@angular/core';
import { BaseComponent } from '../base.component';
import { Shortcut } from '../../../models/shortcuts.model';
import { ShortcutService } from '../../../services/shortcuts.service';
import { MessagesService } from '../../../services/messages.service';
import { FormMode } from '../../../models/form.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-shortcut-list',
    template: ` 
<header>
    Shortcuts
    <d3s-tile-actions [hasAdd]="formMode == FormMode.Default" (addClick)="add()"></d3s-tile-actions>
</header>
<div [ngSwitch]="formMode">
    <div *ngSwitchCase="FormMode.Default">
<p-table #dt [value]="shortcuts" selectionMode="single" [metaKeySelection]="true" [pageLinks]="3" [paginator]="true" [rows]="10">
    <ng-template pTemplate="header">
        <tr>
            <th>Name</th>
            <th></th>
        </tr>
    </ng-template>
    <ng-template pTemplate="body" let-item>
        <tr [pSelectableRow]="item">
            <td>{{item.Name}}</td>
            <td>
                <div class="RowTools">
                    <a (click)="moveUp(item.ID)" style="cursor:pointer;"><i class="fa fa-caret-up"></i></a>
                    <a (click)="moveDown(item.ID)" style="cursor:pointer;"><i class="fa fa-caret-down"></i></a>
                    <a (click)="edit(item.ID)"><i class="fa fa-pencil"></i></a>
                    <a (click)="delete(item.ID)"><i class="fa fa-trash-o"></i></a>
                </div>
            </td>
        </tr>
    </ng-template>
    <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
        <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
    </ng-template>
</p-table>


<!--<p-dataTable #dt [value]="shortcuts" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3">
            <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
            <p-column field="Name" header="Name"></p-column>                
            <p-column field="ID">
                <ng-template pTemplate type="body" let-item="rowData">
                    <div class="RowTools">
                        <a (click)="moveUp(item.ID)" style="cursor:pointer;"><i class="fa fa-caret-up"></i></a>
                        <a (click)="moveDown(item.ID)" style="cursor:pointer;"><i class="fa fa-caret-down"></i></a>
                        <a (click)="edit(item.ID)"><i class="fa fa-pencil"></i></a>
                        <a (click)="delete(item.ID)"><i class="fa fa-trash-o"></i></a>
                    </div>
                </ng-template>
            </p-column>
        </p-dataTable> -->
    </div>
    <div *ngSwitchCase="FormMode.Adding">
        <d3s-shortcut-item (onSave)="cancel()" (onCancel)="cancel()"></d3s-shortcut-item>
    </div>
    <div *ngSwitchCase="FormMode.Editing">
        <d3s-shortcut-item [shortcut]="selectedShortcut" (onSave)="cancel()" (onCancel)="cancel()"></d3s-shortcut-item>
    </div>
    <div *ngSwitchCase="FormMode.Deleting">
        <div>
            Are you sure you want to delete the [{{selectedShortcut.Name}}] shortcut?
        </div>
        <button pButton type="button" label="Delete" (click)="confirmDelete()"></button>
        <button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default"></button>
    </div>
</div>
                `
    ,providers: [ShortcutService]
})

export class ShortcutListComponent extends BaseComponent implements OnInit {
    private shortcuts: Shortcut[] = [];
    private selectedShortcut: Shortcut = null;
    private formMode = FormMode.Default;
    FormMode = FormMode;
    
    constructor(private shortcutService: ShortcutService, private messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.shortcutService.getShortcuts()
            .then(r => this.shortcuts = r);
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
            .then(r => {
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
            .then(r => this.shortcutService.getShortcuts())
            .then(r => this.shortcuts = r);
    }

    moveDown(id: number) {
        this.isLoading = true;
        this.shortcutService.moveShortcutDown(id)
            .then(r => this.shortcutService.getShortcuts())
            .then(r => this.shortcuts = r);
    }
}