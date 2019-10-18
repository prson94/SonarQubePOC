import { Input, Component, Output, SimpleChange, EventEmitter, OnInit, NgModule, OnChanges, ViewChild, ElementRef } from '@angular/core';
import { SiteModalModule } from '../modal/gov-modal.module';


@Component({
    selector: 'd3s-checkbox',
    templateUrl: 'gov-checkbox.html'
})

export class D3SCheckbox  {

    @Input() label: string;
    @Input() value: boolean = false;
    
    private pendingValue: boolean;

    @Output() onchange: EventEmitter<any> = new EventEmitter();

    @Input() trueTitle: string;
    @Input() trueButtonText: string;
    @Input() trueText: string;
    @Input() falseTitle: string;
    @Input() falseButtonText: string;
    @Input() falseText: string;

    private confirmTitle: string;
    private confirmButton: string;
    private confirmText: string;

    private isModalVisible: boolean = false;
    private changeInProgress: boolean = false;
    @ViewChild("switch", {static:false}) _el: ElementRef;

    tryChangeValue(val: boolean) {
        this.changeInProgress = false;

        this._el.nativeElement.focus();

        if (val == this.value) return;
        this.pendingValue = val;
        if (this.pendingValue == true && this.trueText) {
            this.confirmTitle = this.trueTitle;
            this.confirmButton = this.trueButtonText;
            this.confirmText = this.trueText;
            this.isModalVisible = true;
        }
        else if (this.pendingValue == false && this.falseText) {
            this.confirmTitle = this.falseTitle;
            this.confirmButton = this.falseButtonText;
            this.confirmText = this.falseText;
            this.isModalVisible = true;
        }
        else {
            this.confirm();
        }

    }

    cancel() {
        this.isModalVisible = false;
    }

    confirm() {
        this.changeInProgress = true;
        this.isModalVisible = false;
        this.value = this.pendingValue;
        this.onchange.emit(this.value);

    }
}




@NgModule({
    declarations: [
        D3SCheckbox
        
    ],
    exports: [
        D3SCheckbox
    ],
    imports: [
        SiteModalModule
    ]

})

export class D3SCheckboxModule { }

