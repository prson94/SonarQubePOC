import { Component, Input, OnChanges, SimpleChange } from '@angular/core';
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
    templateUrl: 'admin-nym-allocations.component.html'
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
        if (this.objectID > 0 && this.objectType) {this.load();}
    }

    private load() {
        this.isLoading = true;

        this.objectDetailService.getNymAllocations(this.objectID, this.objectType).subscribe(
            (data) => {
                this.nyms = data;

                this.isLoading = false;
            }
        );
    }

    private save() {
        this.objectDetailService.saveNymAllocations(this.objectID, this.objectType, this.nyms).subscribe(
            (data) => {
                this.showMessageForResult(this.messagesService, data);
            }
        );
    }
}
