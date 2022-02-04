import { Component, NgModule, Input, ChangeDetectorRef, ChangeDetectionStrategy, OnInit, ElementRef, ViewChild, AfterViewInit, OnChanges, SimpleChanges, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormGroup, FormControl } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';
import * as _ from 'lodash';

@Component({
    selector: 'ig-property-group',
    templateUrl: './property-group.component.html',
    styleUrls: ['./property-group.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PropertyGroupComponent implements OnInit, AfterViewInit {
    @Input() igformGroup: FormGroup;
    @Input() title: string = "Property Group";
    @Input() showMoreInfo: boolean = false;
    @Input() moreInfoHtml: string = "";
    @Input() shouldBePadded: boolean = true;
    @Input() showHeaderLine: boolean = true;
    @Input() hideIfNoTitle: boolean = false;

    @Output() isValid = new EventEmitter();
    invalidCount: number = 0;
    requiredCount: number = 0;
    @Input() expanded: boolean = true;
    @Output() expandedChange = new EventEmitter();

    private requiredPos: number = 0;
    private invalidPos: number = 0;

    delayedRefresh = _.debounce(() => {
        this.requiredCount = this.getRequiredCount();
        this.invalidCount = this.getInvalidCount();
        this.isValid.emit(this.requiredCount === 0 && this.invalidCount === 0);
        this.ref.markForCheck();
    }, 200);

    @ViewChild("pgcontainer", { static: false }) inputContainer: ElementRef;
    constructor(private ref: ChangeDetectorRef) {

    }

    ngAfterViewInit(): void {
        if (this.igformGroup) {
            this.igformGroup.valueChanges.subscribe(x => {
                this.delayedRefresh();
            });
        }
    }

    ngOnInit(): void {
        if (this.igformGroup) {
            this.igformGroup.valueChanges.subscribe(x => {
                this.delayedRefresh();
            });
        }
    }

    public refreshBadgeCounts() {
        this.delayedRefresh();
    }

    getRequiredCount(): number {
        let reqCount = 0;
        if (this.igformGroup) {
            Object.keys(this.igformGroup.controls).forEach(x => {
                let control = <FormControl>this.igformGroup.get(x);
                let elem = this.getFormControlDomElement(x);

                if (elem && control && control.errors && control.errors["required"] == true) {
                    reqCount++;
                }
            });
        }
        
        return reqCount;
    }

    getInvalidCount(): number {
        let invCount = 0;
        if (this.igformGroup) {
            Object.keys(this.igformGroup.controls).forEach(x => {
                let control = <FormControl>this.igformGroup.get(x);
                let elem = this.getFormControlDomElement(x);
                if (elem && control && control.errors) {
                    invCount += Object.keys(control.errors).filter(x => x != "required").length > 0 ? 1 : 0;
                }
            });
        }
        return invCount;
    }

    focusInvalid(event) {
        event.stopPropagation();
        let found = false;
        if (!this.expanded)
            this.expanded = true;

        let fcCount = this.getFormControlCount("errors");
        let idx = 0;
        Object.keys(this.igformGroup.controls).forEach(x => {
            let control = <FormControl>this.igformGroup.get(x);
            if (control && control.errors && !found) {
                let invFound = Object.keys(control.errors).filter(x => x != "required").length > 0;
                if (invFound) {
                    let elem = this.getFormControlDomElement(x);

                    if (elem) {
                        idx++;
                        if ((idx > this.invalidPos)) {
                            this.invalidPos++;
                            if (this.invalidPos >= fcCount) {
                                this.invalidPos = 0;
                            }
                            if (elem.tagName === 'IG-DATE' || elem.tagName === 'IG-NUMBER-INPUT') {
                                elem.querySelector('input').click();
                            }
                            elem.focus();
                            found = true;
                        }
                    }
                }
            }
        });
    }

    focusRequired(event) {
        event.stopPropagation();
        let found = false;
        if (!this.expanded)
            this.expanded = true
        let fcCount = this.getFormControlCount("required");
        let idx = 0;
        Object.keys(this.igformGroup.controls).forEach((x) => {
            let control = <FormControl>this.igformGroup.get(x);
            if (control && control.errors && control.errors["required"] == true && !found) {
                let elem = <HTMLElement>this.getFormControlDomElement(x);
                if (elem) {
                    idx++;
                    if ((idx > this.requiredPos)) {
                        this.requiredPos++;
                        if (this.requiredPos >= fcCount) {
                            this.requiredPos = 0;
                        }
                        if (elem.tagName === 'IG-DATE' || elem.tagName === 'IG-NUMBER-INPUT') {
                            elem.querySelector('input').click();
                        }
                        else if (elem.tagName === 'P-DROPDOWN') {
                            (elem.querySelectorAll('.p-dropdown-trigger')[0] as HTMLElement).click();
                        }
                        elem.focus();
                        found = true;
                    }
                }
            }
        });
    }

    getFormControlDomElement(controlName: string) {
        if (this.inputContainer) {
            return this.inputContainer.nativeElement.querySelectorAll("[formControlName=" + controlName + "], [name=" + controlName + "], [id=" + controlName + "]").length > 0 ?
                this.inputContainer.nativeElement.querySelectorAll("[formControlName=" + controlName + "], [name=" + controlName + "], [id=" + controlName + "]")[0] : null;
        }
    }

    getFormControlCount(type: string): number {
        let count = 0;
        Object.keys(this.igformGroup.controls).forEach(x => {
            let control = <FormControl>this.igformGroup.get(x);
            if (control) {
                if (type == "required") {
                    if (control.errors && control.errors["required"] == true) {
                        let elem = <HTMLElement>this.getFormControlDomElement(x);
                        if (elem) {
                            count++;
                        }
                    }
                }
                if (type == "errors") {
                    if (control.errors) {
                        if (Object.keys(control.errors).filter(x => x != "required").length > 0) {
                            let elem = <HTMLElement>this.getFormControlDomElement(x);
                            if (elem) {
                                count++;
                            }
                        }
                    }
                }

            }
        });
        return count;
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
    ]
})
export class PropertyGroupModule { }