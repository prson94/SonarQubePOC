import { Component, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { AdminBaseComponent } from '../../admin/admin-base.component';
import { ConnectorLabel } from '../../../models/connectorLabel.model';
import { Router } from '@angular/router';
import { ConnectorLabelService } from '../../../services/connectorLabel.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Title } from '@angular/platform-browser';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
@Component({
    selector: 'd3s-connector-labels',
    templateUrl: './connector-labels-sidebar.component.html',
    providers: [ConnectorLabelService]
})

export class ConnectorLabelsComponent extends AdminBaseComponent {
    labels: ConnectorLabel[] = [];
    selected: ConnectorLabel[] = [];

    error: any;

    showDelete: boolean = false;
    showEditor: boolean = false;
    showConsolidate: boolean = false
    filters: any = { globalSearch: '', Value: '', UseCount: '' };
    sort: any;

    private deletePopupTitle: string = 'Delete Connector Label';
    private editPopupTitle: string = 'Edit Connector Label';
    private isUsageLoading: boolean = false;
    private deleteConfirmationText: string = '';
    private labelUsage: any;
    public theDeleteCallback: Function;

    @ViewChild('dt', { static: false }) tableEl: any;
    private lastSelectedElement: ConnectorLabel;

    constructor(private router: Router,
        private connectorLabelService: ConnectorLabelService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private messagesService: MessagesObservableService,
        titleService: Title,
        secondaryNavService: SecondaryNavService,
        private cdRef: ChangeDetectorRef
    ) {
        super(headerBreadcrumbService, titleService, secondaryNavService);
        this.areaName = "Diagram Assets";
        this.setCommonItems();
        this.tabTitle = 'Diagram Assets';
        this.secondaryNavService.setCurrentArea(this.areaName, 'fa-sliders', this.tabTitle);

        this.buildSecondaryNavigationForObject(0, "ConnectorLabel");


    }

    ngOnInit() {
        this.setCommonSecondaryNavTabs(true);

        if (this.auditSidebar) {
            this.auditSidebar.url = `/sidebar/audit/connectorLabels`;
        }
        this.getLabels();

        this.theDeleteCallback = this.deleteLabel.bind(this);
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    updateSort(event) {
        this.sort = event;
    }
    onFilterChange(event) {
        if (event != 'globalSearch')
            this.filters.globalSearch = '';

        this.filters[event.prop] = event.value;
    }
    getLabels() {
        this.isLoading = true;
        this.connectorLabelService.getLabelList().subscribe(res => {
            if (res && res.length > 0) {
                this.labels = res.sort((a, b) => a.Value.localeCompare(b.Value));
            }
            this.isLoading = false;
        }, err => this.error = err);
    }

    private deselectElement(element: any) {
        element.classList.remove('ui-state-highlight');
        if (element.querySelector('span.ui-chkbox-icon')) {
            element.querySelector('span.ui-chkbox-icon').classList.remove('pi-check');
            element.querySelector('span.ui-chkbox-icon').classList.remove('pi');
            element.querySelector('div.ui-chkbox-box').classList.remove('ui-state-active');
        }
    }
    private selectElement(element: any) {
        element.classList.add('ui-state-highlight');
        if (element.querySelector('span.ui-chkbox-icon')) {
            element.querySelector('span.ui-chkbox-icon').classList.add('pi-check');
            element.querySelector('span.ui-chkbox-icon').classList.add('pi');
            element.querySelector('div.ui-chkbox-box').classList.add('ui-state-active');
        }
    }

    private clearAllSelectedItems(element: any) {
        var nodeList = this.tableEl.el.nativeElement.querySelectorAll("tr.ui-state-highlight");
        Array.from(nodeList)
            .forEach(x => {
                this.deselectElement(x);
            });
        if (nodeList.length == 0)
            this.selectElement(element);

    }

    selectSingleItem(event: MouseEvent, item: ConnectorLabel, element: ElementRef = null) {
        this.editPopupTitle = 'Edit Connector Label';


        //p table options and eventing doesnt handle multiple selection well, this is custom implementation of ctrl/shift holding while selecting
        if (event && element) {
            if ((event.ctrlKey || event.metaKey) && !event.shiftKey) {
                if (this.selected.filter(x => x.uid == item.uid).length > 0) {
                    this.selected = this.selected.filter(x => x.uid != item.uid);
                    var el = (<any>(event.target)).parentNode;
                    el = (el.nodeName === "TD") ? el.parentNode : el;
                    this.deselectElement(el);
                }
                else {
                    this.selected.push(item);
                    var el = (<any>(event.target)).parentNode;
                    el = (el.nodeName === "TD") ? el.parentNode : el;
                    this.selectElement(el);
                }

                this.lastSelectedElement = item;
                return;
            }
            if (event.shiftKey) {
                var lastIndex = this.labels.indexOf(this.lastSelectedElement);
                if (lastIndex == -1 && this.selected.length == 1) {
                    lastIndex = this.labels.indexOf(this.selected[0]);
                }
                var currentIndex = this.labels.indexOf(item);

                if (lastIndex > currentIndex) {
                    lastIndex += currentIndex;
                    currentIndex = lastIndex - currentIndex;
                    lastIndex -= currentIndex;
                }

                var tableRows = (<any>this.tableEl).el.nativeElement.querySelectorAll('table tbody tr');
                for (var i = lastIndex; i <= currentIndex; i++) {
                    if (!tableRows[i].classList.contains('ui-state-highlight')) {
                        this.selected.push(this.labels[i]);
                        this.selectElement(tableRows[i]);
                    }
                }

                this.lastSelectedElement = item;
                return;
            }

        }
        let target = (<any>(event.target));
        if (element && target.nodeName !== "P-TABLECHECKBOX") {
            var el = (<any>(event.target));
            if (el.nodeName === "I")
                el = el.parentNode.parentNode.parentNode; //gets <a>-><div>-><td>
            if (el.nodeName === "A")
                el = el.parentNode.parentNode; //gets <div>-><td>
            el = (el.nodeName === "TD") ? el.parentNode : el;
            this.clearAllSelectedItems(el);
            this.selected = [];
            this.selected.push(item);
            this.lastSelectedElement = item;
        } else {
            if (this.selected.filter(x => x.uid == item.uid).length > 0) {
                this.selected = this.selected.filter(x => x.uid != item.uid);
                var el = (<any>(event.target)).parentNode;
                el = (el.nodeName === "TD") ? el.parentNode : el;
                this.deselectElement(el);
            }
            else {
                this.selected.push(item);
                var el = (<any>(event.target)).parentNode;
                this.selectElement(el);
            }
            this.lastSelectedElement = item;
        }
    }


    closeEditor() {
        this.showEditor = false;
        this.cdRef.markForCheck();
    }

    openEditor() {
        this.showEditor = true;
        this.cdRef.markForCheck();
    }

    add() {
        this.selected = [];
        this.editPopupTitle = 'Add Connector Label';
        this.showEditor = true;
        this.cdRef.markForCheck();
    }
    saveLabel(event) {
        if (event.additionalOption && event.additionalOption.uid) {
            let arr: string[] = [];
            arr.push(event.item.uid);
            this.consolidateLabels(event.additionalOption.uid, arr);
            return;
        }

        this.connectorLabelService.saveLabel(event.item)
            .subscribe(result => {
                let msg: string = '';
                if (event.item.uid == undefined) {
                    msg = `${result.Value} succesfully created`;
                }
                else {
                    msg = `${result.Value} succesfully updated`;
                }
                this.showMessageForResult(this.messagesService, result, msg);
                if (event.item.uid == undefined) {
                    this.labels.push(result);
                }
                else {
                    this.labels[this.findLabelIndex(event.item.uid)].Value = event.item.Value;
                }
                this.labels = this.labels.sort((a, b) => a.Value.localeCompare(b.Value));

                this.selected = [];
                event.item.UseCount = 0;
                this.selected.push(event.item);

                this.showEditor = false;

            });
    }

    consolidateLabels(parentUid: string, childrenUids: string[]) {
        this.connectorLabelService.consolidateTags(parentUid, childrenUids)
            .subscribe(result => {

                if (result) {

                    this.messagesService.showInfoMessage("Success", "Connector label consolidation succesfull");

                    this.getLabels();
                }
                this.selected = [];
                this.selected.push(this.labels[0])
                this.showConsolidate = false;
                this.showEditor = false;
            }, err => {
                this.showMessageForResult(this.messagesService, err);
                this.showConsolidate = false;
                this.showEditor = false;

            });
    }

    deleteLabel() {
        this.connectorLabelService.deleteLabels(this.selected).
            subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                //remove the template with this id from the grid
                if (result.type != 'error') {
                    this.selected.forEach(t => {
                        this.labels.splice(this.findLabelIndex(t.uid), 1);
                    })
                    this.selected = [];
                }
                this.showDelete = false;
                this.cdRef.markForCheck();
            }, err => this.showMessageForResult(this.messagesService, err));
    }

    private lastLoadedUid: string = '';
    openDeleteModal(labelUid: string) {

        if (this.lastLoadedUid != labelUid)
            this.isUsageLoading = true;

        this.lastLoadedUid = labelUid;
        setTimeout(() => {
            this.deletePopupTitle = this.selected.length == 1 ? 'Delete Connector Label' : 'Delete Connector Labels';
            this.deleteConfirmationText = `Delete the Connector Label '${this.selected[0].Value}'`;
            this.showDelete = true;
        }, 100);
    }

    private usageLoaded(data) {
        this.isUsageLoading = false;
        this.labelUsage = data;
        this.cdRef.markForCheck();
    }

    openConsolidateModal() {
        this.showConsolidate = true;
    }
    findLabelIndex(uid: string) {
        var index: number = -1;
        for (var label of this.labels) {
            index++;
            if (label.uid == uid) return index;
        }
    }

    private export() {
        this.connectorLabelService.exportLabels(this.filters, this.sort);
    }

    private exportUsage() {
        var selected = this.selected[0];
        this.connectorLabelService.exportLabelUsage(selected.uid, `Connector Label "${selected.Value}"`)
    }
}
