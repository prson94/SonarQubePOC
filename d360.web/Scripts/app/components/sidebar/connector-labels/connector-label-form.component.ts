import { Component, OnChanges, Input, SimpleChanges, Output, EventEmitter, ChangeDetectorRef, ElementRef } from '@angular/core';
import { ConnectorLabelService } from '../../../services/connectorLabel.service';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { AsyncValidatorService } from '../../../services/async-validators.service';
import { ConnectorLabel } from '../../../models/connectorLabel.model';
@Component({
    selector: 'd3s-connector-label-form',
    templateUrl: './connector-label-form.component.html',
    providers: [ConnectorLabelService, AsyncValidatorService]
})

export class ConnectorLabelsFormComponent implements OnChanges {
    @Input() label: any;
    @Input() isVisible: boolean = false;
    @Input() isSaving: boolean = false;

    @Output() onSave = new EventEmitter<any>();
    @Output() onCancel = new EventEmitter<any>();

    connectorLabelForm = new FormGroup({
        value: new FormControl('', [Validators.required, Validators.maxLength(40)])
    });

    private suggestionResults: string[] = [];
    private suggestionResultsArray: any[] = [];

    selectedValue: any;

    get value() { return this.connectorLabelForm.get('value'); }

    get saveLabel(): string {
        return this.selectedValue ? $localize`Consolidate` : (this.label ? $localize`Save` : $localize`Create`);
    }

    constructor(
        private asyncValidators: AsyncValidatorService,
        private cdRef: ChangeDetectorRef,
        private connectorLabelService: ConnectorLabelService,
        private elRef: ElementRef
    ) {

    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes && changes.label && changes.label.currentValue != changes.label.previousValue) {
            this.load();
        }
        if (changes && changes.isVisible && changes.isVisible.currentValue != changes.isVisible.previousValue) {
            this.load();
            this.setValidators();
        }
    }

    private setValidators() {
        if (!this.label) {
            this.connectorLabelForm.controls['value'].setAsyncValidators([this.asyncValidators.labelUniqueValidator()]);
        }
        else {
            this.connectorLabelForm.controls['value'].clearAsyncValidators();
        }
    }

    load() {
        this.connectorLabelForm.reset();
        this.selectedValue = null;
        if (this.label)
            this.connectorLabelForm.setValue({ value: this.label.Value });
    }

    onSubmit() {
        var objLabel = new ConnectorLabel();
        objLabel.Value = this.connectorLabelForm.value['value'];
        if (this.label) {
            objLabel.uid = this.label.uid;
        }
        var event = { item: objLabel };

        if (this.label && this.selectedValue) {
            event['additionalOption'] = this.selectedValue;
        }

        this.onSave.emit(event);

    }

    OnBlurTrim() {
        let value: string = this.connectorLabelForm.controls['value'].value;
        if (value)
            this.connectorLabelForm.controls['value'].setValue(value.trim());
    }
    onAutocomplete(q: any) {
        this.autoCompleteSelected(null);
        this.connectorLabelService.getAvailableLabels(q.query, false, true, this.label.uid)
            .subscribe((response) => {
                this.suggestionResultsArray = response.map((s) => { return { name: s.Value, UseCount: s.UseCount, uid: s.uid } });
                this.suggestionResults = [];
                this.suggestionResultsArray.forEach(x => this.suggestionResults.push(x.name));

                this.suggestionResultsArray.forEach((s) => {
                    if (s.name.toLowerCase() == this.connectorLabelForm.controls['value'].value.toLowerCase()) {
                        this.autoCompleteSelected(s);
                    }
                });

                this.cdRef.markForCheck();
            });
    }

    onAutocompleteSelect(event) {
        var obj = this.suggestionResultsArray.filter(x => x.name.toLowerCase().trim() == event.toLowerCase().trim())[0];
        this.autoCompleteSelected(obj);
    }

    private autoCompleteSelected($event) {
        this.selectedValue = $event;
    }

}