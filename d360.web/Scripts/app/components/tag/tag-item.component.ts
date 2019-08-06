import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { RulesService } from '../../services/rules.service';
import { PermissionsService } from '../../services/permissions.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { AssetAction, EditFormData, DeleteFormData } from '../../models/rightsidebar.model';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { TagService } from '../../services/tag.service';
import { TagType, TagDetail, TagItem } from '../../models/tag.model';
import { Location } from '@angular/common';
import { forEach } from '@angular/router/src/utils/collection';


@Component({
    selector: 'd3s-tag-item',
    providers: [RulesService, PermissionsService, TagService, GridDefinitionService],
    templateUrl: 'tag-item.component.html',
    host: { 'class': 'gov-detail-page' }
})

export class TagItemComponent extends BaseComponent implements OnInit, OnDestroy {
    routeParamsSubscription: any;
    tagUid: number;
    tag: TagType;
    tagUsage: TagDetail[];
    selected: TagDetail;
    private currentAreaName: string;

    private backUrl: string;


    private sub: any;
    actions: AssetAction;


    constructor(private route: ActivatedRoute,
        private router: Router,
        private loc: Location,
        protected tagsService: TagService,
        protected titleService: Title,
        protected messagesService: MessagesObservableService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        rightSidebarService: RightSidebarService) {
        super();
        this.rightSidebarService = rightSidebarService;

    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.tagUid = params['tagUid'];
            this.headerBreadcrumbService.setCurrentObjectInfo('Tag', this.tagUid);
            this.logAction('open', 'Tag', this.tagUid);
            this.isLoading = true;

            this.loadPermissions(this.permissionsService, "Tag", this.tagUid)
                .then(p => {
                    this.load();
                });

            this.currentAreaName = "Tag";
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.rightSidebarService.clearActions();
        this.clearSidebar();
    }

    load() {
        this.isLoading = true;
        this.tagsService.getTagByUid(this.tagUid)
            .subscribe(result => {
                if (result) {
                    this.tag = result;
                    this.setObjectInfo('Tag', this.tagUid);
                    this.buildBreadcrumb();
                    this.setBrowserTitle(this.titleService, this.tag.Value);

                    this.setObjectInfo(
                        'Tag',
                        this.tagUid,
                        this.tag.Value,
                        null,
                        null,
                        this.tag.uid
                    );
                    this.setCommonRightSideBar(true);
                    this.rightSidebarService.showHeader(true);

                    this.setActions();

                    if (this.auditSidebar) {
                        this.auditSidebar.hasDynamicUrl = true;
                        this.auditSidebar.dynamicUrlCallback = (() => {
                            return `/sidebar/audit/Tag/${this.tagUid}`
                        });
                    }

                    this.tagsService.getTagDetails(this.tag.uid)
                        .subscribe(data => {

                            this.tagUsage = data.items;
                            if (this.tagUsage.length > 0) {
                                this.selected = this.tagUsage[0];
                            }
                            this.tagUsage.forEach(tu => {
                                tu.TagsAsString = tu.Tags.map(x => x.Value).join('|');
                            })
                            this.isLoading = false;
                        });
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
        this.rightSidebarService.setCurrentArea(this.tag.Value, 'fa-tag', 'Tagged Assets');

    }

    getAssetType(item: TagDetail) {

        switch (item.AssetType) {
            case 'ArtifactType': return `Glossary <i class='fa fa-angle-right'></i> ${item.AssetTypeName}`;
            case 'PolicyType': return `Policy <i class='fa fa-angle-right'></i> ${item.AssetTypeName}`;
            case 'TaxonomyType': return `Model <i class='fa fa-angle-right'></i> ${item.AssetTypeName}`;
            case 'RuleType': return `Rule <i class='fa fa-angle-right'></i> ${item.AssetTypeName}`;
            default: return '';
        }

    }

    openTagPage(item: TagItem) {
        if (item.Uid != this.tagUid) {
            this.openTagPageByID(item.Uid);
        }
    }

    openTagPageByID(id) {
        this.router.navigate([`${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${id}`]);
    }

    export() {
        this.tagsService.exportTagsByUid(this.tag.uid);
    }

    setActions() {
        this.actions = new AssetAction();
        this.actions.isVisible = true;
        this.actions.showBack = true;
        this.actions.showDelete = true;
        this.actions.showEdit = true;
        this.actions.editCallback = this.onActionEditClick.bind(this);
        this.actions.deleteCallback = this.onActionDeleteClick.bind(this);
        this.actions.backCallback = this.onActionBackClick.bind(this);

        let editAction: EditFormData = new EditFormData();
        editAction.title = 'Edit Tag';
        editAction.closeClick = this.onActionEditCloseClick.bind(this);
        editAction.selected = { uid: this.tag.uid, Value: this.tag.Value, UseCount: this.tag.UseCount };
        editAction.isModalVisible = false;
        editAction.modalTitle = "Edit Tag";
        editAction.objectID = this.tag.uid;
        editAction.objectType = 'Tag';
        editAction.saveClick = this.saveTag.bind(this);
        editAction.showAsModal = true;

        let deleteAction: DeleteFormData = new DeleteFormData();
        deleteAction.callback = this.deleteCallback.bind(this);
        deleteAction.item = { uid: this.tag.uid, Value: this.tag.Value, UseCount: this.tag.UseCount };
        deleteAction.modalTitle = 'Delete Tag';
        deleteAction.isModalVisible = false;
        deleteAction.showAsModal = true;

        this.actions.edit = editAction;
        this.actions.delete = deleteAction;

        this.rightSidebarService.setActionTitleItems(this.actions);
    }



    onActionEditCloseClick() {
        if (this.actions) {
            this.actions.edit.isModalVisible = false;
            this.rightSidebarService.setActionTitleItems(this.actions);
        }
    }

    onActionEditClick() {
        this.actions.edit.isModalVisible = true;
        this.rightSidebarService.setActionTitleItems(this.actions);
    }

    onActionDeleteCloseClick() {
        if (this.actions) {
            this.actions.delete.isModalVisible = false;
            this.rightSidebarService.setActionTitleItems(this.actions);
        }
    }

    onActionDeleteClick() {
        this.actions.delete.isModalVisible = true;
        this.rightSidebarService.setActionTitleItems(this.actions);
    }

    onActionBackClick() {
        this.loc.back();
    }

    deleteCallback() {
        let tagForDelete: TagType[] = [];
        tagForDelete.push(this.tag);
        this.tagsService.deleteTags(tagForDelete).
            subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                this.onActionBackClick();

            }, err => this.showMessageForResult(this.messagesService, err));
    }

    saveTag(event) {

        if (event.additionalOption && event.additionalOption.code) {
            let arr: string[] = [];
            arr.push(event.item.uid);
            this.consolidateTags(event.additionalOption.code, arr);
            return;
        }

        this.tagsService.saveTag(event.item)
            .subscribe(result => {
                let msg: string = '';
                if (event.item.uid == undefined) {
                    msg = `${result.Value} succesfully created`;
                }
                else {
                    msg = `${result.Value} succesfully updated`;
                }
                this.tag = event.item;
                this.showMessageForResult(this.messagesService, result, msg);
                this.rightSidebarService.setCurrentArea(this.tag.Value, 'fa-tag', 'Tagged Assets');
                this.setBrowserTitle(this.titleService, this.tag.Value);

                this.tagUsage.forEach(detail => {
                    detail.Tags.forEach(t => {
                        if (t.Uid == this.tagUid) {
                            t.Value = event.item.Value;
                        }
                    });
                });

                this.onActionEditCloseClick();


            });
    }

    consolidateTags(parentUid: string, childrenUids: string[]) {
        this.tagsService.consolidateTags(parentUid, childrenUids)
            .subscribe(result => {

                if (result) {
                    this.messagesService.showInfoMessage("Success", "Tag consolidation succesfull");
                    this.onActionEditCloseClick();
                    this.openTagPageByID(parentUid);

                }
            }, err => {
                this.showMessageForResult(this.messagesService, err);

            });
    }

};