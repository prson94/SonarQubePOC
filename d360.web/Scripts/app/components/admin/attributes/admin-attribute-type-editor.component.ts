import {Component, EventEmitter, Input, Output} from '@angular/core';
import {FieldsService} from '../../../services/fields.service';
import {AttributeTypeService} from '../../../services/attribute-type.service';
import {AttributeType} from '../../../models/attribute-type.model';
import {DropdownOption} from '../../../models/dropdown.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-attribute-type-editor',
    templateUrl: './admin-attribute-type-editor.component.html',
    providers: [AttributeTypeService, FieldsService],
})

export class AdminAttributeTypeEditor {
    @Input() attribute: AttributeType;
    @Input() parentID: number;

    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();

    action: string = "Edit";
    isLoading: boolean = false;

    editedAttribute: AttributeType;
    categoryTypes: DropdownOption[] = [];
    fieldTypes: DropdownOption[] = [];

    constructor(
        private attributeTypeService: AttributeTypeService,
        private fieldsService: FieldsService
    ) {}

    ngOnInit() {
        if (this.attribute != undefined) {
            this.editedAttribute = _.cloneDeep(this.attribute);
            if (this.editedAttribute.DisplayFormat == null) this.editedAttribute.DisplayFormat = "";
            this.loadAttributeFields();
        } else {
            this.editedAttribute = new AttributeType();
            this.editedAttribute.ParentID = this.parentID;
            this.editedAttribute.ShowNameInTree = true;
            this.editedAttribute.DisplayFormat = "";
            this.editedAttribute.AttributeTypeCategoryID = 0;
            this.action = "Add";
        }

        if (this.editedAttribute.ParentID <= 0 && this.editedAttribute.AttributeTypeCategoryID == null) {
            this.editedAttribute.AttributeTypeCategoryID = 0;
        }

        this.loadCategoryTypes(this.editedAttribute.ParentID);
    }

    onSubmit() {
        this.saveClick.emit({attribute: this.editedAttribute, action: this.attribute ? "new" : "edit"});
    }

    private loadAttributeFields() {
        this.fieldsService.getFields(this.editedAttribute.ID, 'AttributeType')
            .then(result => {
                this.fieldTypes = [];
                for (let field of result) {
                    this.fieldTypes.push({title: field.FriendlyName, value: '{' + field.Name + '}'});
                }
            });
    }

    private loadCategoryTypes(parentID?: number) {
        this.isLoading = true;

        this
            .attributeTypeService
            .getAttributeCategoryTypes()
            .subscribe(result => {
                this.categoryTypes = result;
                this.isLoading = false;
            });
    }

    private fieldTokenSelect(item) {
        this.editedAttribute.DisplayFormat += item.value;
    }
}
