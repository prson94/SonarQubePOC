import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { ResponsibilityTypeRelationRuleSummary } from '../../../models/responsibility-type.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-responsibility-rules',
    templateUrl: './responsibility-rules.component.html',
    providers: [ResponsibilityTypeService]
})

export class ResponsibilityRulesComponent extends BaseComponent implements OnChanges {
    @Input() id: number;
    @Input() title: string = $localize`Ownership Rules`;
    @Input() forceReload: boolean = false;

    @Input() showAddButton: boolean = true;
    @Input() showEditButton: boolean = true;
    @Input() showDeleteButton: boolean = true;

    @Output() onEdit = new EventEmitter();
    @Output() onAdd = new EventEmitter();
    @Output() onDelete = new EventEmitter();
    @Output() onCancel = new EventEmitter();
    @Output() onDeleteDate = new EventEmitter();
    @Output() onFieldsChanged = new EventEmitter();

    @Input() isEditing = false;
    @Input() isAdding = false;
    @Input() isDeleting = false;
    @Input() isDeletingDate = false;

    private rows = new Array<ResponsibilityTypeRelationRuleSummary>();
    private selectedRow = new ResponsibilityTypeRelationRuleSummary();

    private theDeleteCallback: Function;
    private theDeleteDateCallback: Function;

    deleteButtonText = $localize`Run Rule`;
    searchText = $localize`Search...`;

    constructor(
        private responsibilityTypeService: ResponsibilityTypeService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);

        this.theDeleteCallback = this.deleteRule.bind(this);
        this.theDeleteDateCallback = this.deleteDate.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                this.id = changes['id'].currentValue;
                this.isEditing = false;
                this.isAdding = false;
                this.isDeleting = false;
                this.isDeletingDate = false;
            }
        }
        this.load();
    }

    load(): void {
        if (this.id == null)
            return;

        this.isLoading = true;

        this.responsibilityTypeService.getRelationRulesByResponsibilityType(this.id)
            .subscribe(data => {
                this.rows = data;
                this.selectedRow = null;
                this.isLoading = false;
            });
    }

    edit(id: number): void {
        this.selectedRow = this.rows.find(f => f.ID == id);
        this.isEditing = true;
        this.isDeleting = false;
        this.isAdding = false;
        this.onEdit.emit();
    }

    add(): void {
        this.selectedRow = null;
        this.isEditing = true;
        this.isDeleting = false;
        this.onAdd.emit();
    }

    delete(id: number): void {
        this.selectedRow = this.rows.find(f => f.ID == id);
        this.isEditing = false;
        this.isDeleting = true;
        this.isAdding = false;
        this.onDelete.emit();
    }

    clearDate(id: number): void {
        this.selectedRow = this.rows.find(f => f.ID == id);
        this.isEditing = false;
        this.isDeleting = false;
        this.isAdding = false;
        this.isDeletingDate = true;
        this.onDeleteDate.emit();
    }

    editComplete(event) {
        this.isEditing = false;
        this.onCancel.emit();
        this.load();
        this.onFieldsChanged.emit();
    }

    deleteRule(item: any) {
        this.responsibilityTypeService.deleteResponsibilityRulesForType(item.uid, item.ResponsibilityTypeUid).subscribe((res) => {
            this.showMessageForApiResponse(this.messagesService, res[0]);
            if (!res.isError) {
                this.isDeleting = false;
                let index = this.rows.findIndex((f) => f.ID == item.ID);
                if (index >= 0 && index < this.rows.length)
                    this.rows.splice(index, 1);
                this.onFieldsChanged.emit();
            }
        });
    }

    deleteDate(id: number) {
        this.responsibilityTypeService.deleteDate(id).subscribe(res => {
            this.showMessageForResult(this.messagesService, res);
            if (!res.isError) {
                this.isDeletingDate = false;
                this.load();
                this.onFieldsChanged.emit();
            }
        });
    }

    private htmlDecode(val: string): string {
        return val ? String(val).replace(/<[^>]+>/gm, '') : '';
    }

    get deletePrompt(): string {
        return $localize`Are you sure you want to delete the rule[${this.selectedRow?.Name}]?`;
    }

    get runRulePrompt(): string {
        return $localize`Are you sure you want to run the rule [${this.selectedRow?.Name}] now? The rule will be scheduled to run immediately, clearing the current Last Run Date. Ownership rules are scheduled to run automatically on a regular basis. You should only need to use this feature if you are having issues with ownership rules being applied automatically.`;
    }
}