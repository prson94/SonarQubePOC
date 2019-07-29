import {Input, Output, Component, EventEmitter, OnInit} from '@angular/core';
import {FusionAttributeTypeCustomQuery} from '../../../models/fusion.model';
import {FusionService} from '../../../services/fusion.service';
import {BaseComponent} from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-fusion-attribute-type-custom-query',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading && !showEditor && !showDelete">
            <header>
                Override Queries For Attribute Types
                <d3s-tile-actions hasClose="true"
                                  (closeClick)="onClose.emit()"
                                  [hasAdd]="true"
                                  (addClick)="selected=null;showEditor=true;"
                                  [hasFilterMode]="false"
                                  [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
            </header>

            <p-table #dt
                     [value]="customqueries"
                     selectionMode="single"
                     [metaKeySelection]="true"
                     [paginator]="true"
                     [rows]="20"
                     [(selection)]="selected"
                     [scrollable]="true"
                     scrollWidth="100%">
                <ng-template pTemplate="colgroup"
                             let-columns>
                    <colgroup>
                        <col>
                        <col style="width:40px">
                        <col style="width:40px">
                    </colgroup>
                </ng-template>
                <ng-template pTemplate="header">
                    <tr>
                        <th>Type</th>
                        <th style="width: 40px"></th>
                        <th style="width: 40px"></th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body"
                             let-item>
                    <tr [pSelectableRow]="item">
                        <td>{{item.FusionAttributeType}}</td>
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
        <d3s-fusion-attribute-type-custom-query-editor *ngIf="showEditor"
                                                       [fusionId]="fusionId"
                                                       [selection]="selected"
                                                       [existingOverrides]="customqueries"
                                                       (saveClick)="saveOverride($event)"
                                                       (closeClick)="closeEditor()">
        </d3s-fusion-attribute-type-custom-query-editor>
        <d3s-delete-form *ngIf="showDelete"
                         [callback]="theDeleteCallback"
                         [itemId]="selected?.ID"
                         [method]="'callback'"
                         [prompt]="'Are you sure you want to delete the selected query override?'"
                         (onCancel)="showDelete=false;">
        </d3s-delete-form>
    `,
    providers: [FusionService]
})

export class FusionAttributeTypeCustomQueryComponent extends BaseComponent implements OnInit {
    @Input() fusionId: number;
    @Input() fusionTypeId: number;
    @Output() onClose = new EventEmitter();

    showDelete: boolean = false;
    showEditor: boolean = false;
    theDeleteCallback: Function;

    customqueries: FusionAttributeTypeCustomQuery[];
    selected: FusionAttributeTypeCustomQuery;

    constructor(
        private fusionService: FusionService,
        private messagesService: MessagesObservableService
    ) {
        super();
        this.theDeleteCallback = this.deleteOverride.bind(this);
    }

    ngOnInit() {
        this.load();
    }

    load(): void {
        this.isLoading = true;
        this.fusionService.getFusionAttributeTypeCustomQueries(this.fusionTypeId, this.fusionId).subscribe(
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
        this.fusionService.saveFusionAttributeTypeCustomQuery(event.override).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.load();

                this.showEditor = false;
            }
        );
    }

    private deleteOverride(id: number): void {
        this.fusionService.deleteFusionAttributeTypeCustomQuery(id).subscribe(
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
