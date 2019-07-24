import {Input, Output, Component, EventEmitter, OnInit, OnChanges, SimpleChange} from '@angular/core';
import {SelectItem} from 'primeng/primeng';
import {Group, GroupEditorModel, GroupSearchResultModel, ResourceGroup} from '../../../models/group.model';
import {FormEvents, FormHelper} from '../../../models/form.model';
import {JsonResult} from '../../../models/jsonresult.model';
import {GroupService} from '../../../services/group.service';
import * as _ from 'lodash';
import {EditorField} from '../../../models/editor-field.model';
import {ResourcesService} from '../../../services/resources.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-group-form',
    templateUrl: './group.form.html',
    providers: [GroupService, ResourcesService],
})

export class GroupForm implements OnInit, OnChanges, FormEvents {
    @Input() id: number;
    @Input() title: string = "Edit Group";
    @Output() onComplete = new EventEmitter();
    @Output() onSuccess = new EventEmitter();
    @Output() onError = new EventEmitter();
    @Output() onCancel = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    private primaryOwnerField: EditorField;
    private primaryOwnerGrid: boolean = false;

    private secondaryOwnerField: EditorField;
    private secondaryOwnerGrid: boolean = false;

    private model: GroupEditorModel;
    private isLoading = false;
    private initialItem: GroupEditorModel;

    constructor(private groupService: GroupService, private messagesService: MessagesObservableService) {
        this.model = new GroupEditorModel();
        this.model.group = new Group();
    }

    ngOnInit() {
        this.initialItem = _.cloneDeep(this.model);


    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                this.load();
                this.initialItem = _.cloneDeep(this.model);
            }
        }
    }

    private load(): void {
        this.isLoading = true;
        this.groupService.getGroup(this.id).subscribe(
            d => {
                this.model = d;

                //TODO: primeng does not currently support optgroups, I'm ignoring them here
                FormHelper.mapSelectItems(this.model.resourceList);
                this.onLoadComplete.emit(null);

                this.isLoading = false;
            }
        );

        this.onLoadComplete.emit(null);
    }

    private cancel(): void {
        this.onCancel.emit(null);
    }

    private save(): void {
        this.isLoading = true;
        if (this.id > 0) {
            this.groupService.putGroup(this.model.group).subscribe(
                r => {
                    if (r.type == 'confirm') {
                        this.onSuccess.emit(r);
                    } else if (r.type == 'error') {
                        this.onError.emit(r);
                    }

                    this.isLoading = false;
                    this.onComplete.emit(null);
                }
            );
        } else {
            this.groupService.postGroup(this.model.group).subscribe(
                r => {
                    if (r.type == 'confirm') {
                        this.onSuccess.emit(r);
                    } else if (r.type == 'error') {
                        this.onError.emit(r);
                    }

                    this.isLoading = false;
                    this.onComplete.emit(null);
                }
            );
        }
    }

    private showPrimaryOwnerResourceGrid() {
        this.secondaryOwnerGrid = false;
        this.primaryOwnerField = new EditorField();
        this.primaryOwnerField.TypeaheadUri = `form/GetGroupUserList?id=0`;
        this.primaryOwnerField.FieldName = "resources";
        this.primaryOwnerGrid = true;
    }

    private set primaryFieldValue(value) {
        this.primaryOwnerField.Value = value;
        if (this.primaryOwnerField.Value != null && this.primaryOwnerField.Value.length > 0) {
            let x = this.primaryOwnerField.Value[0];
            this.model.group.PrimaryOwnerResourceID = x.split('|')[1];
            this.model.group.PrimaryOwnerName = x.split('|')[2];
            this.primaryOwnerGrid = false;
        }
    }

    private showSecondaryOwnerResourceGrid() {
        this.primaryOwnerGrid = false;
        this.secondaryOwnerField = new EditorField();
        this.secondaryOwnerField.TypeaheadUri = `form/GetGroupUserList?id=0`;
        this.secondaryOwnerField.FieldName = "resources";
        this.secondaryOwnerGrid = true;
    }

    private set secondaryFieldValue(value) {
        this.secondaryOwnerField.Value = value;
        if (this.secondaryOwnerField.Value != null && this.secondaryOwnerField.Value.length > 0) {
            let x = this.secondaryOwnerField.Value[0];
            this.model.group.SecondaryOwnerResourceID = x.split('|')[1];
            this.model.group.SecondaryOwnerName = x.split('|')[2];
            this.secondaryOwnerGrid = false;
        }
    }
}
