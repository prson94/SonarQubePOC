import {Component, Input, OnChanges, OnInit} from '@angular/core';
import { RelationItem } from '../../../../models/relationship.model';
import { RelationshipsService } from '../../../../services/relationships.service';
import { CompanySettingsService } from '../../../../services/settings.service';
import {BaseComponent} from '../../base.component';

@Component({
    selector: 'd3s-lineage-relations',
    templateUrl: './lineage-relationships.component.html',
    providers: [RelationshipsService]
})

export class LineageRelationshipsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() objectType: string;
    @Input() objectId: number;
    isLoading = false;

    items: RelationItem[] = [];

    constructor(
        private relationshipsService: RelationshipsService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() {
    }

    load() {

        if (this.objectType == null || this.objectId == null) {
            this.items = [];

            return;
        }

        this.isLoading = true;

        this.relationshipsService.getRelations(this.objectType, this.objectId).subscribe(
            data => {
                this.items = data;

                this.isLoading = false;
            }
        );
    }
}
