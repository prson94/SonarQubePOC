import {
    Component,
    Input,
    Output,
    EventEmitter,
    OnInit
} from '@angular/core';

import {Contract} from '../../../models/organization.model';
import {OrganizationsService} from '../../../services/organizations.service';

import {BaseComponent} from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-organization-contract-editor',
    providers: [OrganizationsService],
    template: `
        <header>{{ isAdding ? 'Add' : 'Edit' }} Contract
        </header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <form (ngSubmit)="save()"
                  #contractForm="ngForm">
                <div class="row"
                     style="padding-bottom:10px">
                    <div class="col s6">
                        <div class="FieldName">Title</div>
                        <div>
                            <input type="text"
                                   [(ngModel)]="contract.Title"
                                   style="width: 98%"
                                   name="title"
                                   required
                                   autocomplete="off"
                                   #title="ngModel"/>
                            <div [hidden]="title.pristine || title.valid"
                                 class="errorMessage">* Title is required
                            </div>
                        </div>
                    </div>
                    <div class="col s6">
                        <div class="FieldName">Contract Type</div>
                        <div>
                            <select [(ngModel)]="contract.ContractType"
                                    name="contractType"
                                    style="width: 98%"
                                    required
                                    #type="ngModel">
                                <option></option>
                                <option *ngFor="let c of contractType"
                                        [value]="c.value">{{ c.label }}</option>
                            </select>
                            <div [hidden]="type.pristine || type.valid"
                                 class="errorMessage">* Contract Type is required
                            </div>
                        </div>
                    </div>
                </div>
                <div class="row"
                     style="padding-bottom: 10px">
                    <div class="col s12">
                        <div class="FieldName">Body</div>
                        <div>
                            <p-editor [style]="{'height':'150px'}"
                                      [(ngModel)]="contract.Body" required
                                      name="body">
                            </p-editor>
                        </div>
                    </div>
                </div>
                <div class="row"
                     style="padding-bottom: 10px"
                     *ngIf="!isAdding">
                    <div class="col s6">
                        <div class="FieldName">Last Updated</div>
                        <div>{{ contract.UpdatedOn | date : 'short' }}</div>
                    </div>
                    <div class="col s6">
                        <div class="FieldName">Last published</div>
                        <div>{{ contract.PublishedOn == null ? 'Never' : (contract.PublishedOn | date : 'short') }}</div>
                    </div>

                </div>
                <div class="row"
                     style="padding-bottom: 10px">
                    <div class="col s12">
                        <button pButton
                                type="submit"
                                label="Save"
                                (click)="save()"
                                [disabled]="!contractForm.form.valid"></button>
                        <button pButton
                                type="submit"
                                label="Save & Publish"
                                (click)="save(true)"
                                [disabled]="!contractForm.form.valid"></button>
                        <button pButton
                                type="button"
                                label="Close"
                                (click)="onClose.emit()"></button>
                    </div>
                </div>
            </form>
        </div>
    `
})

export class AdminOrganizationContractEditorComponent extends BaseComponent implements OnInit {
    @Input() contractId = -1;
    @Input() organizationId: number = null;
    @Output() onClose = new EventEmitter();
    @Output() onSave = new EventEmitter();
    contract: Contract;
    isAdding = true;
    isLoading = false;

    contractType = [
        {value: 1, label: 'Organization Terms of Use'},
        {value: 2, label: 'User Terms of Use'}
    ];

    constructor(
        private organizationsService: OrganizationsService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
    }

    ngOnInit() {
        if (this.contractId <= 0) {
            this.isAdding = true;
            this.contract = new Contract();
            this.contract.OrganizationID = this.organizationId;
        } else {
            this.isAdding = false;
            this.load();
        }
    }

    load() {
        this.isLoading = true;
        this.organizationsService.getContract(this.contractId)
            .subscribe(
                r => {
                    this.contract = r;

                    this.isLoading = false;
                }
            );
    }

    save(publish: boolean = false) {
        if (this.isLoading == false) {
            this.isLoading = true;
        } else {
            return;
        }

        if (this.isAdding) {
            this.organizationsService.postContract(this.contract, publish)
                .subscribe(
                    r => {
                        this.showMessageForResult(this.messagesService, r);

                        this.isLoading = false;
                        this.onSave.emit(r);
                    }
                )
            ;
        } else {
            this.organizationsService.putContract(this.contract, publish)
                .subscribe(
                    r => {
                        this.showMessageForResult(this.messagesService, r);

                        this.isLoading = false;
                        this.onSave.emit(r);
                    }
                )
            ;
        }
    }
}
