import { Component, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { ActivatedRoute, Router } from '@angular/router';
import { PermissionsService } from '../../services/permissions.service';
import { ConnectorLabelService } from '../../services/connectorLabel.service';
import { ConnectorLabel, ConnectorLabelUsage } from '../../models/connectorLabel.model';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { AssetAction, EditFormData } from '../../models/secondaryNav.model';
import { Location } from '@angular/common';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-connector-label-item',
    providers: [PermissionsService, ConnectorLabelService],
    templateUrl: 'connector-label-item.component.html',
    host: { 'class': 'gov-detail-page' }
})

export class ConnectorLabelItemComponent extends BaseComponent implements OnInit {

    label: ConnectorLabel;
    private usage: ConnectorLabelUsage[] = [];
    private sub: any;
    private labelUid: number;
    private isAdmin: boolean = false;
    private currentAreaName: string;

    private actions: AssetAction;
    isEditorVisible: boolean = false;
    isDeleteVisible: boolean = false;
    isSaving: boolean = false;

    deletePopupTitle = 'Delete Connector Label';
    deleteConfirmationText = '';

    private theDeleteCallback: Function;

    filters: any = { globalSearch: '', Diagram: '', AssetTypeName: '', Occurrences: '' };
    sort: any;

    constructor(
        private route: ActivatedRoute,
        secondaryNavService: SecondaryNavService,
        protected permissionsService: PermissionsService,
        private connectorLabelService: ConnectorLabelService,
        protected titleService: Title,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private router: Router,
        private loc: Location,
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.theDeleteCallback = this.deleteLabel.bind(this);
    }

    updateSort(event) {
        this.sort = event;
    }
    onFilterChange(event) {
        this.filters[event.prop] = event.value;
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.labelUid = params['labelUid'];

            this.secondaryNavService.clearCurrentObject();

            this.logAction('open', 'ConnectorLabel', this.labelUid);
            this.isLoading = true;

            this.loadPermissions(this.permissionsService, "ConnectorLabel", this.labelUid)
                .then(p => {
                    if (this.hasModifyAssetPermissions() && this.hasDeleteAssetPermissions()) {
                        this.isAdmin = true;
                    }
                    this.load();
                });



        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
        this.secondaryNavService.clearActions();
        this.clearSidebar();
    }

    private load() {
        this.isLoading = true;
        this.connectorLabelService.getLabelByUid(this.labelUid.toString())
            .subscribe(result => {
                if (result) {
                    this.label = result;
                    this.setObjectInfo('ConnectorLabel', this.labelUid);
                    this.buildBreadcrumb();
                    this.setBrowserTitle(this.titleService, this.label.Value);
                    this.deleteConfirmationText = `Delete the Connector Label '${this.label.Value}'`;

                    this.setObjectInfo(
                        'ConnectorLabel',
                        this.labelUid,
                        this.label.Value,
                        null,
                        null,
                        this.label.uid
                    );


                    if (this.isAdmin) {

                        this.setCommonSecondaryNavTabs(false);

                        //Coming soon
                        //if (this.auditSidebar) {
                        //    this.auditSidebar.url = `/sidebar/audit/ConnectorLabel/${this.labelUid}`;
                        //}
                    }
                    else {
                        this.setCommonSecondaryNavTabs(false);

                    }
                    this.setActions();

                    this.secondaryNavService.showHeader(true);

                    this.connectorLabelService.getLabelUsage(this.label.uid)
                        .subscribe(data => {
                            this.usage = data;
                            this.isLoading = false;
                        });


                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.currentAreaName = "Connector Labels";
                    let areaBreadcrumb = new Breadcrumb(
                        this.currentAreaName, ``
                    );

                    let itemBreadcrumb = new Breadcrumb(
                        this.label.Value,
                        `${SiteUrlHelpers.SITE_URL_CONNECTORLABEL_ROOT}/${this.label.uid}`
                    )

                    this.headerBreadcrumbService.showBreadcrumb(areaBreadcrumb);
                    this.headerBreadcrumbService.showBreadcrumb(itemBreadcrumb);
                }
                else {
                    this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);

                }

            },
                err => {
                    this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
                });


    }

    buildBreadcrumb() {
        this.secondaryNavService.setCurrentArea(this.label.Value, 'fa-tag', 'Where Used');
    }

    formatValue(item: ConnectorLabelUsage) {
        return item.AssetTypeName.replace('>', ` <i class='fa fa-angle-right'></i> `);
    }

    setActions() {
        this.actions = new AssetAction();
        this.actions.type = "CONNECTORLABEL";
        this.actions.isVisible = true;
        this.actions.showDelete = false;
        this.actions.showEdit = false;
        this.actions.showBack = true;

        this.actions.backCallback = this.onActionBackClick.bind(this);

        if (this.isAdmin) {
            this.actions.showEdit = true;
            this.actions.editCallback = this.onActionEditClick.bind(this);

            this.actions.showDelete = true;
            this.actions.deleteCallback = this.onActionDeleteClick.bind(this);
        }

        this.secondaryNavService.setActionTitleItems(this.actions);
    }

    onActionEditClick() {
        this.isEditorVisible = true;
        this.secondaryNavService.setActionTitleItems(this.actions);
    }

    onActionDeleteClick() {
        this.isDeleteVisible = true;
        this.secondaryNavService.setActionTitleItems(this.actions);
    }

    onActionBackClick() {
        this.loc.back();
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
            .subscribe(result => {
                let msg: string = '';
                if (event.item.uid == undefined) {
                    msg = `Connector label succesfully created`;
                }
                else {
                    msg = `Connector label succesfully updated`;
                }
                this.showMessageForResult(this.messagesService, result, msg);
                this.label.Value = event.item.Value;
                this.load();

                this.isEditorVisible = false;
                this.isSaving = false;

            });
    }

    consolidateLabels(parentUid: string, childrenUids: string[]) {
        this.connectorLabelService.consolidateConnectorLabels(parentUid, childrenUids)
            .subscribe(result => {

                if (result) {
                    this.messagesService.showInfoMessage("Success", "Connector label consolidation succesfull");
                }
                this.isEditorVisible = false;
                this.isSaving = false;
                this.openConnectorLabelPageByUID(parentUid);

            }, err => {
                this.showMessageForResult(this.messagesService, err);
                this.isEditorVisible = false;
                this.isSaving = false;

            });
    }

    deleteLabel() {
        this.connectorLabelService.deleteLabels([this.label]).
            subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                this.isDeleteVisible = false;
                this.onActionBackClick();
            }, err => this.showMessageForResult(this.messagesService, err));
    }
    private exportUsage() {
        this.connectorLabelService.exportLabelUsage(this.label.uid, `Where Used report for Connector Label "${this.label.Value}"`)
    }
    openConnectorLabelPageByUID(uid: string) {
        this.router.navigate([`${SiteUrlHelpers.SITE_URL_CONNECTORLABEL_ROOT}/${uid}`]);
    }
    private export() {
        this.connectorLabelService.exportLabelUsage(this.label.uid, `Connector Labels`, this.sort, this.filters)
    }
}