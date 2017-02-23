import { Component, Input, OnInit, OnChanges, Output, EventEmitter } from '@angular/core';
import { DiagramService } from '../../../services/diagram.service';
import { MessagesService } from '../../../services/messages.service';
import { PermissionsService } from '../../../services/permissions.service';
import {
    MapSequenceItem,
    MapSequenceModel,
    MapContext,
    MapReferenceItem,
    SourceRuleItem,
    SourceRuleSequence,
    SourceRuleSource,
} from '../../../models/lineage.model';
import { BaseComponent } from '../base.component';
import { Permission } from '../../../models/permission.model';

@Component({
    selector: 'd3s-lineage-source-rule-editor',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <header>
                Manage Source Rules 
                <d3s-tile-actions hasSave="true" hasClose="true" (saveClick)="save()" (closeClick)="close()"></d3s-tile-actions>
            </header>
            <div class="row" *ngFor="let item of items">
                <div style="margin-top: 25px" class="col s12">
                    <h4>{{item.Name}}</h4>
                </div>
                <div class="col s3">
                    <table class="responsive-table striped">
                        <thead>
                            <tr>
                                <th>1. Available Sources</th>
                                <th></th>
                            </tr>
                        </thead>
                        <tbody *ngFor="let a of item.Available">
                            <tr>
                                <td>{{a.Name}}</td>
                                <td style="width: 25px">
                                    <i class="fa fa-lg fa-plus blue-text" (click)="add(item, a)"></i>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
                <div class="col s9">
                    <table class="responsive-table striped">
                        <thead>
                            <tr style="vertical-align: top">
                                <th style="width: 30%">2. Referenced Sources</th>
                                <th style="width: 60px; padding-right: 5px">Sequence</th>
                                <th>Translation</th>
                                <th style="width: 30px"></th>
                            </tr>
                        </thead>
                        <tbody *ngFor="let s of item.Selected; let i = index">
                            <tr *ngIf="(s.IsDeleting || false) == false">
                                <td style="vertical-align: top">{{s.SourceName}}</td>
                                <td style="vertical-align: top">{{s.Sequence}}</td>
                                <td style="vertical-align: top; background-color: #fff;">
                                    <p-editor [(ngModel)]="s.Description"></p-editor>
                                </td>
                                <td style="vertical-align: top; text-align: center; width: 25px">
                                    <a style="cursor: pointer"><i class="fa fa-lg fa-trash red-text" (click)="remove(s)"></i></a>
                                    <a style="cursor: pointer"><i class="fa fa-lg fa-arrow-up black-text" (click)="moveUp(item, s)"></i></a>
                                    <a style="cursor: pointer"><i class="fa fa-lg fa-arrow-down black-text" (click)="moveDown(item, s)"></i></a>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    `,
    providers: [DiagramService]
})

export class LineageSourceRuleEditorComponent extends BaseComponent implements OnInit {
    @Input() object: string;
    @Input() objectId: number;

    @Output() onClose = new EventEmitter();
    @Output() onSaveComplete = new EventEmitter();

    model: MapSequenceModel;
    items: SourceRuleItem[] = [];
    permissions: Permission[] = [];

    isLoading = false;

    constructor(private diagramService: DiagramService, protected messagesService: MessagesService, protected permissionsService: PermissionsService) {
        super();
    }

    ngOnInit() {
        this.load();
        this.permissionsService.getPermissions(this.objectId, this.object)
            .then(data => {
                this.permissions = data;
            });
    }

    load() {
        this.isLoading = true;
        this.diagramService.getLineageMapSequence(this.object, this.objectId)
            .then(data => {
                this.model = data;

                this.model.Available.forEach(i => {
                    let top = this.items.find(t => t.TargetIntersectID == i.TargetIntersectID);
                    let topItem: any;

                    if (!top) {
                        topItem = {
                            ID: i.ID,
                            Name: i.Target,
                            TargetIntersectID: i.TargetIntersectID,
                            Available: [],
                            Selected: []
                        };

                        this.model.Available.forEach(s => {
                            if (s.TargetIntersectID == topItem.TargetIntersectID) {
                                topItem.Available.push({
                                    MapItemID: s.ID,
                                    SourceIntersectID: s.SourceIntersectID,
                                    Name: s.Source
                                });
                            }
                        });

                        this.model.Referenced.forEach(r => {
                            if (r.TargetIntersectID == topItem.TargetIntersectID) {
                                let sourceName = "";

                                topItem.Available.forEach(a => {
                                    if (r.MapItemID == a.MapItemID) {
                                        sourceName = a.Name;
                                    }
                                });

                                let selectedItem = {
                                    ID: r.ID,
                                    MapItemID: r.MapItemID,
                                    Sequence: r.Sequence,
                                    Contexts: [],
                                    Description: r.Description,
                                    SourceName: sourceName
                                };

                                //Add to the Selected collection.
                                topItem.Selected.push(selectedItem);
                            }
                        });
                        this.items.push(topItem);
                    }
                });

                //console.log(this.topItems);
                this.isLoading = false;
            });
    }

    private add(parent: any, item: any) {
        let newItem = {
            ID: 0,
            MapItemID: item.MapItemID,
            Sequence: parent.Selected.length + 1,
            Contexts: null,
            Description: '',
            SourceName: item.Name || '',
            TargetName: parent.Name || '',
        };
        this.setSequenceNumbers();
        parent.Selected.push(newItem);
    }

    private remove(seq: SourceRuleSequence) {
        seq.IsDeleting = true;
        this.setSequenceNumbers();
    }

    close() {
        this.onClose.emit();
    }

    save() {
       
        let permCreate = this.permissions.find(p => p.ClaimObject == 'Relationship' && p.Claim == 'Create');
        let permEdit = this.permissions.find(p => p.ClaimObject == 'Relationship' && p.Claim == 'Update');

        if (!permEdit || !permCreate)
            return;

        this.isLoading = true;

        this.setSequenceNumbers();
        let model = { Items: [] };

        if (this.items == null)
            this.items = [];
        this.items.forEach(i => {
            i.Selected.forEach(s => {
                model.Items.push(s);
            });
        });

        //console.log(model);

        this.diagramService.postLineageMapSequence(this.object, this.objectId, model)
            .then(r => {
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, r);
                this.onSaveComplete.emit();
            });
    }

    private setSequenceNumbers() {
        this.items.forEach(i => {
            if (i.Selected != null) {
                let index = 1;
                for (let s = 0; s < i.Selected.length; s++) {
                    if (i.Selected[s].IsDeleting)
                        continue;
                    i.Selected[s].Sequence = index++;
                }
            }
        });
    }

    moveUp(item: SourceRuleItem, seq: SourceRuleSequence) {
        this.setSequenceNumbers();
        let previousIndex = item.Selected.findIndex(i => i == seq) - 1;
        //console.log(item, seq, previousIndex);

        if (previousIndex < 0)
            return;

        let previous = item.Selected[previousIndex];
        seq.Sequence--;
        previous.Sequence++;

        item.Selected[previousIndex] = seq;
        item.Selected[previousIndex + 1] = previous;
    }

    moveDown(item: SourceRuleItem, seq: SourceRuleSequence) {
        this.setSequenceNumbers();
        let nextIndex = item.Selected.findIndex(i => i == seq) + 1;
        //console.log(item, seq, nextIndex);

        if (nextIndex >= item.Selected.length)
            return;

        let next = item.Selected[nextIndex];
        seq.Sequence++;
        next.Sequence--;

        item.Selected[nextIndex] = seq;
        item.Selected[nextIndex - 1] = next;
    }

}