import {
    Input,
    Component,
    EventEmitter,
    Output,
    OnInit,
    ChangeDetectionStrategy,
    ChangeDetectorRef
} from '@angular/core';

import {
    BaseComponent
} from '../shared/base.component';
import {
    GridFilterExpression,
    GridFilterColumn,
    GridFilterFieldType
} from '../../models/grid-definition.model';
import {FieldsObservableService} from '../../services/fieldsObservable.service';
import {setTimeout} from 'core-js';

@Component({
    selector: 'd3s-artifact-top-level-filter',
    templateUrl: 'artifact-top-level-filter.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [FieldsObservableService]
})

export class ArtifactTopLevelFilterComponent extends BaseComponent implements OnInit {
    @Input() fields: GridFilterColumn[];
    @Input() filters: GridFilterExpression[] = [];

    @Output() filterChanged = new EventEmitter();
    @Output() filtersChange = new EventEmitter();

    constructor(
        protected ref: ChangeDetectorRef,
        protected fieldService: FieldsObservableService
    ) {
        super();
    }

    ngOnInit() {
        for (let field of this.fields) {
            if (this.filters) {
                for (let filter of this.filters) {
                    if (`Field${field.id}` == filter.field && filter.value != null) {
                        if (field.columntype == 'dropdownlist') {
                            field.value = filter.value.split("!~!");
                        } else
                            field.value = filter.value;
                    }
                }
            }

            if (field.parentFieldTypeID > 0) {
                field.disabled = true;
            } else {
                field.disabled = false;
            }
        }
        this.ref.markForCheck();
    }

    private resetFilters(): void {
        this.filters = [];

        for (let field of this.fields) {
            if (!field.value || field.value === '') {
                continue;
            }

            field.value = null;
        }

        this.filtersChange.emit(this.filters);
        this.filterChanged.emit();
    }

    onSubmit() {
        this.filters = [];
        //copy field values to filter values
        for (let field of this.fields) {
            if (!field.value || field.value === '') {
                continue;
            }

            let filter = new GridFilterExpression();
            filter.field = field.datafield;

            if (field.columntype == "dropdownlist") {
                let newVal = '';
                if (field.value.length > 0) {
                    for (let item of field.value) {
                        if (newVal.length > 0) {
                            newVal += '!~!';
                        }

                        newVal += item;
                    }

                    filter.value = newVal;
                    filter.condition = "IN";
                    filter.fieldtype = (field.hiddenfield) ? GridFilterFieldType.Hidden : GridFilterFieldType.Normal;
                    this.filters.push(filter);
                }
            } else if (field.columntype == "datetimeinput") {
                filter.condition = "EQUALS";

                var date = new Date(field.value);
                filter.value = date.getMonth() + 1 + "/" + date.getDate() + "/" + date.getFullYear();
                filter.fieldtype = (field.hiddenfield) ? GridFilterFieldType.Hidden : GridFilterFieldType.Normal;
                this.filters.push(filter);
            } else {
                filter.condition = "CONTAINS";
                filter.value = field.value;
                filter.fieldtype = (field.hiddenfield) ? GridFilterFieldType.Hidden : GridFilterFieldType.Normal;
                this.filters.push(filter);
            }
        }

        this.filtersChange.emit(this.filters);
        this.filterChanged.emit();
    }

    public enableParentFilters(givenfield: GridFilterColumn): void {
        for (let field of this.fields) {
            if (`Field${field.parentFieldTypeID}` == givenfield.datafield) {
                this.loadFieldItems(givenfield, field);
            }
        }
    }

    public loadFieldItems(givenparentfield: GridFilterColumn, givenfield: GridFilterColumn): void {
        var fieldId = +givenfield.datafield.replace('Field', '');

        if (givenparentfield.value.length > 0) {
            this.fieldService.getCascadingListFieldValues(fieldId, undefined, givenparentfield.value).subscribe(
                res => {
                    givenfield.disabled = false;
                    givenfield.filteritems = res.map(r => r.Text + '!~!' + r.Value);
                });
        } else {
            givenfield.disabled = true;
            givenfield.filteritems = [];
            givenfield.value = null;
        }

        setTimeout(() => {
            this.ref.markForCheck();
        }, 50);
    }
}
