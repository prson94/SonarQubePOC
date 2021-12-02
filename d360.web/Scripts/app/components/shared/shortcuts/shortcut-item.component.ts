import { Input, Output, Component, OnInit, EventEmitter } from '@angular/core';
import { BaseComponent } from '../base.component';
import { Shortcut, LinkTarget } from '../../../models/shortcuts.model';
import { CompanyImage } from '../../../models/settings.model';
import { ShortcutService } from '../../../services/shortcuts.service';
import * as _ from 'lodash';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

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
    <div class="row">
        <div class="col s12 m6">
            <div class="FieldName">Description</div>
            <input type="text" [(ngModel)]="shortcut.Description" style="width: 98%" maxlength="500" />
        </div>        
        <div class="col s12 m6">
            <div class="FieldName">Link Opens In</div>
            <div><select [(ngModel)]="shortcut.LinkTarget" style="width:98%">
                <option value="0">New Tab</option>
                <option value="1">Current Tab - Reload</option>
                <option value="2">Current Tab - Within D3S</option>
            </select></div>
        </div>     
    </div>
    <div class="row">
        <div class="col s12 m6">
            <div class="FieldName">Override Icon Color</div>
            <input type="checkbox" [ngModel]="showIconColor" (ngModelChange)="showIconColor=$event;(shortcut.IconColor = ($event ? '#FF0000' : null))"/>
        </div>        
        <div class="col s12 m6" *ngIf="showIconColor">
            <div class="FieldName">Icon Color</div>
            <p-colorPicker [(ngModel)]="shortcut.IconColor" name="iconColor"></p-colorPicker>                            
            <input type="text" [(ngModel)]="shortcut.IconColor" name="iconColorText" style="padding:2px;" />
        </div>        
    </div>
    <div class="row">
        <div class="col s12 m6">
            <div class="FieldName">Override Title Color</div>
            <input type="checkbox" [ngModel]="showTitleColor" (ngModelChange)="showTitleColor=$event;(shortcut.TitleColor = ($event ? '#00FF00' : null))"/>
        </div>        
        <div class="col s12 m6" *ngIf="showTitleColor">
            <div class="FieldName">Title Color</div>
            <p-colorPicker [(ngModel)]="shortcut.TitleColor" name="titleColor"></p-colorPicker>       
            <input type="text" [(ngModel)]="shortcut.TitleColor" name="titleColorText" style="padding:2px;" />
        </div>
    </div>
    <div class="row">
        <div class="col s12 m6">
            <div class="FieldName">Override Background Color</div>
            <input type="checkbox" [ngModel]="showBackgroundColor" (ngModelChange)="showBackgroundColor=$event;(shortcut.BackgroundColor = ($event ? '#0000FF' : null))"/>
        </div>        
        <div class="col s12 m6" *ngIf="showBackgroundColor">
            <div class="FieldName">Background Color</div>
            <p-colorPicker [(ngModel)]="shortcut.BackgroundColor" name="backgroundColor"></p-colorPicker>                            
            <input type="text" [(ngModel)]="shortcut.BackgroundColor" name="backgroundColorText" style="padding:2px;" />
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
                    <ig-icon-picker [(ngModel)]="shortcut.Icon" required ngDefaultControl></ig-icon-picker>
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
                <img *ngIf="shortcut.IconPayload == null && iconImage.dataUrl == null && shortcut.FullURL != null" [src]="shortcut.FullURL" style="max-width: 72px; max-height: 127px;" />
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

    private showIconColor: boolean = false;
    private showTitleColor: boolean = false;
    private showBackgroundColor: boolean = false;

    constructor(
        private shortcutService: ShortcutService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    ngOnInit() {
        if (this.shortcut == null) {
            this.isAdding = true;
            this.shortcut = new Shortcut();
            this.shortcut.LinkTarget = LinkTarget.NewWindow;
        } else {
            if (this.shortcut.IconUrl != null)
                this.iconType = 'image';
            else
                this.iconType = 'icon';

            this.showBackgroundColor = (this.shortcut.BackgroundColor != null);
            this.showTitleColor = (this.shortcut.TitleColor != null);
            this.showIconColor = (this.shortcut.IconColor != null);
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
        this.iconImage.dataUrl = null;
        this.shortcut.FullURL = null;
        this.iconImage = new CompanyImage();
        this.onFileChange(null);
    }

    save() {
        this.isLoading = true;

        if (this.iconImage != null && this.iconImage.dataUrl != null) {
            this.shortcut.IconPayload = this.iconImage.dataUrl;
        }

        if (this.isAdding) {
            this.shortcutService.addShortcut(this.shortcut)
                .subscribe(r => {
                    this.showMessageForResult(this.messagesService, r);
                    this.isLoading = false;
                    this.onSave.emit();
                });
        } else {
            this.shortcutService.editShortcut(this.shortcut)
                .subscribe(r => {
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
        //if (this.shortcut.Url == null)
        //    return false;
        if (this.shortcut.Icon == null && this.shortcut.IconUrl == null && this.shortcut.IconPayload == null && (this.iconImage.dataUrl == null || this.iconImage.dataUrl == ''))
            return false;

        return true;
    }

    onFileChange(event): void {
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