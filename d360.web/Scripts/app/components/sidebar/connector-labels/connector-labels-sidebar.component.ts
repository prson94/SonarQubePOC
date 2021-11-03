import { Component, ViewChild, ChangeDetectorRef, ElementRef } from '@angular/core';
import { AdminBaseComponent } from '../../admin/admin-base.component';
import { ConnectorLabel } from '../../../models/connectorLabel.model';
import { Router } from '@angular/router';
import { ConnectorLabelService } from '../../../services/connectorLabel.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Title } from '@angular/platform-browser';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { CompanySettingsService } from '../../../services/settings.service';
@Component({
    selector: 'd3s-connector-labels',
    templateUrl: './connector-labels-sidebar.component.html',
    providers: [ConnectorLabelService]
})

export class ConnectorLabelsComponent extends AdminBaseComponent {
    labels: ConnectorLabel[] = [];
    selected: ConnectorLabel[] = [];
    rowsPerPage: number = 25;
    rowsPerModal: number = 5;
    error: any;

    showDelete: boolean = false;
    showEditor: boolean = false;
    showConsolidate: boolean = false
    filters: any = { globalSearch: '', Value: '', UseCount: '' };
    sort: any;

    deletePopupTitle: string = 'Delete Connector Label';
    editPopupTitle: string = 'Edit Connector Label';
    isUsageLoading: boolean = false;
    deleteConfirmationText: string = '';
    labelUsage: any;
    public theDeleteCallback: Function;
    isSaving: boolean = false;

    showConsolidationPopup: boolean = false;
    consolidateValue: string;


    @ViewChild('dt', { static: false }) tableEl: any;
    @ViewChild('usageTable', { static: false }) tableEl1: any;
    @ViewChild('usageTableConsolidate', { static: false }) tableEl2: any;

    selectedCount: number = 0;
    lastSelectedElement: ConnectorLabel;

    constructor(private router: Router,
        private connectorLabelService: ConnectorLabelService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private messagesService: MessagesObservableService,
        titleService: Title,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef
    ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
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
            this.labels = [];
            if (res && res.length > 0) {
                this.labels = res.sort((a, b) => a.Value.localeCompare(b.Value));
            }
            this.isLoading = false;
        }, (err) => this.error = err);
    }

    closeEditor() {
        this.showEditor = false;
        this.cdRef.markForCheck();
    }

    openEditor(label: ConnectorLabel) {
        this.selected = [label];
        this.showEditor = true;
        this.editPopupTitle = 'Edit Connector Label';
        this.cdRef.markForCheck();
    }

    add() {
        this.selected = [];
        this.editPopupTitle = 'Add Connector Label';
        this.showEditor = true;
        this.cdRef.markForCheck();
    }

    consolidateClick() {
        if (!this.consolidateValue || this.consolidateValue.trim() == "") {
            console.error("Cannot consolidate connectors without selecting a connector to keep.")
            return;
        }
        this.isSaving = true;
        var children = [];
        this.selected.forEach(label => {
            if (label.uid != this.consolidateValue) {
                children.push(label.uid);
            }
        });
        this.consolidateLabels(this.consolidateValue, children);
    }

    saveLabel(event) {
        this.isSaving = true;
        if (event.additionalOption && event.additionalOption.uid) {
            let arr: string[] = [];
            arr.push(event.item.uid);
            this.consolidateLabels(event.additionalOption.uid, arr);
            return;
        }

        this.connectorLabelService.saveLabel(event.item)
            .subscribe((result) => {
                let msg: string = '';
                if (event.item.uid == undefined) {
                    msg = `Connector label succesfully created`;
                }
                else {
                    msg = `Connector label succesfully updated`;
                }
                this.showMessageForResult(this.messagesService, result, msg);
                this.getLabels();
                this.showEditor = false;
                this.isSaving = false;

            });
    }

    consolidateLabels(parentUid: string, childrenUids: string[]) {
        this.connectorLabelService.consolidateConnectorLabels(parentUid, childrenUids)
            .subscribe(result => {

                if (result) {

                    this.messagesService.showInfoMessage("Success", "Connector label consolidation succesfull");

                    this.getLabels();
                }
                this.selected = [];
                this.consolidateValue = null;
                this.showConsolidate = false;
                this.showConsolidationPopup = false;
                this.showEditor = false;
                this.isSaving = false;
            }, err => {
                this.showMessageForResult(this.messagesService, err);
                this.showConsolidate = false;
                this.showEditor = false;
                this.isSaving = false;
            });
    }

    deleteLabel() {
        this.connectorLabelService.deleteLabels(this.selected).
            subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                //remove the template with this id from the grid
                if (result.type != 'error') {
                    this.selected = [];
                }
                this.getLabels();
                this.showDelete = false;
                this.cdRef.markForCheck();
            }, err => this.showMessageForResult(this.messagesService, err));
    }

    private lastLoadedUid: string = '';

    onRowSelected() {

        if (this.lastLoadedUid != this.selected[0].uid) {
            this.isUsageLoading = true;
            this.lastLoadedUid = this.selected[0].uid;
            this.cdRef.markForCheck();
        }
    }

    openDeleteModal(label: ConnectorLabel) {
        this.selected = [label];

        if (this.lastLoadedUid != label.uid) {
            this.cdRef.markForCheck();
            this.isUsageLoading = true;
        }

        this.lastLoadedUid = label.uid;
        setTimeout(() => {
            this.deletePopupTitle = this.selected ? 'Delete Connector Label' : 'Delete Connector Labels';
            this.deleteConfirmationText = `Delete the Connector Label '${this.selected[0].Value}'`;
            this.isUsageLoading = false;
            this.showDelete = true;
            this.cdRef.markForCheck();
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

    export() {
        this.connectorLabelService.exportLabels(this.filters, this.sort);
    }

    exportUsage() {
        this.selected.forEach(item => {
            this.connectorLabelService.exportLabelUsage(item.uid, `Where Used report for Connector Label "${item.Value}"`)
        })
    }


    openDetailsPage(item: ConnectorLabel) {
        this.router.navigate([`${SiteUrlHelpers.SITE_URL_CONNECTORLABEL_ROOT}/${item.uid}`]);
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
                this.selectedCount = this.selected.length;
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
                    if (!tableRows[i].classList.contains('p-highlight')) {
                        this.selected.push(this.labels[i]);
                        this.selectElement(tableRows[i]);
                    }
                }

                this.lastSelectedElement = item;
                this.selectedCount = this.selected.length;
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
            if (this.tableEl1)
                this.tableEl1.totalRecords = this.selected.length;

            if (this.tableEl2)
                this.tableEl2.totalRecords = this.selected.length;
            this.lastSelectedElement = item;
        }
        this.selectedCount = this.selected.length;
    }
    private deselectElement(element: HTMLElement) {
        var trElement = this.getTrElement(element);

        trElement.classList.remove('p-highlight');
        trElement.querySelector('span.p-checkbox-icon').classList.remove('pi-check');
        trElement.querySelector('span.p-checkbox-icon').classList.remove('pi');
        trElement.querySelector('div.p-checkbox-box').classList.remove('p-state-active');

    }
    private selectElement(element: HTMLElement) {
        var trElement = this.getTrElement(element);

        trElement.classList.add('p-highlight');
        trElement.querySelector('span.p-checkbox-icon').classList.add('pi-check');
        trElement.querySelector('span.p-checkbox-icon').classList.add('pi');
        trElement.querySelector('div.p-checkbox-box').classList.add('p-state-active');

    }

    private getTrElement(element: HTMLElement) {
        if (element.tagName === "TR")
            return element;

        else
            return this.getTrElement(element.parentElement);
    }

    private clearAllSelectedItems(element: any) {
        var nodeList = this.tableEl.el.nativeElement.querySelectorAll("tr.p-highlight");
        Array.from(nodeList)
            .forEach(x => {
                this.deselectElement(x as HTMLElement);
            });
        if (nodeList.length == 0)
            this.selectElement(element);

    }

    actionSelected($event) {
        if ($event.value === 'Delete') {
            this.showDelete = true;
            this.deletePopupTitle = 'Delete Connector Labels';
            this.deleteConfirmationText = `Delete all Connector Labels listed above`;
        }

        if ($event.value === 'Consolidate') {
            this.showConsolidationPopup = true;
        }
    }

    multiselectMenu = [
        {
            title: 'Delete'
        },
        {
            title: 'Consolidate'
        }
    ]
}
