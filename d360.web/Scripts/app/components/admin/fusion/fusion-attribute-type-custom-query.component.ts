import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionAttributeTypeCustomQuery} from '../../../models/fusion.model';

import {FusionService} from '../../../services/fusion.service';
import {MessagesService} from '../../../services/messages.service';

import {BaseComponent} from '../../shared/base.component';

@Component({
    selector: 'd3s-fusion-attribute-type-custom-query',
    templateUrl: './fusion-attribute-type-custom-query.component.html',
    providers: [FusionService]
})

export class FusionAttributeTypeCustomQueryComponent extends BaseComponent implements OnInit {
    @Input() fusionId: number;
    @Input() fusionTypeId: number;
    @Output() onClose = new EventEmitter();

    destroySubject$: Subject<void> = new Subject();

    showDelete: boolean = false;
    showEditor: boolean = false;
    theDeleteCallback: Function;

    customqueries: FusionAttributeTypeCustomQuery[];
    selected: FusionAttributeTypeCustomQuery;

    constructor(
        private fusionService: FusionService,
        private messagesService: MessagesService
    ) {
        super();

        this.theDeleteCallback = this.deleteOverride.bind(this);
    }

    ngOnInit() {
        this.load();
    }

    load(): void {
        this.isLoading = true;

        this.fusionService
            .getFusionAttributeTypeCustomQueries(this.fusionTypeId, this.fusionId)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                data => {
                    this.customqueries = data;
                    this.isLoading = false;
                }
            );
    }

    private closeEditor(): void {
        this.showEditor = false;
    }

    private saveOverride(event): void {
        event.override.FusionID = this.fusionId;

        this.fusionService
            .saveFusionAttributeTypeCustomQuery(event.override)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                result => {
                    this.showMessageForResult(this.messagesService, result);
                    this.load();

                    this.showEditor = false;
                }
            );
    }

    private deleteOverride(id: number): void {
        this.fusionService
            .deleteFusionAttributeTypeCustomQuery(id)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                result => {
                    this.showMessageForResult(this.messagesService, result);
                    this.showDelete = false;

                    if (result.type != 'error') {
                        this.customqueries = this.customqueries.filter(x => x.ID != id);
                    }
                }
            );
    }
}
