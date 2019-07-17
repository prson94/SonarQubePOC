import {Input, Output, Component, EventEmitter, OnInit} from '@angular/core';
import {Router} from '@angular/router';
import {FusionConfiguration, FusionType, FusionSchedule, FusionScheduleDay} from '../../../models/fusion.model';
import {FusionService} from '../../../services/fusion.service';
import {BaseComponent} from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-fusion-schedule',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading && !showEditor && !showDelete">
            <header>Agent Execution Schedule
                <d3s-tile-actions hasClose="true"
                                  (closeClick)="onClose.emit()"
                                  [hasAdd]="true"
                                  (addClick)="selected=null;showEditor=true;"
                                  [hasFilterMode]="false"
                                  [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
            </header>
            <p-table #dt
                     [value]="schedules"
                     selectionMode="single"
                     [metaKeySelection]="true"
                     [paginator]="true"
                     [rows]="20"
                     [(selection)]="selected">
                <ng-template pTemplate="header">
                    <tr>
                        <th>Day</th>
                        <th>Time (UTC)</th>
                        <th>Full Refresh?</th>
                        <th style="width: 40px"></th>
                        <th style="width: 40px"></th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body"
                             let-item>
                    <tr [pSelectableRow]="item">
                        <td>{{item.DayText}}</td>
                        <td>{{item.Time}}</td>
                        <td>
                            <span>
                                <i *ngIf="item.ForceRefresh"
                                   class="fa fa-check enabled"
                                   title="True"></i>
                                <i *ngIf="!item.ForceRefresh"
                                   class="fa fa-times disabled"
                                   title="False"></i>
                            </span>
                        </td>
                        <td>
                            <div class="RowTools">
                                <a style="cursor:pointer;"
                                   (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                            </div>
                        </td>
                        <td>
                            <div class="RowTools">
                                <a style="cursor:pointer;"
                                   (click)="selected=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
                            </div>
                        </td>
                    </tr>
                </ng-template>
                <ng-template *ngIf="dt.totalRecords"
                             pTemplate="summary">
                    <d3s-grid-paging-info [first]="dt.first"
                                          [rows]="dt.rows"
                                          [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                </ng-template>
            </p-table>
        </div>
        <d3s-fusion-schedule-editor *ngIf="showEditor"
                                    [selection]="selected"
                                    (saveClick)="saveSchedule($event)"
                                    (closeClick)="closeEditor()">
        </d3s-fusion-schedule-editor>
        <d3s-delete-form *ngIf="showDelete"
                         [callback]="theDeleteCallback"
                         [itemId]="selected?.ID"
                         [method]="'callback'"
                         [prompt]="'Are you sure you want to delete the selected schedule?'"
                         (onCancel)="showDelete=false;">
        </d3s-delete-form>
    `,
    providers: [FusionService]
})

export class FusionScheduleComponent extends BaseComponent implements OnInit {
    @Input() fusionId: number;
    @Input() fusionTypeId: number;
    @Output() onClose = new EventEmitter();

    showDelete: boolean = false;
    showEditor: boolean = false;
    theDeleteCallback: Function;

    schedules: FusionSchedule[];
    selected: FusionSchedule;

    constructor(private fusionService: FusionService,
                private messagesService: MessagesObservableService
    ) {
        super();
        this.theDeleteCallback = this.deleteScheduleItem.bind(this);
    }

    ngOnInit() {
        this.load();
    }

    load(): void {
        this.isLoading = true;
        this.fusionService.getFusionConfigurationSchedules(this.fusionTypeId, this.fusionId).subscribe(
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

        this.fusionService.saveFusionConfigurationSchedule(event.schedule).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.load();
                this.showEditor = false;
            }
        );
    }

    private deleteScheduleItem(id: number): void {
        this.fusionService.deleteFusionConfigurationSchedule(id).subscribe(
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
