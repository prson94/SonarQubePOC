import { Input, Component, OnInit, OnChanges, SimpleChange, ViewEncapsulation } from '@angular/core';
import { ObjectDetailService } from '../../services/object-detail.service';
import { BaseComponent } from '../shared/base.component';
import { NymType } from '../../models/object-detail.model';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { CompanySettingsService } from '../../services/settings.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-admin-nym-allocations',
    providers: [ObjectDetailService],
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <table class="striped">
                <thead>
                <tr>
                    <th class="permission-header"></th>
                    <th style="width: 15%;"
                        class="permission-header" i18n>Enabled
                    </th>
                </tr>
                </thead>
                <tbody>
                <tr *ngFor="let nym of nyms" class="nym-row">
                    <td>{{nym.Name}}</td>
                    <td>
                    <p-checkbox igCheckbox [(ngModel)]="nym.Enabled" [disabled]="readonly"></p-checkbox>
                    </td>
                </tr>
                </tbody>
            </table>
            <div *ngIf="!readonly"
                 class="pull-right"
                 style="padding:5px">
                <button pButton
                        i18n-label
                        label="Save Changes"
                        (click)="save()"></button>
            </div>
        </div>
    `
})

export class AdminNymAllocationsComponent extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;

    private nyms: NymType[] = [];

    constructor(
        private messagesService: MessagesObservableService,
        private objectDetailService: ObjectDetailService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.objectID > 0 && this.objectType) this.load();
    }

    private load() {
        this.isLoading = true;

        this.objectDetailService.getNymAllocations(this.objectID, this.objectType).subscribe(
            data => {
                this.nyms = data;

                this.isLoading = false;
            }
        );
    }

    private save() {
        this.objectDetailService.saveNymAllocations(this.objectID, this.objectType, this.nyms).subscribe(
            data => {
                this.showMessageForResult(this.messagesService, data);
            }
        );
    }
}
