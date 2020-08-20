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
    selected: ConnectorLabel;

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

    closeEditor() {
        this.showEditor = false;
        this.cdRef.markForCheck();
    }

    openEditor(label: ConnectorLabel) {
        this.selected = label;
        this.showEditor = true;
        this.editPopupTitle = 'Edit Connector Label';
        this.cdRef.markForCheck();
    }

    add() {
        this.selected = null;
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

                this.selected = null;
                event.item.UseCount = 0;
                this.selected = event.item;

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
                this.selected = null;
                this.selected = this.labels[0];
                this.showConsolidate = false;
                this.showEditor = false;
            }, err => {
                this.showMessageForResult(this.messagesService, err);
                this.showConsolidate = false;
                this.showEditor = false;

            });
    }

    deleteLabel() {
        this.connectorLabelService.deleteLabels([this.selected]).
            subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                //remove the template with this id from the grid
                if (result.type != 'error') {

                    this.labels.splice(this.findLabelIndex(this.selected.uid), 1);
                    this.selected = null;
                }
                this.showDelete = false;
                this.cdRef.markForCheck();
            }, err => this.showMessageForResult(this.messagesService, err));
    }

    private lastLoadedUid: string = '';
    openDeleteModal(label: ConnectorLabel) {
        this.selected = label;

        if (this.lastLoadedUid != label.uid)
            this.isUsageLoading = true;

        this.lastLoadedUid = label.uid;
        setTimeout(() => {
            this.deletePopupTitle = this.selected ? 'Delete Connector Label' : 'Delete Connector Labels';
            this.deleteConfirmationText = `Delete the Connector Label '${this.selected.Value}'`;
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

    private export() {
        this.connectorLabelService.exportLabels(this.filters, this.sort);
    }

    private exportUsage() {
        this.connectorLabelService.exportLabelUsage(this.selected.uid, `Where Used report for Connector Label "${this.selected.Value}"`)
    }
}
