import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    Input,
    NgModule
} from '@angular/core';
import { TooltipModule } from 'primeng/tooltip';

@Component({
    selector: 'd3s-dynamic-field-name',
    templateUrl: './dynamic-field-name.component.html',
    styles: [`svg{    position: relative;
    top: 4px;
    margin-left: 2px;}`],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class DynamicFieldNameComponent {
    @Input() field: { Name: string, FieldDescription: string, IsPartOfKey: boolean };
    tabLinkColor: string = "";
    constructor() {
        this.tabLinkColor = getComputedStyle(document.documentElement).getPropertyValue('--tabLinkColor');
    }

    get fieldTooltip() {
        const description = this.field?.FieldDescription ?? '';
        if (description === '') {
            return undefined;
        }

        return description.replace(/<[^>]+>/gm, '');
    }
}

@NgModule({
    declarations: [DynamicFieldNameComponent],
    exports: [DynamicFieldNameComponent],
    imports: [
        CommonModule,
        TooltipModule
    ],
})
export class DynamicFieldNameModule { }

