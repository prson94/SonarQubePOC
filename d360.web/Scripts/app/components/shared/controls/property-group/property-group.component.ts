import { Component, NgModule, Input, ChangeDetectionStrategy, OnInit, ElementRef, ViewChild, AfterViewInit, Output, EventEmitter, HostBinding } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormGroup } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';
import { FormFeedbackBadgesModule } from '../form-feedback-badges/form-feedback-badges.component';
import { isFormContainerValid } from '../form-feedback-badges/form-feedback-utils';
import { uuidv4 } from '../../../../static/lang';
import * as _ from 'lodash';
import { PropertyGroupsService } from './property-groups.service';

export const PropertyGroupInstanceIdAttributeName = 'data-property-group-instance-id';

@Component({
    selector: 'ig-property-group',
    templateUrl: './property-group.component.html',
    styleUrls: ['./property-group.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PropertyGroupComponent implements OnInit, AfterViewInit {
    @HostBinding(`attr.${PropertyGroupInstanceIdAttributeName}`) instanceId: string;

    @Input() igformGroup: FormGroup;
    @Input() title: string = $localize`Property Group`;
    @Input() showMoreInfo: boolean = false;
    @Input() moreInfoHtml: string = "";
    @Input() shouldBePadded: boolean = true;
    @Input() showHeaderLine: boolean = true;
    @Input() hideIfNoTitle: boolean = false;

    @Output() isValid = new EventEmitter();
    @Input() expanded: boolean = true;
    @Output() expandedChange = new EventEmitter();

    delayedRefresh = _.debounce(() => {
        this.isValid.emit(isFormContainerValid({ formGroup: this.igformGroup, formContainer: this.inputContainer }));
    }, 200);

    @ViewChild("pgcontainer", { static: false }) inputContainer: ElementRef;

    constructor(private propertyGroups: PropertyGroupsService) {
    }

    ngAfterViewInit(): void {
        if (this.igformGroup) {
            this.igformGroup.valueChanges.subscribe(x => {
                this.delayedRefresh();
            });
        }
    }

    ngOnInit(): void {
        this.instanceId = uuidv4();
        this.propertyGroups.register(this);

        if (this.igformGroup) {
            this.igformGroup.valueChanges.subscribe(x => {
                this.delayedRefresh();
            });
        }
    }

    public forceExpand() {
        if (this.expanded) {
            return;
        }

        this.expanded = true;
        this.expandedChange.next(this.expanded);
    }

    public refreshBadgeCounts() {
        this.delayedRefresh();
    }

    onInputKeyUp(event) {
        event.preventDefault();
        event.stopPropagation();
        switch (event.which) {
            case 32:
                event.target.click();
                return false;
        }
    }

    ngOnDestroy() {
        this.propertyGroups.unregister(this);
    }
}

@NgModule({
    declarations: [
        PropertyGroupComponent
    ],
    exports: [
        PropertyGroupComponent
    ]
    , imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        TooltipModule,
        FormFeedbackBadgesModule
    ]
})
export class PropertyGroupModule { }