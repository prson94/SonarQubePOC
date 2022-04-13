import { Input, Output, Component, EventEmitter, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { SelectItem } from 'primeng/api';
import { LoadFilePostModel, LoadColumn } from '../../../models/load.model';
import { LoadService } from '../../../services/load.service';
import { FormHelper } from '../../../models/form.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-load-form',
    templateUrl: './load.form.html',
    providers: [LoadService],
})

export class LoadForm implements OnInit, OnChanges {
    @Input() title: string = $localize`Upload a Spreadsheet Job`;
    @Output() onComplete = new EventEmitter();
    @Output() onSuccess = new EventEmitter();
    @Output() onError = new EventEmitter();
    @Output() onCancel = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();

    isLoading = false;
    isLoadingColumns = false;
    isLoadingTypes = false;
    isSaving = false;
    actions: SelectItem[];
    selectedAction: string;
    types: SelectItem[];
    selectedType: string;
    notes: string;
    columns: LoadColumn[];
    file: File;
    errorMessage = "";

    saveLabel = $localize`Save`;
    cancelLabel = $localize`Cancel`;

    constructor(private loadService: LoadService) {
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                this.load();
            }
        }
    }

    private load(): void {
        this.actions = this.loadService.getActionOptions();
        this.selectedAction = this.actions[0].value;
        this.loadTypes();
        this.onLoadComplete.emit(null);
    }

    loadTypes(): void {
        this.isLoadingTypes = true;
        this.selectedType = '';
        this.loadService.getTypeOptions(this.selectedAction).subscribe(
            data => {
                this.types = data;

                if (this.types && this.types.length > 0) {
                    this.selectedType = this.types[0].value;
                    this.loadColumns();
                }

                this.isLoadingTypes = false;
            }
        );
    }

    private loadColumns(): void {
        let id, type;

        try {
            id = parseInt(this.selectedType.split('|')[1]);
            type = this.selectedType.split('|')[0];
        } catch (e) {
            return;
        }

        this.isLoadingColumns = true;

        this.loadService.getExpectedColumns(this.selectedAction, type, id).subscribe(
            data => {
                this.columns = data;

                this.isLoadingColumns = false;
            }
        );
    }

    private isRequiredColumn(col: string) {
        let type = this.selectedType.split('|')[0];

        if (type == null) return true;
        type = type.toLowerCase();
        col = col.toLowerCase();

        if (this.selectedAction == 'P' && type == 'artifacttype') {
            if (_.includes(['name', 'subject area'], col) || col.startsWith('parent ')) return true;
            return false;
        }
        if (this.selectedAction == 'P' && type == 'domain') {
            if (_.includes(['name', 'code'], col)) return true;
            return false;
        }
        if (this.selectedAction == 'P' && type == 'domaintype') {
            if (_.includes(['name', 'domain group'], col)) return true;
            return false;
        }
        return true;
    }

    showDetail() {
        return (this.selectedAction && this.selectedAction != '' && this.selectedType && this.selectedType != '');
    }

    private getTemplateDownloadUri() {
        let id, type;
        try {
            id = parseInt(this.selectedType.split('|')[1]);
            type = this.selectedType.split('|')[0];
        } catch (e) {
            return null;
        }
        return `form/Load_ExpectedColumns_ToExcel?action=${this.selectedAction}&id=${id}&type=${type}`;
    }

    private changeFile(e) {
        this.file = e.srcElement.files[0];
    }

    cancel(): void {
        this.onCancel.emit(null);
    }

    save(): void {
        let model = new LoadFilePostModel();

        this.errorMessage = "";
        this.isSaving = true;

        if (this.file) {
            FormHelper.getDataUrl(this.file)
                .then(
                    s => {
                        model.File = s;
                        model.LoadAction = this.selectedAction;
                        model.Type = this.selectedType;
                        model.Notes = this.notes;
                    }
                )
                .then(() => {
                    this.loadService.postLoad(model).subscribe(
                        data => {
                            if (data["type"] == 'error') {
                                this.onError.emit(null);
                                this.errorMessage = data["message"];
                            } else {
                                this.onSuccess.emit(null);
                            }
                            this.isSaving = false;
                            this.onComplete.emit(null);
                        }
                    )
                }
                );
        }
    }
}
