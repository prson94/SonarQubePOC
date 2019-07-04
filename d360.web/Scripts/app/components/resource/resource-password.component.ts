import { Component, EventEmitter, Output } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { UriBasedService } from '../../services/uri-based.service';
import { MessagesService } from '../../services/messages.service';

@Component({
    selector: 'd3s-resource-password',
    template: 
    `
<header>Edit Your Password</header>
<div class="row">
    <div class="col s12 m6">
        <div class="FieldNameRequired">Current Password</div>
        <div>
            <input [(ngModel)]="currentPassword" type="password" style="width:98%" />
        </div>
    </div>
</div>
<div class="row">
    <div class="col s12 m6">
        <div class="FieldNameRequired">New Password</div>
        <div>
            <input [ngModel]="newPassword" (ngModelChange)="newPassword = $event; validate();" type="password" style="width:98%" />
        </div>
        <div class="errorMessage" *ngIf="newPassword.length > 0 && !newPasswordValid">
            New password must be between 7 and 25 characters in length; at least 1 uppercase character; at least 1 lowercase chacter; at least 1 number; at least 1 special character
        </div>
    </div>
    <div class="col s12 m6">
        <div class="FieldNameRequired">Confirm New Password</div>
        <div>
            <input [ngModel]="newPassword2" (ngModelChange)="newPassword2 = $event; validate();" type="password" style="width:98%" />
        </div>
        <div class="errorMessage" *ngIf="newPassword2.length > 0 && !newPassword2Match">
           The passwords you entered do not match
        </div>
    </div>
</div>
<div class="row" style="padding-top:15px;">
    <div class="col s12">
        <button pButton label="Cancel" type="button" (click)="onClose.emit()"></button>
        <button pButton label="Save" type="button" (click)="save()" [disabled]="isLoading || newPassword.length == 0 || newPassword2.length == 0 || !newPassword2Match || currentPassword.length == 0 || !newPasswordValid"></button>
    </div>
</div>
`,
    providers: [UriBasedService]
})

export class ResourcePasswordComponent extends BaseComponent{
    @Output() onSave = new EventEmitter();
    @Output() onClose = new EventEmitter();

    currentPassword = "";
    newPassword = "";
    newPasswordValid = true;
    newPassword2 = "";
    newPassword2Match = true;

    passwordRegex = /(?=^.{7,25}$)((?=.*\d)(?=.*[A-Z])(?=.*[a-z])|(?=.*\d)(?=.*[^A-Za-z0-9])(?=.*[a-z])|(?=.*[^A-Za-z0-9])(?=.*[A-Z])(?=.*[a-z])|(?=.*\d)(?=.*[A-Z])(?=.*[^A-Za-z0-9]))^.*/;

    constructor(
        private uriBasedService: UriBasedService,
        protected messagesService: MessagesService) {
        super();
    }

    save() {
        let values: any = {};
        values.CurrentPassword = this.currentPassword;
        values.NewPassword = this.newPassword;
        values.ConfirmNewPassword = this.newPassword2;
        //force edit instead of create
        values.ID = -1;

        this.isLoading = true;
        this.uriBasedService.saveItem(null, "form/dynamicedit/edit/resourceselfpassword", values)
            .subscribe(result => {
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    this.onSave.emit();
                }
            });
    }

    validate() {
        if (!this.passwordRegex.test(this.newPassword))
            this.newPasswordValid = false;
        else
            this.newPasswordValid = true;

        if (this.newPassword != null && this.newPassword.length > 0 && this.newPassword2 != this.newPassword && this.newPasswordValid)
            this.newPassword2Match = false;
        else
            this.newPassword2Match = true;
    }
};

