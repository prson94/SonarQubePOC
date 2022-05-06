import { Component, EventEmitter, Output } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { UriBasedService } from '../../services/uri-based.service';
import { ResourceApiModel } from '../../models/resource.model';
import { ResourcesService } from '../../services/resources.service';
import { CompanySettingsService } from '../../services/settings.service';
import '@angular/localize/init';

@Component({
    selector: 'd3s-resource-password',
    template:
        `
<header>Edit Your Password</header>
<div class="row">
    <div class="col s12 m6">
        <div class="FieldNameRequired" i18n>Current Password</div>
        <div>
            <input [(ngModel)]="currentPassword" type="password" style="width:98%" />
        </div>
    </div>
</div>
<div class="row">
    <div class="col s12 m6">
        <div class="FieldNameRequired" i18n>New Password</div>
        <div>
            <input [ngModel]="newPassword" (ngModelChange)="newPassword = $event; validate();" type="password" style="width:98%" />
        </div>
        <div class="errorMessage" *ngIf="newPassword.length > 0 && !newPasswordValid" i18n>
            New password must be between 7 and 25 characters in length; at least 1 uppercase character; at least 1 lowercase chacter; at least 1 number; at least 1 special character
        </div>
        <div class="errorMessage" *ngIf="newPassword.length > 0 && newPasswordValid && SamePasswordMatch" i18n>
            New password may not be the same as old password.
        </div>
    </div>
    <div class="col s12 m6">
        <div class="FieldNameRequired" i18n>Confirm New Password</div>
        <div>
            <input [ngModel]="newPassword2" (ngModelChange)="newPassword2 = $event; validate();" type="password" style="width:98%" />
        </div>
        <div class="errorMessage" *ngIf="newPassword2.length > 0 && !newPassword2Match" i18n>
           The passwords you entered do not match
        </div>
    </div>
</div>
<div class="row" style="padding-top:15px;">
    <div class="col s12">
        <button pButton i18n-label label="Cancel" type="button" (click)="onClose.emit()"></button>
        <button pButton i18n-label label="Save" type="button" (click)="save()" [disabled]="isLoading || newPassword.length == 0 || newPassword2.length == 0 || !newPassword2Match || SamePasswordMatch || currentPassword.length == 0 || !newPasswordValid"></button>
    </div>
</div>
`,
    providers: [ResourcesService, UriBasedService]
})

export class ResourcePasswordComponent extends BaseComponent {
    @Output() onSave = new EventEmitter();
    @Output() onClose = new EventEmitter();

    private sub: any;
    private resourceId = -1;
    private items: any[] = [];
    private resource: any;

    currentPassword = "";
    newPassword = "";
    newPasswordValid = true;
    newPassword2 = "";
    newPassword2Match = true;
    SamePasswordMatch = false;

    passwordRegex = /(?=^.{7,25}$)((?=.*\d)(?=.*[A-Z])(?=.*[a-z])|(?=.*\d)(?=.*[^A-Za-z0-9])(?=.*[a-z])|(?=.*[^A-Za-z0-9])(?=.*[A-Z])(?=.*[a-z])|(?=.*\d)(?=.*[A-Z])(?=.*[^A-Za-z0-9]))^.*/;

    constructor(
        private uriBasedService: UriBasedService,
        private resourcesService: ResourcesService,
        private route: ActivatedRoute,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    save() {
        const user = new ResourceApiModel;

        this.sub = this.route.params.subscribe(params => {
            this.resourceId = +params['resourceId']; // (+) converts string 'id' to a number
            this.resourcesService.getResource(this.resourceId)
                .subscribe(r => {
                    this.items = r.items;
                    if (this.items.length > 0) {
                        this.resource = this.items[0];

                        user.FirstName = this.resource.FirstName;
                        user.LastName = this.resource.LastName;
                        user.uid = this.resource.uid;
                        user.State = this.resource.State;
                        user.Username = this.resource.Email;
                        user.IsAdministrator = this.resource.IsAdministrator;

                        user.Fields = new Object();

                        user.Fields['NewPassword'] = this.newPassword;
                        user.Fields['CurrentPassword'] = this.currentPassword;

                        this.isLoading = true;
                        this.resourcesService.saveResource(user, false, true)
                            .subscribe(
                                result => {
                                    this.isLoading = false;
                                    if (result.Message == "" && result.Success) {
                                        result.Message = $localize`Password successfully updated...`;
                                    }
                                    this.showMessageForApiResult(this.messagesService, result, $localize`Password successfully updated...`);
                                    if (result.Success) {
                                        this.onSave.emit();
                                    }
                                }
                            );
                    }
                });
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
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

        if (this.newPassword != null && this.newPassword.length > 0 && this.currentPassword == this.newPassword && this.newPasswordValid)
            this.SamePasswordMatch = true;
        else
            this.SamePasswordMatch = false;
    }
}

