import { Component, Input, Output, HostListener, EventEmitter, OnChanges, SimpleChanges, ViewChild, ElementRef, AfterContentInit, OnDestroy, AfterViewChecked } from '@angular/core';


@Component({
    selector: 'd3s-modal-drawer',
    templateUrl: 'gov-modal-drawer.component.html',
    styleUrls: ['gov-modal-drawer.component.less']
})

export class D3SModalDrawer implements OnChanges, AfterContentInit, OnDestroy, AfterViewChecked {
    @Input() title: string = 'Default Title';
    @Input() additionalClasses: string = '';
    @Input() isVisible: false;

    @Input() appendToBody: boolean = false;

    @Output() onClose = new EventEmitter();

    @ViewChild('modalDrawerBox', { static: false }) modalDrawerDiv: ElementRef;
    @ViewChild('modalDrawerBody', { static: false }) modalDrawerBody: ElementRef;
    @ViewChild('modalDrawerContent', { static: false }) modalDrawerContent: ElementRef;
    @ViewChild('modalWrapper', { static: false }) modalWrapper: ElementRef;

    display: boolean = false;

    ngAfterContentInit() {
        if (this.appendToBody) {
            setTimeout(() => {
                document.body.append(this.modalWrapper.nativeElement);
            });
        }
    }


    ngOnChanges(changes: SimpleChanges) {
        if (changes.isVisible && (changes.isVisible.previousValue !== changes.isVisible.currentValue)) {
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
            this.modalWrapper.nativeElement.remove();
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

    showPopUp() {
        this.display = true;
        window.setTimeout(function () {
            if (this.modalDrawerBody) {
                this.modalDrawerBody.nativeElement.className = this.modalDrawerBody.nativeElement.className + " full-width";
                this.modalDrawerDiv.nativeElement.focus();
            }
        }.bind(this), 10);

    }

    closePopUp() {
        if (this.modalDrawerBody) {
            this.modalDrawerBody.nativeElement.className = this.modalDrawerBody.nativeElement.className.replace("full-width", "");
            window.setTimeout(function () {
                this.modalDrawerBody.nativeElement.className = "modal-drawer-body";
                this.onClose.emit(null);
                this.display = false;

            }.bind(this), 250);

        }

    }

    @HostListener('wheel', ['$event'])
    handleWheelEvent(event) {
        let path: any[] = event.path;
        //add scroll exceptions here
        if (this.display === true
            && !(path.filter(x => x.tagName === 'D3S-TAG-USAGE').length > 0)
            && !(path.filter(x => x.tagName === 'D3S-ASSET-TYPE-MODAL-EDITOR').length > 0)
            && !(path.filter(x => x.tagName === 'P-DROPDOWNITEM').length > 0)
            && !(path.filter((x) => x.tagName === 'IG-PROPERTY-GROUP').length > 0)
        ) {
            event.preventDefault();
        }
    }

    @HostListener('window:resize', ['$event'])
    onResize(event) {
        this.setContentMaxHeight();
    }

    ngAfterViewChecked() {
        this.setContentMaxHeight();
    }

    private setContentMaxHeight() {
        if (this.modalDrawerContent) {
            var htmlElement = this.modalDrawerContent.nativeElement as HTMLElement;
            var maxHeight = (window.innerHeight - 160) + 'px';
            if (maxHeight !== htmlElement.style.maxHeight) {
                htmlElement.style.maxHeight = maxHeight;
            }
        }
    }
}

