import {Component, Input, SimpleChange} from '@angular/core';
import {AttributeTypeService} from '../../../services/attribute-type.service';
import {BaseComponent} from '../../shared/base.component';
import {AttributeTypeAllocation} from '../../../models/attribute-type.model';
import * as _ from 'lodash';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-attribute-allocation',
    providers: [],
    templateUrl: './admin-attribute-allocation.component.html'
})

export class AdminAttributeAllocationComponent extends BaseComponent {
    @Input() attributeID: number;

    private selected: AttributeTypeAllocation;
    private allocations: AttributeTypeAllocation[] = [];
    private showEditor: boolean;
    private showDelete: boolean;
    private editParams: any[];

    theDeleteCallback: Function;

    constructor(
        private messagesService: MessagesObservableService,
        private attributeTypeService: AttributeTypeService
    ) {
        super();

        this.theDeleteCallback = this.deleteAttributeAllocation.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.attributeID != null) {
            this.load();
        }
    }

    private load() {
        this.isLoading = true;

        this
            .attributeTypeService
            .getAttributeTypeAllocations(this.attributeID)
            .subscribe(result => {
                this.allocations = result;
                this.isLoading = false;
            });
    }

    private editItem() {
        this.editParams = [];
        this.editParams.push(this.attributeID);
        this.editParams.push(this.selected.ObjectType);
        this.editParams.push(this.selected.ObjectID);
        this.showEditor = true;
    }

    private deleteAttributeAllocation(id: number) {
        this.isLoading = true;

        this
            .attributeTypeService
            .deleteAttributeTypeAllocations(
                this.attributeID,
                this.selected.ObjectID,
                this.selected.ObjectType
            )
            .subscribe(result => {
                this.showMessageForResult(this.messagesService, result);

                if (result.type != 'error') {
                    let index = this.allocations.findIndex(x => (x.ObjectID == this.selected.ObjectID && x.ObjectType == this.selected.ObjectType));

                    if (index >= 0 && index < this.allocations.length) {
                        this.allocations.splice(index, 1);
                    }
                }

                this.showDelete = false;
                this.isLoading = false;
            });
    }

    private saveAllocation(data) {
        /* FIXME: identical code. Duplication. */
        if (data.action == 'new') {
            this.isLoading = true;

            this
                .attributeTypeService
                .addAttributeTypeAllocations(
                    data.item.ObjectTypeInfo,
                    data.item.AllowMultipleEntries,
                    this.attributeID)
                .subscribe(result => {
                    this.showMessageForResult(this.messagesService, result);

                    if (result.type != 'error') {
                        this.load();
                    }

                    this.isLoading = false;
                    this.showEditor = false;
                });

        } else {
            this.isLoading = true;

            this
                .attributeTypeService
                .editAttributeTypeAllocations(
                    data.item.ObjectTypeInfo,
                    data.item.AllowMultipleEntries,
                    this.attributeID)
                .subscribe(result => {
                    this.showMessageForResult(this.messagesService, result);

                    if (result.type != 'error') {
                        this.load();
                    }

                    this.isLoading = false;
                    this.showEditor = false;
                });

        }

        this.showEditor = false;
    }

    private columnSort(event) {
        /* event.field = Field to sort */
        /* event.order = Sort order, 1 ascending , -1 descending */
        this.allocations = _.orderBy(
            this.allocations,
            [
                item => item[event.field]
                    ? item[event.field].toLowerCase()
                    : item[event.field]],
            [
                event.order == -1
                    ? 'desc'
                    : 'asc'
            ]
        );
    }

    add() {
        this.showEditor = true;
        this.editParams = [];
        this.selected = null;
    }
}
