import { Component, Input, Output, HostListener, EventEmitter, OnChanges, SimpleChanges, ViewChild, ElementRef, AfterContentInit, OnDestroy } from '@angular/core';
import { PopupBackButtonService } from '../../../services/popup.service';


@Component({
    selector: 'd3s-modal',
    templateUrl: 'gov-modal.component.html'
})

export class D3SModal implements OnChanges, AfterContentInit, OnDestroy {
    @Input() title: string = 'Default Title';
    @Input() additionalClasses: string = '';
    @Input() isVisible: false;
    @Input() showConfirm: false;
    @Input() showTitle: boolean = true;
    @Input() subtitle: string;

    @Input() appendToBody: boolean = false;

    @Output() onClose = new EventEmitter();
    @Output() onConfirm = new EventEmitter();

    @ViewChild('popupBox', { static: false }) modalDiv: ElementRef;

    private display: boolean = false;
    private modalUid: string = '';

    constructor(private popupBackButtonService: PopupBackButtonService) {
        this.modalUid = this.randomUid();

        popupBackButtonService.backButtonClicked.subscribe((uuid) => {
            if (uuid === this.modalUid) {
                this.closePopUp();
            }
        });
    }

    ngAfterContentInit() {
        if (this.appendToBody) {
            setTimeout(() => {
                document.body.append(this.modalDiv.nativeElement);
            });
        }
    }


    ngOnChanges(changes: SimpleChanges) {
        if (changes.isVisible !== undefined && (changes.isVisible.previousValue != changes.isVisible.currentValue)) {
            if (changes.isVisible.currentValue) {
                this.showPopUp();
            }
            else {
                this.closePopUp();
            }
        }
    }

    ngOnDestroy() {
        if (this.appendToBody) {
            this.modalDiv.nativeElement.remove();
        }
    }

    @HostListener('wheel', ['$event'])
    handleWheelEvent(event) {
        let path: any[] = event.path;
        //add scroll exceptions here
        if (this.display == true
            && !(path.filter(x => x.tagName == 'D3S-TAG-USAGE').length > 0)
            && !(path.filter(x => x.tagName == 'D3S-ASSET-TYPE-MODAL-EDITOR').length > 0)
            && !(path.filter(x => x.tagName == 'P-DROPDOWNITEM').length > 0)
            && !(path.filter((x) => x.tagName === 'IG-PROPERTY-GROUP').length > 0)
        ) {
            event.preventDefault();
        }
    }

    showPopUp() {
        this.popupBackButtonService.addState(this.modalUid);
        this.display = true;
        if (this.modalDiv) {
            this.modalDiv.nativeElement.className = "modal-overlay";
            this.modalDiv.nativeElement.className = this.modalDiv.nativeElement.className + " show";
            this.modalDiv.nativeElement.focus();
        }
    }

    closePopUp() {
        if (this.modalDiv) {
            this.modalDiv.nativeElement.className = this.modalDiv.nativeElement.className + " begin-hide";
            window.setTimeout(function () {
                this.modalDiv.nativeElement.className = "modal-overlay";
                this.onClose.emit(null);
            }.bind(this), 250);

            this.display = false;
            this.popupBackButtonService.popState(this.modalUid);
        }

    }

    confirm() {
        this.onConfirm.emit('confirm');
        this.closePopUp();
    }

    randomUid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }
}

