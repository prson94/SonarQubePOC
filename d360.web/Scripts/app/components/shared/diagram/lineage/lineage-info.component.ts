import { Component, Input, OnInit, OnChanges, ViewChild, ViewContainerRef, ComponentFactoryResolver, ComponentFactory, ComponentRef, Output, EventEmitter } from '@angular/core';
import { LineageService } from '../../../../services/lineage.service';
import { DynamicTypeBuilder, IHaveDynamicData } from '../../../../services/dynamic-type-builder';
import { NodeModelV2 } from '../../../../models/lineage.model';
import * as go from 'gojs';

@Component({
    selector: 'd3s-lineage-info',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="node != null && node.errors != null && node.errors.length != null && node.errors.length > 0" class="row">
            <div *ngFor="let e of node.errors" class="errorMessage col s12" style="padding-bottom: 10px">
                &bull; {{e}}
            </div>
        </div>
        <div *ngIf="node != null && node.category == 'transform'">
            <div class="row" *ngIf="node.businessTransformation != null">
                <div class="col s12">
                    <div class="FieldName">
                        Business Transformation
                    </div>
                    <div>
                        {{node.businessTransformation}}
                    </div>
                </div>
            </div>
            <div class="row" *ngIf="node.technicalTransformation != null">
                <div class="col s12">
                    <div class="FieldName">
                        Technical Transformation
                    </div>
                    <div>
                        {{node.technicalTransformation}}
                    </div>
                </div>
            </div>
            <div class="row" *ngIf="node.technicalTransformation == null && node.businessTransformation == null">
                <div class="col s12">
                    <div>
                        No transformations defined
                    </div>
                </div>
            </div>
        </div>
        <div #target [hidden]="isLoading && (node != null && node.category != 'map')"></div>
        <div *ngIf="!isLoading && node != null && node.category == 'map'">
            <div class="row">
                <div class="col s12">
                    <table style="width: 100%">
                        <tbody>
                            <tr>
                                <td>
                                    <div style="width: 100%; height: 30px; white-space: nowrap; overflow: hidden" *ngFor="let p of previous">
                                        <span style="display: block; background-color: #eee; color: black; border: 2px solid gray; padding: 3px; border-radius: 3px;"><a (click)="select(p)" style="cursor: pointer; color: black">{{p.name}}</a></span>
                                    </div>
                                </td>
                                <td style="vertical-align: middle; font-size: 1.5em; max-width: 32px">
                                    <ng-container *ngIf="previous.length > 0">&#x2192;</ng-container>
                                </td>
                                <td style="text-align: center">
                                    <span style="background-color: #eee; color: black; border: 2px solid #1E90FF; padding: 3px; border-radius: 3px;">{{node.name}}</span>
                                </td>
                                <td style="vertical-align: middle; font-size: 1.5em; max-width: 32px">
                                    <ng-container *ngIf="next.length > 0">&#x2192;</ng-container>
                                </td>
                                <td>
                                    <div style="width: 100%; height: 30px; white-space: nowrap; overflow: hidden" *ngFor="let n of next">
                                        <span style="display: block; background-color: #eee; color: black; border: 2px solid gray; padding: 3px; border-radius: 3px;"><a (click)="select(n)" style="cursor: pointer; color: black">{{n.name}}</a></span>
                                    </div>
                                </td>
                            </tr>
                            <tr>
                                <td colspan="2"></td>
                                <td style="text-align: center">
                                    <div *ngFor="let c of children" style="padding:3px">
                                        <span style="padding: 3px; border-radius: 3px;" [style.background-color]="c.backColor">
                                            <a (click)="select(c, true)" style="cursor: pointer" [style.color]="c.foreColor">{{c.name}}</a>
                                        </span>
                                    </div>
                                </td>
                                <td colspan="2"></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    `,
    styles: [
        `
        
`
    ],
    providers: [LineageService]
})

export class LineageInfoComponent implements OnInit, OnChanges {
    @ViewChild('target', { read: ViewContainerRef }) protected dynamicComponentTarget: ViewContainerRef;
    protected componentRef: ComponentRef<IHaveDynamicData>;
    @Input() node: NodeModelV2;
    @Input() diagram: go.Diagram;
    @Output() selectionChange = new EventEmitter();
    data: any = null;
    isLoading = false;
    children = [];
    previous = [];
    next = [];

    constructor(private lineageService: LineageService, protected typeBuilder: DynamicTypeBuilder, public componentFactoryResolver: ComponentFactoryResolver) { }

    ngOnChanges() {
        //console.log(this.node);
        this.data = null;
        if (this.componentRef) {
            this.componentRef.destroy();
        }
        this.children = [];
        this.previous = [];
        this.next = [];
        this.load();
    }

    ngOnInit() { }

    load() {
        if (this.node == null || this.node.objectId == null || this.node.object == 'MapGroup')
            return;

        if (this.node.category == 'map') {
            this.children = this.diagram.model.nodeDataArray.filter(n => (<any>n).group == this.node.key);

            //find the next and previous maps
            let links = (<go.GraphLinksModel>this.diagram.model).linkDataArray;

            links.forEach(l => {
                if ((<any>l).to == this.node.key) {
                    let prev = this.diagram.model.findNodeDataForKey((<any>l).from);
                    if (prev && this.previous.filter(p => p.key == prev.key).length < 1)
                        this.previous.push(prev);
                }

                if ((<any>l).from == this.node.key) {
                    let nxt = this.diagram.model.findNodeDataForKey((<any>l).to);
                    if (nxt && this.next.filter(n => n.key == nxt.key).length < 1)
                        this.next.push(nxt);
                }
            });

        } else {
            this.isLoading = true;
            this.lineageService.getLineageObjectDetail(this.node.object, this.node.objectId)
                .then(data => {
                    //console.log(data);
                    this.data = data._body;
                    this.isLoading = false;
                }).then(() => {
                    //TODO: don't generate html from server to avoid having to do this

                    if (this.componentRef) {
                        this.componentRef.destroy();
                    }

                    // here we get Factory (just compiled or from cache)
                    this.typeBuilder
                        .createComponentFactory(this.data)
                        .then((factory: ComponentFactory<IHaveDynamicData>) => {

                            // Target will instantiate and inject component (we'll keep reference to it)                                        
                            this.componentRef = this
                                .dynamicComponentTarget
                                .createComponent(factory);
                        });

                });
        }



    }

    select(c: any, expand = false) {
        let part = this.diagram.findPartForData(c);
        if (part != null) {
            this.diagram.clearSelection();
            part.isSelected = true;
            if (expand && part.containingGroup != null)
                part.containingGroup.isSubGraphExpanded = true;
            this.selectionChange.emit();
        }
    }
}