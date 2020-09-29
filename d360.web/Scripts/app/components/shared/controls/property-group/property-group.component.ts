
import { Component, NgModule, Input, ChangeDetectorRef, ChangeDetectionStrategy, OnInit, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormGroup, FormControl } from '@angular/forms';
import { Tooltip, TooltipModule } from 'primeng/tooltip';

@Component({
    selector: 'ig-property-group',
    templateUrl: './property-group.component.html',
    styleUrls: ['./property-group.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PropertyGroupComponent implements OnInit, AfterViewInit {
    @Input() igformGroup: FormGroup;
    @Input() title: string = "Property Group";

    invalidCount: number = 0;
    requiredCount: number = 0;
    expanded: boolean = true;

    @ViewChild("pgcontainer", { static: false }) inputContainer: ElementRef;
    constructor(private ref: ChangeDetectorRef) {

    }
    ngAfterViewInit(): void {
        this.requiredCount = this.getRequiredCount();
        this.invalidCount = this.getInvalidCount();
        this.ref.markForCheck();
    }

    ngOnInit(): void {
        this.igformGroup.valueChanges.subscribe(x => {
            this.requiredCount = this.getRequiredCount();
            this.invalidCount = this.getInvalidCount();
            this.ref.markForCheck();
        });
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

        Object.keys(this.igformGroup.controls).forEach(x => {
            let control = <FormControl>this.igformGroup.get(x);
            if (control && control.errors && !found) {
                let invFound = Object.keys(control.errors).filter(x => x != "required").length > 0;
                if (invFound) {
                    let elem = this.getFormControlDomElement(x);

                    if (elem) {
                        elem.focus();
                        found = true;
                    }
                }
            }
        });
    }

    focusRequired(event) {
        event.stopPropagation();
        let found = false;
        if (!this.expanded)
            this.expanded = true;
        Object.keys(this.igformGroup.controls).forEach(x => {
            let control = <FormControl>this.igformGroup.get(x);
            if (control && control.errors && control.errors["required"] == true && !found) {
                let elem = <HTMLElement>this.getFormControlDomElement(x);
                if (elem) {
                    elem.focus();
                    found = true;
                }
            }
        });
    }

    getFormControlDomElement(controlName:string) {
        if (this.inputContainer) {
            return this.inputContainer.nativeElement.querySelectorAll("[formControlName=" + controlName + "], [name=" + controlName + "]").length > 0 ? 
                this.inputContainer.nativeElement.querySelectorAll("[formControlName=" + controlName + "], [name=" + controlName + "]")[0] : null;
        }
    }

    onInputKeyUp(event) {
        console.log(event);
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