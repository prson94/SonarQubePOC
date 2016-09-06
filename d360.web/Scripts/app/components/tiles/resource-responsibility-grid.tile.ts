///<reference path="../../es6-shim.d.ts"/>
import { Component, Input } from '@angular/core';
import { Column, Header } from 'primeng/primeng';
import { CountObject } from '../../models/resource.model';

@Component({
    selector: 'd3s-resource-responsibility-grid-tile',
    template: `
<div>
    <p-dataTable>
    
    </p-dataTable>
</div>
`,
})
export class ResourceResponsibilityGridTile {
    @Input() items: CountObject[];
}