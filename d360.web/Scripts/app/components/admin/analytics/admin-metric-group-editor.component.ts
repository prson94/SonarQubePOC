import { Input, Component, EventEmitter, Output } from '@angular/core';
import { MetricsService } from '../../../services/metrics.service';
import { Group, GroupForm } from '../../../models/metrics.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesService } from '../../../services/messages.service';

@Component({
    selector: 'd3s-admin-metric-group-editor',
    template: ` 
                <header>{{verb}} Group</header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading">
                    <div class="row">
                        <div class="col s12">
                            <div class="FieldName">
                                Name
                            </div>
                            <div>
                                <input type="text" [(ngModel)]="model.Group.Name" style="width: 95%" />
                            </div>
                        </div>
                        <div class="col s12">
                            <div class="FieldName">
                                Description
                            </div>
                            <div>
                                <p-editor [(ngModel)]="model.Group.Description" [style]="{'width' : '95%' }" [styleClass]="{'width' : '95%' }">
                                </p-editor>
                            </div>
                        </div>
                        <div *ngIf="model.Children != null && model.Children.length > 0" class="col s6">
                            <div class="FieldName">
                                Child Weights
                            </div>
                            <div class="directions">
                                The total weight of the children must add up to 1.0. 
                            </div>
                            <table>
                                <thead style="border-bottom: none;">
                                    <tr>
                                        <th style="padding:5px">Name</th>
                                        <th style="padding:5px">Weight</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr *ngFor="let child of model.Children">
                                        <td style="padding:5px">{{child.Name}}</td>
                                        <td style="padding:5px">
                                            <input type="number" [ngModel]="child.Weight" (ngModelChange)="child.Weight = $event; sumWeights();" style="width: 64px" />
                                        </td>
                                    </tr>
                                </tbody>
                                <tfoot style="border-top: 1px solid #666">
                                    <tr>
                                        <td style="font-weight: bold">Total</td>
                                        <td style="font-weight: bold">&nbsp;&nbsp;&nbsp;{{childrenWeight}}</td>
                                    </tr>
                                </tfoot>
                            </table>
                        </div>
                        <div class="col s12" style="padding-top: 15px">
                            <button pButton type="button" label="Save" [disabled]="!valid()" (click)="save()"></button>
                            <button pButton type="button" label="Cancel" (click)="cancel()"></button>
                        </div>
                    </div>
                </div>
                `,
providers: [MetricsService, MessagesService]
})

export class AdminMetricGroupEditorComponent extends BaseComponent {
    @Input() groupId: number = -1;
    @Input() parentId: number = -1;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    verb = "Add";

    model: GroupForm = null;
    childrenWeight = 0;

    constructor(private metricsService: MetricsService, protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        //console.log(this.groupId);
        this.load();
    }

    load() {
        if (this.groupId > 0) {
            this.verb = "Edit"
            this.isLoading = true;
            this.metricsService.getGroupFormModel(this.groupId)
                .then(r => {
                    this.model = r;
                    this.sumWeights();
                    this.isLoading = false;
                    console.log(this.model);
                });
        } else {
            this.verb = "Add";
            this.model = new GroupForm();
            this.model.Group = new Group();
            if (this.parentId > 0)
                this.model.Group.ParentID = this.parentId;
            else
                this.model.Group.Weight = 1;;
            this.model.Children = [];
            this.sumWeights();
        }
    }

    sumWeights() {
        this.childrenWeight = 0;

        if (this.model == null || this.model.Children == null || this.model.Children.length < 1)
            return;

        this.model.Children.forEach(c => {
            this.childrenWeight += c.Weight;
        });
    }

    valid() {
        let valid = true;

        if (this.model == null || this.model.Group == null) {
            valid = false;
        } else {
            if (this.model.Group.Name == null || this.model.Group.Name.length < 1)
                valid = false;

            if (this.model.Children != null && this.model.Children.length > 1) {
                let total = 0;
                this.model.Children.forEach(c => {

                    if (c.Weight == 0)
                        valid = false;
                    total += c.Weight;
                });

                if (total != 1)
                    valid = false;
            }
        }

        return valid;
    }

    save() {
        this.isLoading = true;
        this.metricsService.saveGroup(this.model)
            .then(r => {
                this.showMessageForResult(this.messagesService, r);
                this.isLoading = false;
                this.onSave.emit();
            });
    }

    cancel() {
        this.onCancel.emit();
    }
};