import {
    AfterContentInit,
    Component,
    ElementRef,
    EventEmitter,
    HostListener,
    Input,
    OnChanges,
    OnDestroy,
    Output,
    SimpleChanges,
    ViewChild,
    ViewEncapsulation
} from '@angular/core';


@Component({
    selector: 'd3s-modal',
	templateUrl: 'gov-modal.component.html',
	styleUrls: ['gov-modal.component.less'],
	encapsulation: ViewEncapsulation.None
})

export class D3SModal implements OnChanges, AfterContentInit, OnDestroy {
    @Input() title: string = $localize`Default Title`;
    @Input() additionalClasses: string = '';
    @Input() isVisible: boolean = false;
    @Input() showConfirm: boolean = false;
    @Input() showTitle: boolean = true;
    @Input() includePreciselyLogo: boolean = false;
    @Input() subtitle: string;

    @Input() formFeedbackPortalName: string;

    @Input() modalSidePanelAssetUID: string = 'TETET';

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
        if (changes.isVisible !== undefined && (changes.isVisible.previousValue !== changes.isVisible.currentValue)) {
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
		const path: any[] = event.composedPath();
		//add scroll exceptions here
        if (this.display === true
            && !(path.filter((x) => x.tagName === 'D3S-TAG-USAGE').length > 0)
            && !(path.filter((x) => x.tagName === 'D3S-ASSET-TYPE-MODAL-EDITOR').length > 0)
            && !(path.filter((x) => x.tagName === 'P-DROPDOWNITEM').length > 0)
			&& !(path.filter((x) => x.tagName === 'IG-PROPERTY-GROUP').length > 0)
			&& !(path.filter((x) => x.tagName === 'TABLE').length > 0) 
		){
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

    public closePopUp() {
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

