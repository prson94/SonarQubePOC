///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Artifact } from '../../models/artifacts.model';


@Component({
    selector: 'd3s-artifact-definition',
    template: `<header *ngIf="showHeader">Definition</header>                
                <div class="row" [innerHtml]="artifact.Description">                    
                </div>        
                `
})

export class ArtifactDefnintionComponent implements OnInit, OnDestroy {
    @Input() artifact: Artifact
    @Input() showHeader: boolean = true;

    constructor() {
        
    }

    ngOnInit() {
        
    }

    ngOnDestroy() {
        
    }


};