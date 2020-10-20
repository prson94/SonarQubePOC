
import { Component, NgModule, Input, ChangeDetectorRef, ChangeDetectionStrategy, OnInit, ElementRef, ViewChild, AfterViewInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormGroup, FormControl } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';

@Component({
    selector: 'ig-property-group',
    templateUrl: './property-group.component.html',
    styleUrls: ['./property-group.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PropertyGroupComponent implements OnInit, AfterViewInit, OnChanges {
    @Input() igformGroup: FormGroup;
    @Input() title: string = "Property Group";

    invalidCount: number = 0;
    requiredCount: number = 0;
    expanded: boolean = true;

    private requiredPos: number = 0;
    private invalidPos: number = 0;

    @ViewChild("pgcontainer", { static: false }) inputContainer: ElementRef;
    constructor(private ref: ChangeDetectorRef) {

    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes && this.igformGroup && changes.igformGroup.currentValue != changes.igformGroup.previousValue) {
            this.igformGroup.valueChanges.subscribe(x => {
                this.requiredCount = this.getRequiredCount();
                this.invalidCount = this.getInvalidCount();
                this.ref.markForCheck();
            });
        }
    }

    ngAfterViewInit(): void {
        this.requiredCount = this.getRequiredCount();
        this.invalidCount = this.getInvalidCount();
        this.ref.markForCheck();
    }

    ngOnInit(): void {
        if (this.igformGroup) {
            this.igformGroup.valueChanges.subscribe(x => {
                this.requiredCount = this.getRequiredCount();
                this.invalidCount = this.getInvalidCount();
                this.ref.markForCheck();
            });
        }
    }

    getRequiredCount(): number {
        let reqCount = 0;
        if (this.igformGroup) {
            Object.keys(this.igformGroup.controls).forEach(x => {
                let control = <FormControl>this.igformGroup.get(x);
                let elem = this.getFormControlDomElement(x);
                if (control && control.errors && control.errors["required"] == true && elem) {
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
                if (control && control.errors && elem) {
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
                        elem.focus();
                        found = true;
                    }
                }
            }
        });
    }

    getFormControlDomElement(controlName: string) {
        if (this.inputContainer) {
            return this.inputContainer.nativeElement.querySelectorAll("[formControlName=" + controlName + "], [name=" + controlName + "]").length > 0 ?
                this.inputContainer.nativeElement.querySelectorAll("[formControlName=" + controlName + "], [name=" + controlName + "]")[0] : null;
        }
    }

    getFormControlCount(type: string): number {
        let count = 0;
        Object.keys(this.igformGroup.controls).forEach(x => {
            let control = <FormControl>this.igformGroup.get(x);
            if (control) {
                if (type == "required") {
                    if (control && control.errors && control.errors["required"] == true) {
                        let elem = <HTMLElement>this.getFormControlDomElement(x);
                        if (elem) {
                            count++;
                        }
                    }
                }
                if (type == "errors") {
                    if (Object.keys(control.errors).filter(x => x != "required").length > 0) {
                        let elem = <HTMLElement>this.getFormControlDomElement(x);
                        if (elem) {
                            count++;
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