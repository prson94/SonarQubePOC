import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LinkClickInterceptor {
    private subject = new Subject<AssetDetailClickEvent>();

    constructor(private router: Router) { }

    sendEvent(origEvent: any, data: any, url: string) {
        var adcEv = new AssetDetailClickEvent();
        adcEv.type = AssetDetailClickType.Undefined;
        if (origEvent) {
            origEvent.preventDefault();
            origEvent.stopPropagation();

            console.log(origEvent);
            var adcEv = new AssetDetailClickEvent();

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
                }
            }

            if (data.DataType === "Lookup"
                || data.FieldName === "ReferenceList"
                || data.DataType === "color"
            ) {
                //list fields
                var val = data.Values[0];
                adcEv.event = origEvent;
                adcEv.type = data.FieldName !== "ReferenceList" ? AssetDetailClickType.Asset : AssetDetailClickType.ReferenceItem;
                adcEv.data = data;

                adcEv.objectId = val.TooltipID;
                adcEv.objectType = val.TooltipType;
                adcEv.uid = val.uid;
                adcEv.assetTypeUid = val.assetTypeUid;
            }

            if (data.ResourceID) {
                //call from members section of groups
                adcEv.type = AssetDetailClickType.User;
                adcEv.objectType = "Resource";
                adcEv.uid = data.uid;
            }

            if (data.column && data.column.uidfield && data.column.uidfield.indexOf("_Uid") > 0) {
                //clicked on preview column on relation lookup
                adcEv.type = AssetDetailClickType.Asset;
                adcEv.objectType = "Artifact";
                adcEv.uid = data.item[data.column.uidfield];
            }

        } else {
            adcEv = null;
        }
        this.subject.next(adcEv);
    }

    getEvents(): Observable<AssetDetailClickEvent> {
        return this.subject.asObservable();
    }
}

export enum AssetDetailClickType {
    Undefined = 'Undefined',
    Asset = 'Asset',
    ReferenceItem = 'ReferenceItem',
    User = 'User',
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
}
