import { Component, Input, Output, HostListener, EventEmitter, OnChanges, SimpleChanges, ViewChild, ElementRef } from '@angular/core';


@Component({
    selector: 'd3s-modal',
    templateUrl: 'gov-modal.component.html'
})

export class D3SModal implements OnChanges {
    @Input() title: string = 'Default Title';
    @Input() additionalClasses: string = '';
    @Input() isVisible: false;

    @Output() onClose = new EventEmitter();

    @ViewChild('popupBox') modalDiv: ElementRef; 

    private display: boolean = false;

    ngOnChanges(changes: SimpleChanges) {
        if (changes.isVisible.previousValue != changes.isVisible.currentValue) {
            if (changes.isVisible.currentValue) {
                this.showPopUp();
            }
            else {
                this.closePopUp();
            }
        }
    }


    checkKey(event) {
        if (event.keyCode) {
            if (event.keyCode == 27)
                this.closePopUp();
        }
    }


    @HostListener('wheel', ['$event'])
    handleWheelEvent(event) {
        if (this.display == true) {
            event.preventDefault();
        }
    }

    showPopUp() {
        this.display = true;
        this.modalDiv.nativeElement.className = "modal-overlay";
        this.modalDiv.nativeElement.className = this.modalDiv.nativeElement.className + " show";
        this.modalDiv.nativeElement.focus();
    }

    closePopUp() {
        this.modalDiv.nativeElement.className = this.modalDiv.nativeElement.className + " begin-hide";
        window.setTimeout(function () {
            this.modalDiv.nativeElement.className = "modal-overlay";
            this.onClose.emit(null);
        }.bind(this), 250);
        this.display = false;

    }



}

