import { Component, Input, OnInit, OnChanges, ViewChild, ViewContainerRef, ComponentFactoryResolver, ComponentFactory, ComponentRef } from '@angular/core';
import { DiagramService } from '../../../services/index';
import { DynamicTypeBuilder, IHaveDynamicData } from '../../../services/dynamic-type-builder';

@Component({
    selector: 'd3s-lineage-object-detail',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div #target [hidden]="isLoading"></div>
    `,
    providers: [DiagramService]
})

export class LineageObjectDetailComponent implements OnInit, OnChanges {
    @ViewChild('target', { read: ViewContainerRef }) protected dynamicComponentTarget: ViewContainerRef;
    protected componentRef: ComponentRef<IHaveDynamicData>;
    @Input() objectType: string;
    @Input() objectId: number;

    data: any = null;
    isLoading = false;

    constructor(private diagramService: DiagramService, protected typeBuilder: DynamicTypeBuilder, public componentFactoryResolver: ComponentFactoryResolver) { }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() { }

    load() {
        this.isLoading = true;
        this.diagramService.getLineageObjectDetail(this.objectType, this.objectId)
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