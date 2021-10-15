import { Component, Input, Output, HostListener, EventEmitter, OnChanges, SimpleChanges, ViewChild, ElementRef, AfterContentInit, OnDestroy } from '@angular/core';


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
    @Input() includePreciselyLogo: boolean = false;
    @Input() subtitle: string;

    @Input() appendToBody: boolean = false;

    @Output() onClose = new EventEmitter();
    @Output() onConfirm = new EventEmitter();

    @ViewChild('popupBox', { static: false }) modalDiv: ElementRef;

    private display: boolean = false;

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


    checkKey(event: KeyboardEvent) {
        if (event.keyCode) {
            if (event.keyCode === 27) {
                if (!event.defaultPrevented) {
                    this.closePopUp();
                }
            }
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
        }

    }

    confirm() {
        this.onConfirm.emit('confirm');
        this.closePopUp();
    }
}

