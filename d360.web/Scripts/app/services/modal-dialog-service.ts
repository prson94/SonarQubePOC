import { ApplicationRef, ComponentFactoryResolver, EmbeddedViewRef, Injectable, Injector } from '@angular/core';
import { Subject, Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})

export class ModalService {

    private componentRef: any;
    private modalContainer: any;

    private actionSource = new Subject<string>();
    Action$ = this.actionSource.asObservable();

    constructor(
        private componentFactoryResolver: ComponentFactoryResolver,
        private appRef: ApplicationRef,
        private injector: Injector) { }

    private createFormModal(component: any): Element {

        this.componentRef = this.componentFactoryResolver.resolveComponentFactory(component.component).create(this.injector);

        this.componentRef.instance.modal = this;

        this.appRef.attachView(this.componentRef.hostView);

        return (this.componentRef.hostView as EmbeddedViewRef<any>).rootNodes[0] as HTMLElement;
    }

    open(component: any, title: string, showConfirm: boolean, body?: any): Observable<string> {

        const alertElement = this.createFormModal(component);

        const content = document.createElement('div');
        content.appendChild(alertElement);
        this.modalContainer = document.createElement('div');
        this.modalContainer.appendChild(alertElement);
        document.body.appendChild(this.modalContainer);

        this.componentRef.instance.title = title;
        this.componentRef.instance.showConfirm = showConfirm;
        this.componentRef.instance.showPopUp();

        this.componentRef.instance.onClose.subscribe(res => { this.close(); });
        this.componentRef.instance.onConfirm.subscribe(res => { this.actionSource.next(res); });
        return this.Action$;
    }

    close(): void {
        this.appRef.detachView(this.componentRef.hostView);
        this.modalContainer.parentNode.removeChild(this.modalContainer);
        this.componentRef.destroy();
    }


}