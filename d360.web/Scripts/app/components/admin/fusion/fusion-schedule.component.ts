import {Component, EventEmitter, Input, OnDestroy, OnInit, Output} from '@angular/core';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionSchedule, FusionScheduleDay} from '../../../models/fusion.model';

import {FusionService} from '../../../services/fusion.service';
import {MessagesService} from '../../../services/messages.service';

import {BaseComponent} from '../../shared/base.component';

@Component({
    selector: 'd3s-fusion-schedule',
    templateUrl: './fusion-schedule.component.html',
    providers: [FusionService]
})

export class FusionScheduleComponent extends BaseComponent implements OnInit {
    @Input() fusionId: number;
    @Input() fusionTypeId: number;
    @Output() onClose = new EventEmitter();

    destroySubject$: Subject<void> = new Subject();

    showDelete: boolean = false;
    showEditor: boolean = false;
    theDeleteCallback: Function;

    schedules: FusionSchedule[];
    selected: FusionSchedule;

    constructor(
        private fusionService: FusionService,
        private messagesService: MessagesService
    ) {
        super();
        this.theDeleteCallback = this.deleteScheduleItem.bind(this);
    }

    ngOnInit() {
        this.load();
    }

    load(): void {
        this.isLoading = true;

        this.fusionService
            .getFusionConfigurationSchedules(this.fusionTypeId, this.fusionId)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
            data => {
                for (let item of data) {
                    item.DayText = FusionScheduleDay[item.Day];
                }

                this.schedules = data;

                this.isLoading = false;
            }
        );
    }

    private closeEditor(): void {
        this.showEditor = false;
    }

    private saveSchedule(event): void {
        event.schedule.FusionID = this.fusionId;

        this.fusionService
            .saveFusionConfigurationSchedule(event.schedule)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.load();
                this.showEditor = false;
            }
        );
    }

    private deleteScheduleItem(id: number): void {
        this.fusionService
            .deleteFusionConfigurationSchedule(id)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;

                if (result.type != 'error') {
                    this.schedules = this.schedules.filter(x => x.ID != id);
                }
            }
        );
    }
}
