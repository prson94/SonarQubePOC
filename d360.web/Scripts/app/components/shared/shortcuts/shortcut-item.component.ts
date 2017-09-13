import { Input, Output, Component, OnInit, EventEmitter } from '@angular/core';
import { BaseComponent } from '../base.component';
import { Shortcut } from '../../../models/shortcuts.model';
import { CompanyImage } from '../../../models/settings.model';
import { ShortcutService } from '../../../services/shortcuts.service';
import { MessagesService } from '../../../services/messages.service';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-shortcut-item',
    template: ` 
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <div class="row">
        <div class="col s12 m6">
            <div class="FieldName">Name</div>
            <input type="text" [(ngModel)]="shortcut.Name" style="width: 98%" maxlength="250" />
        </div>
        <div class="col s12 m6">
            <div class="FieldName">Url</div>
            <input type="text" [(ngModel)]="shortcut.Url" style="width: 98%" maxlength="250" />
        </div>
    </div>
    <div class="row" style="padding-top:12px">
        <div class="col s12 m6">
            <div class="FieldName">Icon</div>
            <div>
                <div>
                    <input type="radio" name="iconType" [checked]="iconType == 'icon'" (change)="changeIconType($event)" value="icon" /> Use a predefined icon
                </div>
                <div *ngIf="iconType == 'icon'" style="padding-bottom: 10px;">
                    <d3s-icon-picker [(ngModel)]="shortcut.Icon" ngDefaultControl></d3s-icon-picker>
                </div>
                <div>
                    <input type="radio" name="iconType" [checked]="iconType == 'image'" (change)="changeIconType($event)" value="image" /> Upload your own icon
                </div>
                <div *ngIf="iconType == 'image'">
                    <input #imageUpload type="file" (change)="onFileChange($event)" accept="image/gif,image/jpeg,image/png" />
                </div>
            </div>
        </div>
        <div class="col s12 m6">
            <div class="FieldName">Preview</div>
            <div *ngIf="iconType == 'icon'" style="padding:10px">
                <span style="font-size: 64px"><i [class]="'fa ' + shortcut.Icon"></i></span>
            </div>
            <div *ngIf="iconType == 'image'" style="padding:10px">
                <img *ngIf="iconImage != null && iconImage.dataUrl != null" [src]="iconImage.dataUrl" style="max-width: 72px; max-height: 128px;" />
                <img *ngIf="shortcut.IconPayload == null" [src]="shortcut.IconUrl" style="max-width: 72px; max-height: 128px;" />
                <div *ngIf="shortcut.IconPayload != null || shortcut.IconUrl != null">
                    <button pButton type="button" label="Clear" (click)="clearIcon()"></button>
                </div>
            </div>
        </div>
    </div>
    <div class="row" style="padding-top:12px">
        <div class="col s12">
            <button pButton type="button" label="Save" (click)="save()" [disabled]="!valid()"></button>
            <button pButton type="button" label="Cancel" (click)="cancel()"></button>
        </div>
    </div>
</div>
                `
    , providers: [ShortcutService]
})

export class ShortcutItemComponent extends BaseComponent implements OnInit {
    @Input() shortcut: Shortcut;
    @Output() shortcutChange = new EventEmitter();
    @Output() onSave = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private isAdding = false;
    private iconImage: CompanyImage = new CompanyImage();
    private iconType = 'icon';

    constructor(private shortcutService: ShortcutService, private messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        if (this.shortcut == null) {
            this.isAdding = true;
            this.shortcut = new Shortcut();
        } else {
            if (this.shortcut.IconUrl != null)
                this.iconType = 'image';
            else
                this.iconType = 'icon';
        }
    }

    changeIconType(e: any) {
        if (this.iconType == 'icon') {
            this.iconType = 'image'
            this.shortcut.Icon = null;
        } else {
            this.iconType = 'icon';
            this.shortcut.IconUrl = null;
            this.shortcut.IconPayload = null;
            this.iconImage = new CompanyImage();
        }
    }

    clearIcon() {
        this.shortcut.IconUrl = null;
        this.shortcut.IconPayload = null;
        this.iconImage = new CompanyImage();
        this.onFileChange(null);
        console.log(this.iconImage);
    }

    save() {
        this.isLoading = true;

        if (this.iconImage != null && this.iconImage.dataUrl != null) {
            this.shortcut.IconPayload = this.iconImage.dataUrl;
        }

        if (this.isAdding) {
            this.shortcutService.addShortcut(this.shortcut)
                .then(r => {
                    this.showMessageForResult(this.messagesService, r);
                    this.isLoading = false;
                    this.onSave.emit();
                });
        } else {
            this.shortcutService.editShortcut(this.shortcut)
                .then(r => {
                    this.showMessageForResult(this.messagesService, r);
                    this.isLoading = false;
                    this.onSave.emit();
                });
        }
    }

    cancel() {
        this.onCancel.emit();
    }

    valid() {
        if (this.shortcut == null)
            return false;
        if (this.shortcut.Name == null)
            return false;
        if (this.shortcut.Url == null)
            return false;
        if (this.shortcut.Icon == null && this.shortcut.IconUrl == null && this.shortcut.IconPayload == null && (this.iconImage.dataUrl == null || this.iconImage.dataUrl == ''))
            return false;

        return true;
    }

    onFileChange(event): void {
        if (this.iconImage == null)
            this.iconImage = new CompanyImage();

        if (event == null) {
            this.iconImage.file = null;
            this.iconImage.setDataUrl();
            this.shortcut.IconPayload = null;
            return;
        }

        let target = event.target || event.srcElement;
        let files = target.files;

        this.iconImage.file = files[0];
        this.iconImage.setDataUrl();

        this.shortcut.IconPayload = this.iconImage.dataUrl;
    }
}