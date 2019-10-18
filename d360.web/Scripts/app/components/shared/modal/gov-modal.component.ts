import { Component, Input, Output, HostListener, EventEmitter, OnChanges, SimpleChanges, ViewChild, ElementRef } from '@angular/core';
import { ModalService } from '../../../services/modal-dialog-service';


@Component({
    selector: 'd3s-modal',
    templateUrl: 'gov-modal.component.html'
})

export class D3SModal implements OnChanges {
    @Input() title: string = 'Default Title';
    @Input() additionalClasses: string = '';
    @Input() isVisible: false;
    @Input() showConfirm: false;

    @Output() onClose = new EventEmitter();
    @Output() onConfirm = new EventEmitter();

    @ViewChild('popupBox', { static: false }) modalDiv: ElementRef; 

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
        let path: any[] = event.path;
        //add scroll exceptions here
        if (this.display == true && !(path.filter(x => x.tagName == 'D3S-TAG-USAGE').length > 0)) {
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

    confirm() {
        this.onConfirm.emit('confirm');
        this.closePopUp();
    }

}

