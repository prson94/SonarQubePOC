import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, Observable } from 'rxjs';

export enum AssetDetailClickType {
    Undefined = 'Undefined',
    Asset = 'Asset',
    ReferenceItem = 'ReferenceItem',
    User = 'User',
    Tag = 'Tag',
    Group = 'Group'
}

export class AssetDetailClickEvent {
    event: any;
    type: AssetDetailClickType;
    data: any;

    objectId: number;
    objectType: string;
    uid: string;
    assetTypeUid: string;
    url: string;
}

@Injectable({ providedIn: 'root' })
export class LinkClickInterceptor {
    private subject = new Subject<AssetDetailClickEvent>();

    constructor(private router: Router) { }

    sendEvent(origEvent: any, data: any, url: string, valueIndex = 0) {
        var adcEv = new AssetDetailClickEvent();
        adcEv.type = AssetDetailClickType.Undefined;
        adcEv.url = url;

        if (origEvent) {
            origEvent.preventDefault();
            origEvent.stopPropagation();

            var event = origEvent["from-context-method"] ?? "";

            if (event === "open") {
                this.router.navigateByUrl(url);
                return;
            }

            if (event === "new-tab") {
                window.open(url, '_blank');
                return;
            }

            if (data.column && data.column.uidfield === "SecurityAssetUid") {
                //clicked on ownership lookup
                if (data.column.text === "Via") {
                    adcEv.type = AssetDetailClickType.Group;
                    adcEv.objectType = "Group";
                    adcEv.uid = data.item.SecurityAssetUid;
                }
                else {
                    adcEv.type = AssetDetailClickType.User;
                    adcEv.objectType = "Resource";
                    adcEv.uid = data.item.ResourceUid;
                    if (data.item.SecurityAssetUid && !data.item.ResourceUid) {
                        adcEv.uid = data.item.SecurityAssetUid;
                    }
                }
            }

            if (data.DataType === "Lookup"
                || data.FieldName === "ReferenceList"
                || data.DataType === "color"
                || data.FieldName === "ArtifactParentName"
            ) {
                //list fields
                var val = data.Values[valueIndex];
                adcEv.event = origEvent;
                adcEv.type = data.FieldName !== "ReferenceList" ? AssetDetailClickType.Asset : AssetDetailClickType.ReferenceItem;
                adcEv.data = data;

                adcEv.objectId = val.TooltipID;
                adcEv.objectType = val.TooltipType;

                if (adcEv.objectType === "ReferenceItem") {
                    adcEv.type = AssetDetailClickType.ReferenceItem;
                }

                adcEv.uid = val.uid;
                adcEv.assetTypeUid = val.assetTypeUid;
            }

            if (data.ResourceID) {
                //call from members section of groups
                adcEv.type = AssetDetailClickType.User;
                adcEv.objectType = "Resource";
                adcEv.uid = data.uid;
            }

            if (data.ResourceUid) {
                adcEv.type = AssetDetailClickType.User;
                adcEv.objectType = "Resource";
                adcEv.uid = data.ResourceUid;
            }

            //this is a group object
            if (data.PrimaryOwnerUid) {
                adcEv.type = AssetDetailClickType.Group;
                adcEv.objectType = "Group";
                adcEv.uid = data.Uid;
            }

            if (data.column && data.column.uidfield && data.column.uidfield.indexOf("_Uid") > 0) {
                //clicked on preview column on relation lookup
                adcEv.type = AssetDetailClickType.Asset;
                adcEv.objectType = "Artifact";
                adcEv.uid = data.item[data.column.uidfield];
            }

            if (data.DataType === "Relationship") {
                var valRel = data.Values[valueIndex];
                adcEv.event = origEvent;
                adcEv.type = data.FieldName !== "ReferenceItem" ? AssetDetailClickType.Asset : AssetDetailClickType.ReferenceItem;
                adcEv.data = data;

                adcEv.objectId = valRel.TooltipID;
                adcEv.objectType = valRel.TooltipType;
                adcEv.uid = valRel.uid;
                adcEv.assetTypeUid = valRel.assetTypeUid;
            }

            if (data.TooltipType === "tag") {
                adcEv.event = origEvent;
                adcEv.type = AssetDetailClickType.Tag;
                adcEv.uid = data.uid;
            }

            //if tags object exist this did came from tagged assets page
            if (data.Tags) {
                adcEv.event = origEvent;
                adcEv.type = AssetDetailClickType.Asset;
                adcEv.objectId = data.ObjectID;
                adcEv.objectType = data.Object;
                adcEv.uid = data.AssetUid;
                adcEv.assetTypeUid = data.AssetTypeUid;
            }

        } else {
            adcEv = null;
        }
        this.subject.next(adcEv);
    }

    getEvents(): Observable<AssetDetailClickEvent> {
        return this.subject.asObservable();
    }


    handleEvent(baseComponent: any, event: AssetDetailClickEvent) {
        baseComponent.selectedAsset = null;
        baseComponent.selectedReferenceItem = null;
        baseComponent.selectedTag = null;

        //if some other tab is opened in side panel, force opening detail panel
        baseComponent.sidePanelTab = 'detail';

        if (event.type === AssetDetailClickType.Asset) {
            baseComponent.selectedAsset = { uid: event.uid, type: event.objectType };
        }

        if (event.type === AssetDetailClickType.ReferenceItem) {
            baseComponent.selectedReferenceItem = { uid: event.assetTypeUid, assetUid: event.uid, type: event.objectType, url: event.url };
        }

        if (event.type === AssetDetailClickType.User || event.type === AssetDetailClickType.Group) {
            baseComponent.selectedAsset = { uid: event.uid, type: event.objectType };
        }

        if (event.type === AssetDetailClickType.Tag) {
            baseComponent.selectedTag = { uid: event.uid };
        }

        if (event.type !== AssetDetailClickType.Undefined) {
            baseComponent.sidePanelOpen = true;
        }
    }
}
