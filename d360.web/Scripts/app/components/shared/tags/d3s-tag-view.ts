import { Input, Component, OnInit, ViewChild, ElementRef, ChangeDetectorRef, ChangeDetectionStrategy, OnDestroy, EventEmitter, Output, OnChanges, SimpleChanges } from '@angular/core';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { TagType, TagApiModel } from '../../../models/tag.model';
import { TagService } from '../../../services/tag.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Router } from '@angular/router';
import { AuthenticationService } from '../../../services/authentication.service';
import { BaseComponent } from '../base.component';
import { StateService } from '../../../services/state.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { DatePipe } from '@angular/common';
import { LinkClickInterceptor } from '../../../services/href-click-service';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-tag-view',
    templateUrl: './d3s-tag-view.html',
    providers: [TagService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { '(window:resize)': 'manageWidth()' }
})

export class TagView extends BaseComponent implements OnInit, OnDestroy, OnChanges {
    public theDeleteCallback: Function;
    @ViewChild('tagInput', { static: false }) tagInput: ElementRef;
    @Input() data: any;
    @Input() isEditable: boolean = false;
    @Input() allowAddTag: boolean = false;
    @Input() assetUID: string;
    @Input() assetUIDList: string[];
    @Input() ignoreResizing: boolean = false;
    @Input() placeHolder: string = "Click to add...";
    @Output() tagsChanged = new EventEmitter<any[]>();
    @Input() interceptLinkClick: boolean = false;

    showEditor: boolean = false;
    showDelete: boolean = false;
    savingTag: boolean = false;
    private inputValue: any;
    tagNoSpaces: string = "";
    private searchResults: any[] = [];
    tags: any[];
    private resultPanel: any;
    private targetPanel: any;
    private tagsLoading = false;
    private tagID: any;
    existingTag: boolean = false;
    selected: TagType[] = [];
    private selectedtag: TagType = new TagType();
    private isShowAll: boolean = false;
    showDeleteOption: boolean = false;
    @ViewChild("container", { static: false }) container: ElementRef;
    error: any;
    timeouthandle: any;
    resizeSub: any;

    tooltipValue = '';

    private tagTooltip: TagType;
    private isTooltipLoaded: boolean = false;
    EditingTagsLoading: boolean;
    deletingTag: boolean;
    tagNameBeingAdded: any;

    constructor(
        private auth: AuthenticationService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private stateService: StateService,
        private tagService: TagService,
        private ref: ChangeDetectorRef,
        private datePipe: DatePipe,
        private router: Router,
        private linkClickInterceptor: LinkClickInterceptor) {
        super(settingsService);
    }

    ngOnInit() {
        this.theDeleteCallback = this.deleteTags.bind(this);
        this.assignTagsFromData();
    }

    assignTagsFromData() {
        this.tags = [];
        if (this.data) {

            if (typeof this.data == 'string') {
                this.data.split('|').forEach(t => {
                    this.tags = this.tags.concat([{ Value: t, uid: null }])
                });
            } else if (typeof this.data == 'object') {
                if (Array.isArray(this.data) && this.data.every(item => typeof item === "string")) {
                    this.data.forEach(t => {
                        this.tags.push({ Value: t, uid: null });
                    });
                } else {
                    this.tags = this.data;
                }
            }
            if (this.tags) {
                this.tags = this.tags.sort((a, b) => a.Value.localeCompare(b.Value));
            }
            this.selected = this.tags;
        }

        this.resizeSub = this.stateService.recalculateTagSize$.subscribe(() => {
            setTimeout(() => this.manageWidth(), 200);
        });
    }

    populateTagUids(taglist: TagType[]) {
        if (taglist && taglist.length > 0) {
            this.tags.forEach(
                (t) => { t.uid = taglist.filter((r) => r.Value === t.Value)[0].uid; }
            );
        }
        this.selected = this.tags;
    }

    ngOnDestroy() {
        if (this.resizeSub) {
            this.resizeSub.unsubscribe();
        }
    }

    addTag(event, tag) {
        this.tagInput.nativeElement.style.background = "white";
        this.selectedtag.Value = tag.name;
        this.selectedtag.uid = tag.code;
        this.tagInput.nativeElement.value = tag.name;
        this.search(event, tag.name);
        this.saveTag({ Value: this.selectedtag.Value, uid: this.selectedtag.uid });
    }

    show(event, searchPanel, target) {
        this.resultPanel = searchPanel;
        this.targetPanel = target;
        searchPanel.show(event);
        this.tagInput.nativeElement.style.background = "white";
        target.style.background = "white";
        target.style.border = "1px solid #66A9D6";
        let lineDims = target.getBoundingClientRect();
        window.setTimeout(() => {
            let dispPanel = searchPanel.el.nativeElement.children[0];
            dispPanel.style.maxWidth = (window.innerWidth - lineDims.left - 5) + "px";
        }, 150);
    }

    search(event, searchValue) {
        this.tagsLoading = true;
        clearTimeout(this.timeouthandle);
        this.timeouthandle = window.setTimeout(() => {
            this.tagService.searchTagsTypeAhead(searchValue, 10)
                .subscribe(res => {
                    if (res && res.length > 0) {
                        this.searchResults = res.sort((a, b) => a.name.localeCompare(b.name));
                        this.tagsLoading = false;
                        this.ref.markForCheck();
                    }
                    else if (res && res.length == 0) {
                        this.searchResults = res;
                        this.tagsLoading = false;
                        this.ref.markForCheck();
                    }

                    this.searchResults.forEach(x => x.Value = x.name);

                }, err => this.error = err);
        }, 400);
    }

    getTags() {
        this.isLoading = true;
        this.tagService.getTagsList().subscribe(res => {
            if (res && res.length > 0) {
                this.tags = res.sort((a, b) => a.Value.localeCompare(b.Value));
                if (this.tags.length > 0) this.selected.push(this.tags[0]);
            }
            this.isLoading = false;
        }, err => this.error = err);
    }

    openDeleteModal(tag: any) {
        if (this.isEditable == true) {
            this.deleteTags(tag);
        }
    }

    checkKey(event, value) {
        if (event.key == "Enter" && !this.savingTag) {
            event.Value = value;
            this.saveTag(event);
        }
    }

    saveTag(event) {
        this.savingTag = true;
        this.existingTag = false;
        var tags = Array<TagApiModel>();
        event.Value = event.Value.trim();
        if (this.assetUIDList) {
            this.assetUIDList.forEach((uid) => {
                let tag = new TagApiModel();
                tag.AssetUID = uid;
                tag.TagName = event.Value;
                this.tags = this.tags.concat([tag])
            })
        } else {
            let tag = new TagApiModel();
            tag.AssetUID = this.assetUID;
            tag.TagName = event.Value;
            tags = tags.concat([tag])
        }
        this.tags.forEach(x => {
            if (x.Value == event.Value) {
                this.existingTag = true;
                this.showEditor = false;
                this.savingTag = false;
                this.messagesService.showError('Error', `Tag already assigned to Asset${(this.assetUIDList.length > 1 ? "s" : "")}`);
            }
        });

        if (event.Value.includes("|")) {
            this.existingTag = true;
            this.savingTag = false;
            this.messagesService.showError('Error', "Tag can't contain | character");
        }
        this.tagNoSpaces = event.Value.trim();
        if (this.tagNoSpaces.length < 1) {
            this.existingTag = true;
            this.savingTag = false;
            this.messagesService.showError('Error', "Tag must be as least 1 character long in length");
        }
        if (this.tagNoSpaces.length > 100) {
            this.existingTag = true;
            this.savingTag = false;
            this.messagesService.showError('Error', "Tag must be less then 100 characters in length");
        }
        if (!this.existingTag) {
            this.tagNameBeingAdded = tags[0].TagName;
            this.inputValue = ""
            this.searchResults = [];
            this.tagService.doesTagExist(event)
                .subscribe((result) => {
                    if (result == 200) {
                        this.tagService.createAssetTag(tags)
                            .subscribe(result => {
                                let msg: string = '';
                                if (result != null) {
                                    msg = `${event.Value} successfully added to ${tags.length === 1 ? "Asset" : "Assets"}`;
                                }
                                this.showMessageForResult(this.messagesService, result, msg);
                                event.uid = result[0].Uid;
                                this.tags = this.tags.concat([event])
                                this.tags = this.tags.sort((a, b) => a.Value.localeCompare(b.Value));
                                event.UseCount = 0;
                                this.searchResults = [];
                                this.inputValue = "";
                                this.savingTag = false;
                                this.tagsChanged.emit(this.tags);
                                this.EditingTagsLoading = false;
                                this.ref.markForCheck();
                            });
                    }
                },
                    (error) => {
                        if (error.status == 404) {
                            this.tagService.saveTag(event)
                                .subscribe(result => {
                                    let msg: string = '';
                                    if (event.uid == undefined) {
                                        msg = `${event.Value} successfully created`;
                                    }
                                    this.showMessageForResult(this.messagesService, result, msg);
                                    this.tagService.createAssetTag(tags)
                                        .subscribe(result => {
                                            let msg: string = '';
                                            if (event.uid == undefined) {
                                                msg = `${event.Value} successfully added to ${tags.length === 1 ? "Asset" : "Assets"}`;
                                            }
                                            this.showMessageForResult(this.messagesService, result, msg);
                                            if (event.uid == undefined) {
                                                event.uid = result[0].Uid;
                                                this.tags = this.tags.concat([event])
                                            }
                                            this.tags = this.tags.sort((a, b) => a.Value.localeCompare(b.Value));
                                            event.UseCount = 0;
                                            this.searchResults = [];
                                            this.inputValue = "";
                                            this.savingTag = false;
                                            this.EditingTagsLoading = false;
                                            this.tagsChanged.emit(this.tags);
                                            this.ref.markForCheck();
                                        });
                                });
                        }
                    },
                    () => {
                        this.EditingTagsLoading = false;
                    }
                );
        }
        this.showEditor = false;
    }

    deleteTags(selectedTag) {
        this.deletingTag = true;
        if (!selectedTag.uid || selectedTag.uid === null) {
            this.tagService.getTagsList().subscribe(
                (res) => {
                    this.populateTagUids(res);
                    selectedTag = this.tags.filter((x) => x.Value === selectedTag.Value)[0];
                    this.populateAndSendDeleteRequest(selectedTag);
                });
        } else {
            this.populateAndSendDeleteRequest(selectedTag);
        }
    }

    populateAndSendDeleteRequest(selectedTag) {
        var tags = Array<TagApiModel>();
        if (this.assetUIDList) {
            this.assetUIDList.forEach((uid) => {
                tags.push(this.getTagsApiModel(selectedTag, uid));
            });
        } else {
            tags.push(this.getTagsApiModel(selectedTag, this.assetUID));
        }
        this.tagID = selectedTag.uid;

        this.tagService.deleteAssetTag(tags).
            subscribe(result => {
                let msg: string = '';
                msg = `Tag successfully removed`;
                this.showMessageForResult(this.messagesService, result, msg);
                //remove the template with this id from the grid
                if (result.type != 'error') {
                    this.tags = this.tags.filter((x) => x.Value !== selectedTag.Value);
                }
                this.tagsChanged.emit(this.tags);
                this.deletingTag = false;
                this.ref.markForCheck();
            }, err => {
                this.showMessageForResult(this.messagesService, err);
                this.deletingTag = false;
            });
    }

    private getTagsApiModel(selectedTag, assetUid) {
        let tag = new TagApiModel();
        tag.AssetUID = assetUid;
        tag.TagName = selectedTag.Value;
        tag.TagUID = selectedTag.uid;
        return tag;
    }

    showRemoveTag() {
        if (this.isEditable) {
            if (!this.auth.isAdmin || !this.hasModifyAssetPermissions())
                this.showDeleteOption = false;
            if (this.auth.isAdmin || this.hasModifyAssetPermissions())
                this.showDeleteOption = true;
            if (!this.showDeleteOption) {
                var tagElements = this.container.nativeElement.querySelectorAll('.tagging');
                (function () {
                    if (typeof NodeList.prototype.forEach === "function") return false;
                    tagElements.forEach = Array.prototype.forEach;
                })();
                this.tags.forEach(tag => {
                    if (this.assetUIDList) {
                        this.assetUIDList.forEach((uid) => { this.checkIfTagOwner(tagElements, tag, uid) });
                    } else {
                        this.checkIfTagOwner(tagElements, tag, this.assetUID);
                    }
                })
            }
        }
    }

    checkIfTagOwner(tagElements, tag, assetUid) {
        if (tag.TooltipID) {
            this.tagService.getAssetTagDetails(tag.TooltipID, assetUid).
                subscribe(result => {
                    this.showDeleteOnOwnedTags(tagElements, tag, result);
                }, err => this.showMessageForResult(this.messagesService, err));
        } else {
            this.tagService.getAssetTagOwnerByName(tag.Value, assetUid).
                subscribe(result => {
                    this.showDeleteOnOwnedTags(tagElements, tag, result);
                }, err => this.showMessageForResult(this.messagesService, err));
        }
    }

    showDeleteOnOwnedTags(tagElements, tag, createdBy) {
        tagElements.filter = Array.prototype.filter;
        var showDelete = tagElements.filter(te => te.children[0].innerText.trim() == tag.Value.trim());
        if (showDelete.length == 1) {
            if (createdBy == CurrentResourceID)
                this.showDeleteOption = true;
            if (createdBy != CurrentResourceID) {
                tagElements.forEach(e => {
                    if (e.children[0].innerText.trim() == tag.Value.trim()) {
                        e.children[1].parentElement.removeChild(e.children[1]);
                    }
                })
            }
        }
    }

    showAllToggle(event: MouseEvent) {
        this.isShowAll = !this.isShowAll;
        event.stopPropagation();
        this.setVisibility();
    }

    resetValue() {
        this.inputValue = null;
    }

    setVisibility() {

        var items = Array.prototype.slice.call(this.container.nativeElement.querySelectorAll('.tag-item-wrapper'), 0);
        if (items.length > 0) {
            for (let index = 0; index < items.length; index++) {
                var aElement = this.getParentForResizing(items[index], 'A');
                if (!this.isShowAll && index > 9) {
                    aElement.classList.add('hide');
                }
                else {
                    aElement.classList.remove('hide');
                }
            }
        }
        this.ref.markForCheck();
    }

    private getParentForResizing(element: HTMLElement, tags: string): HTMLElement {
        var searchFor = tags.split(',');
        var el = null;
        searchFor.forEach(tagName => {
            if (element.parentElement.tagName == tagName) {
                el = element.parentElement;
            }
        });

        if (el) return el;

        if (element.parentElement) {
            return this.getParentForResizing(element.parentElement, tags);
        }
        return null;
    }

    ngAfterViewInit() {
        this.manageWidth();
        this.showRemoveTag();
    }


    openTagPage(event: MouseEvent, item: any) {
        if (this.interceptLinkClick) {
            item["TooltipType"] = "tag";
            this.linkClickInterceptor.sendEvent(event, item, `${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${item.uid.toString().toLowerCase()}`);
            return;
        }

        if ((this.isEditable != true && this.showDelete == false) || (<HTMLElement>event.target).className == 'tag-item-wrapper') {
            this.router.navigate([`${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${item.uid.toString().toLowerCase()}`]);
        }
        event.stopPropagation();
    }

    public highlight(item, input) {
        if (!input) {
            return item;
        }
        return item.replace(new RegExp(input, "gi"), match => {
            return '<span style="background: #F5FF57;">' + match + '</span>';
        });
    }

    enter(tag: any, el: HTMLElement) {
        this.isTooltipLoaded = false;
        this.tooltipValue = `<i class="fa fa-spinner fa-spin fa-2x"></i>`;
        this.tagService.getTagTooltip(tag.uid, this.assetUID, tag.Value)
            .subscribe(t => {
                if (t.length > 0) {
                    this.tagTooltip = t[0];
                } else {
                    this.tagTooltip = new TagType();
                }

                this.tags.forEach(x => {
                    if (x.Value == tag.Value) {
                        if (!x.uid) {
                            x.uid = t[0].TagUid;
                        }
                    }
                });

                this.isTooltipLoaded = true;
                this.tooltipValue = `<span class="span-break">${this.tagTooltip.Value}</span>
                            <span>Tag added by ${this.tagTooltip.CreatedBy} on ${(this.datePipe.transform(this.tagTooltip.CreatedOn, 'short'))}</span>`;

                if (this.interceptLinkClick) {
                    this.tooltipValue = `<span>Added by ${this.tagTooltip.CreatedBy} on ${(this.datePipe.transform(this.tagTooltip.CreatedOn, 'short'))}</span>`;
                }
                this.ref.markForCheck();
            });
    }


    findTagIndex1(uid: string) {
        var index: number = -1;
        for (var tag of this.tags) {
            index++;
            if (tag.uid == uid) return index;
        }
    }
    findTagIndex(id: number) {
        var index: number = -1;
        for (var tag of this.tags) {
            index++;
            if (tag.TooltipID == id) return index;
        }
    }

    manageWidth() {
        if (this.ignoreResizing) {
            this.setVisibility();
            return;
        }

        if (this.container) {
            let parent = this.getParentForResizing(this.container.nativeElement, 'TD,DIV');

            if (!parent) {
                console.warn("No suitable parent found for tag resizing!");
            } else if (parent.classList.contains("tagsnomanagewidth")) {
                this.setVisibility();
                return;
            }

            let ofWidth = parent ? parent.offsetWidth - 10 : 500;
            parent.classList.remove('no-text-overflow')
            this.container.nativeElement.style.width = ofWidth + 'px';

            var items = Array.prototype.slice.call(this.container.nativeElement.querySelectorAll('.tag-item-wrapper'), 0);
            if (items.length > 0) {
                for (let i = 0; i < items.length; i++) {
                    var x = items[i];
                    if (x.offsetWidth > ofWidth) {
                        x.setAttribute('original-width', x.offsetWidth);
                        x.style.maxWidth = (ofWidth - 30) + 'px';
                        x.classList.add('too-long');
                        x.setAttribute('max-width', ofWidth - 30);
                    }
                }
            }
            this.setVisibility();
        }
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.assignTagsFromData();
    }
}
