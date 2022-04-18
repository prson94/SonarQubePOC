import { Component, ChangeDetectionStrategy, Input, SimpleChanges } from "@angular/core";

@Component({
    selector: "ig-regexp-tester",
    templateUrl: "regexp-tester.component.html",
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["./regexp-tester.component.less"]
})
export class RegexpTesterComponent {
    @Input() regexp: string;
    @Input() isValidRegexp: boolean;
    @Input() disabled: boolean;

    expressionTestString = "";

    isValid: boolean = null;

    ngOnChanges(changes: SimpleChanges) {
        if ('regexp' in changes) {
            this.isValid = null;
        }
    }

    get isEmptyRegexp() {
        if (this.regexp == null || this.regexp.trim() === "") {
            return true;
        }

        return false;
    }

    get isEmptyTestString() {
        if (this.expressionTestString == null || this.expressionTestString.trim() === "") {
            return true;
        }

        return false;
    }
    
    writeExpressionTestString(str: string) {
        this.expressionTestString = str;
        this.isValid = null;
    }

    validate() {
        const regexp = new RegExp(this.regexp);
        this.isValid = regexp.test(this.expressionTestString);
    }
}
