import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { DiagramService } from '../../../services/diagram.service';
import { SourceRule } from '../../../models/lineage.model';

@Component({
    selector: 'd3s-lineage-source-rules',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading" class="rule-list">
            <table>
                <thead>
                    <tr>
                        <th style="padding-right:5px">Order</th>
                        <th>Source</th>
                    </tr>
                </thead>
                <tbody *ngFor="let i of items">
                    <tr class="rule-item-name">
                        <td class="rule-item" rowspan="3" style="text-align:center">{{i.Sequence}}</td>
                        <td class="rule-item">{{i.SubjectTypeName}} : {{i.SubjectName}}</td>
                    </tr>
                    <tr>
                        <td><i>Contexts: </i><span [innerHtml]="i.Contexts"></span></td>
                    </tr>
                    <tr>
                        <td><i>Description: </i><span [innerHtml]="i.Description"></span></td>
                    </tr>
                </tbody>
            </table>
        </div>
    `,
    providers: [DiagramService ]
})

export class LineageSourceRulesComponent implements OnInit, OnChanges {
    @Input() source: string;
    @Input() sourceId: number;
    @Input() target: string;
    @Input() targetId: number;
    @Input() focal: string;
    @Input() focalId: number;

    items: SourceRule[] = [];
    isLoading = false;

    constructor(private diagramService: DiagramService) {

    }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() {

    }

    load() {

        if (this.source == null || this.sourceId == null || this.target == null || this.targetId == null) {
            this.items = [];
            return;
        }
        this.isLoading = true;

        if (this.focal == null || this.focalId == null) {
            this.diagramService.getLineageSourceRules(this.source, this.sourceId, this.target, this.targetId)
                .then(data => {
                    this.items = data;
                    this.isLoading = false;
                });
        } else {
            this.diagramService.getLineageSourceRulesFocal(this.focal, this.focalId, this.source, this.sourceId, this.target, this.targetId)
                .then(data => {
                    this.items = data;
                    this.isLoading = false;
                });
        }
    }
}