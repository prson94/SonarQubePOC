import { Injectable } from '@angular/core';
import { HAMMER_LOADER } from '@angular/platform-browser';
import { Subject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class HrefClickService {
    private subject = new Subject<AssetDetailClickEvent>();

    sendEvent(origEvent: any, data: any) {
        var adcEv = new AssetDetailClickEvent();
        adcEv.type = AssetDetailClickType.Undefined;
        if (origEvent) {
            origEvent.preventDefault();
            origEvent.stopPropagation();

            console.log(data);
            var adcEv = new AssetDetailClickEvent();

            if (data.column && data.column.uidfield === "SecurityAssetUid") {
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
                var val = data.Values[0];
                adcEv.event = origEvent;
                adcEv.type = data.FieldName !== "ReferenceList" ? AssetDetailClickType.Asset : AssetDetailClickType.ReferenceItem;
                adcEv.data = data;

                adcEv.objectId = val.TooltipID;
                adcEv.objectType = val.TooltipType;
                adcEv.uid = val.uid;
                adcEv.assetTypeUid = val.assetTypeUid;
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
