import { Input, Component, OnInit, OnChanges, SimpleChanges, ChangeDetectorRef, ChangeDetectionStrategy, ViewEncapsulation } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { MetricAssetDefinitionViewModel } from '../../../models/metrics.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { FormBuilder, Validators } from '@angular/forms';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import * as _ from 'lodash';
import { BaseMeasureEditorComponent } from './measure-editor-base.component';
import { CompanySettingsService } from '../../../services/settings.service';
import '@angular/localize/init';

@Component({
    selector: 'external-measure-editor',
    templateUrl: './measure-editor-external.component.html',
    providers: [MetricsService, FieldsObservableService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['measure-editor.less']
})
export class ExternalMeasureEditorComponent extends BaseMeasureEditorComponent implements OnInit, OnChanges {
    @Input() scoreData: any;

    delayedReload = _.debounce(() => {
        this.load();
    }, 200);

    labelRequired = $localize`Value required`;
    labelResType = $localize`Choose a responsibility type`;
    labelRelType = $localize`Choose a relationship type`;
    labelPredicate = $localize`Choose a predicate`;

    labelMatchAll = $localize`Match all conditions`;
    labelMatchAny = $localize`Match any condition`;

    addButtonLabel = $localize`Add condition group`;

    constructor(protected metricsService: MetricsService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        protected fieldsService: FieldsObservableService,
        protected fb: FormBuilder,
        protected cdRef: ChangeDetectorRef
    ) {
        super(fieldsService, metricsService, messagesService, settingsService, cdRef);
    }

    ngOnChanges(changes: SimpleChanges): void {
        let requiredLoad = false;

        if (changes['uid'] && (changes['uid'].currentValue != changes['uid'].previousValue && !changes['uid'].firstChange)) {
            this.isLoading = true;
            requiredLoad = true;
        }

        if (requiredLoad)
            this.delayedReload();

        this.cdRef.markForCheck();
    }

    ngOnInit() {
        this.metricForm = this.fb.group({
            name: ['', [Validators.required, this.isEmptyString()]],
            description: null,
            effectiveDate: null
        });
        this.metricForm.updateValueAndValidity();
        this.load();
    }

    ngAfterViewInit() {
        this.originalModel = _.cloneDeep(this.model);
        this.originalEffectiveDate = new Date(this.displayEffectiveDate?.toString());
        if (this.uid) {
            this.metricForm?.valueChanges.subscribe(() => {
                setTimeout(() => {
                    this.checkModelChanged();
                })
            });

            this.cdRef.detectChanges();
        } else {
            this.hasModelChanged = true;
        }
    }

    updateFormValidity(event) {
        if (this.groups && this.groups.length > 0) {
            this.groups.forEach(x => { x.refreshBadgeCounts(); });
        }
        this.checkModelChanged();
        this.cdRef.markForCheck();
    }

    load() {
        this.setFormPropertiesBasedOnMode();

        if (!this.model.Definition) {
            this.model.Definition = new MetricAssetDefinitionViewModel();
        }

        this.onResize(null);
    }

    save() {
        this.model.Definition = null;

        // Common
        this.saveMeasure();
    }

    cancel() {
        this.load();
        this.onCancel.emit(this.model.Name);
        this.model = null;
    }

    checkModelChanged() {
        if (!this.model)
            return false;

        if (
            this.model
            && this.originalModel
            && (
                this.model.Name &&
                this.originalModel.Name != this.model.Name
                || (this.originalModel.Description && this.originalModel.Description != this.model.Description)
                || (!this.originalModel.Description && !(!this.model.Description || this.model.Description == null || this.model.Description.trim() == ""))
                || (this.displayEffectiveDate && this.getFormattedEffectiveDate(this.originalEffectiveDate).getTime() !== this.getFormattedEffectiveDate(this.displayEffectiveDate).getTime())
            )
        ) {
            this.hasModelChanged = true;
        } else {
            this.hasModelChanged = false;
        }

        if (this.verb == "Edit") {
            if (this.hasModelChanged) {
                this.closeLabel = $localize`Discard Changes`;
            } else {
                this.closeLabel = $localize`Close`;
            }
        }
        this.cdRef.markForCheck();
    }
}