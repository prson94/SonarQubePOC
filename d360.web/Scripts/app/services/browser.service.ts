import { Injectable } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';

import {
    AssetBrowserAlert,
    AssetBrowserAlertRequest,
    AssetBrowserApiHopAssetRequestModel,
    AssetBrowserApiHopDirection,
    AssetBrowserLineageRequest,
    AssetBrowserOwnersModel,
    AssetBrowserResponseModel,
    AssetBrowserTranslationLink,
    AssetBrowserTranslationNode,
    DiagramTypesModel,
    FilterAncestryMode,
    FilterDescendancyMode,
    FilterSelectionsModel,
    StoredAssetBrowserFilterModel
} from '../models/lineage.model';

import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { IconService } from './icon.service';
import { AssetTypeClass } from '../models/asset.model';
import { IconProperties } from '../models/icon-properties.model';
import { ApiResult } from '../models/apiresult.model';

@Injectable({
    providedIn: 'root'
})
export class BrowserService extends BaseObservableService {
    private iconProperties: IconProperties[] = [];

    constructor(
        private http: HttpClient,
        private iconService: IconService,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
        this.iconService.getIconProperties().subscribe((data) => {
            this.iconProperties = data;
        });
    }

	private processResponse(response: AssetBrowserResponseModel, forceFromSideValue: string = null) {
        response.nodes.forEach((n) => {
            n.nonHiddenTemplate = n.template;
            if (n.class.toString() === 'Diagram') {
                n.class = AssetTypeClass.DiagramAsset;
            }
            n.class = AssetTypeClass[n.class] as any;
            n.icon = this.getIconUnicode(n.icon, n.class);
            n.isGroup = !n.leaf;
		});

		if (forceFromSideValue) {
			//when loading impact/hops, diagram arrows should always point of out
			//if our current hierarchy key is on other side of relationship (object) we need to switch relationships and predicated to other side 
			//to render impact diagram correctly
			let temp = '';
			if (response.links) {
				response.links.forEach((relationship) => {
					if (relationship.from.toLowerCase() !== forceFromSideValue.toLowerCase()) {
						temp = relationship.from;
						relationship.from = relationship.to;
						relationship.to = temp;
						relationship.isReversed = true;

						relationship.links.forEach((link) => {
							temp = link.from;
							link.from = link.to;
							link.to = temp;
						});
					}
				});
			}
		}

		response.highlightLinks = [];
		if (response.links && response.links.length > 0) {
			response.highlightLinks = JSON.parse(JSON.stringify(response.links));

			response.highlightLinks.forEach((l) => {
				//relationship id should be updated from format typeid|relationshipid|assetid
				//when expanding relationship, from or to part hold relationship id value from previously expanded relationship
				//we need to update new relationships data to hold correct id's
				var fromRelIdx = l.from.split('|')[1];
				var toRelIdx = l.to.split('|')[1];
				l.links.forEach((link) => {
					var partsFrom = link.from.split('|');
					var partsTo = link.to.split('|');

					partsFrom[1] = fromRelIdx;
					partsTo[1] = toRelIdx;

					link.from = partsFrom.join('|');
					link.to = partsTo.join('|');
				});
			});
		}

        //#region Load root data from hierarchy array

        response.hierarchy.forEach((h) => {
            try {
                let rootNode = response.nodes.find((n) => { return n.hierarchyKey === h.hierarchyKey && !n.group; });
                if (rootNode) {
                    rootNode.predictableId = h.predictableId;
                    rootNode.owners = h.owners;
                    rootNode.relations = h.relations;
                    if (h.backwardReveal !== AssetBrowserApiHopDirection.None) {
                        rootNode.showReveal = AssetBrowserApiHopDirection[h.backwardReveal] as any;
                    }
                    else {
                        rootNode.showReveal = AssetBrowserApiHopDirection[h.forwardReveal] as any;
                    }

                    // Handle initial expanded logic.
                    rootNode.relations.forEach((r, rix) => {
                        let ix = response.links.findIndex((l) => { return l.predicateId === r.predicateId && l.from === rootNode.key && l.text === r.predicate; });
                        if (ix > -1) {
                            r.expanded = true;
                            response.links[ix].badgeIdentifier = rootNode.hierarchyKey + '|' + rix;
                        }
                    });
                }
            } catch (e) {
                console.log(e);
            }
        });

        //#endregion
    }

    private processOwnerResponse(hierarchyKey: string,
        badgeIndex: number,
        responsibilityTypeId: number,
        responsibilityTypeName: string,
        response: AssetBrowserOwnersModel
    ): AssetBrowserResponseModel {

        let newResponse = new AssetBrowserResponseModel();
        newResponse.hierarchy = [];
        newResponse.nodes = [];
        newResponse.links = [];
        newResponse.reveals = null;

        let rootKey = hierarchyKey + '|O|' + badgeIndex;

        newResponse.hierarchy.push({
            hierarchyKey: rootKey,
            backwardReveal: AssetBrowserApiHopDirection.None,
            forwardReveal: AssetBrowserApiHopDirection.None,
            owners: [],
            relations: [],
            predictableId: null
        });

        let rootLink: AssetBrowserTranslationLink = {
            from: hierarchyKey,
            to: rootKey,
            text: "",
            back: "#cccccc",
            predicateId: null,
            predicateIds: [],
            predicateType: null,
            predicateUid: null,
            responsibilityTypeId,
            links: [],
			badgeIdentifier: rootKey,
			isReversed: false
        };

        response.ownerRelations.forEach((l) => {
            rootLink.links.push({
                id: 0,
                from: l.assetKey,
                to: l.ownerKey
            });
        });

        newResponse.links.push(rootLink);

        // Add root node.
        let rootNode = new AssetBrowserTranslationNode();
        rootNode.hierarchyKey = rootKey;
        rootNode.key = rootKey;
        rootNode.text = responsibilityTypeName;
        rootNode.hop = 0;
        rootNode.assetUid = "";
        rootNode.focal = false;
        rootNode.actionCount = 0;
        rootNode.assetTypeId = 0;
        rootNode.backAmount = 0;
        rootNode.showIcon = true;
        rootNode.back = "#ffffcc";
        rootNode.hasAssetReadAccess = true;
        rootNode.icon = this.getIconUnicode("fa-users", AssetTypeClass.BusinessAsset);
        rootNode.leaf = false;
        rootNode.isGroup = true;
        rootNode.responsibilityTypeId = responsibilityTypeId;
        rootNode.template = "Owners";
        rootNode.nonHiddenTemplate = "Owners";
        newResponse.nodes.push(rootNode);

        response.owners.forEach((o) => {
            let n = new AssetBrowserTranslationNode();
            n.hierarchyKey = rootKey;
            n.key = o.key;
            n.group = rootKey;
            n.text = o.displayValue;
            n.hop = 0;
            n.assetUid = o.resourceUid;
            n.focal = false;
            n.actionCount = 0;
            n.assetTypeId = 0;
            n.backAmount = 0;
            n.showIcon = true;
            n.back = o.backColor;
            n.hasAssetReadAccess = true;
            n.icon = this.getIconUnicode(o.icon, AssetTypeClass.BusinessAsset);
            n.leaf = true;
            n.isGroup = false;
            n.responsibilityTypeId = responsibilityTypeId;
            n.template = "Owner";
            n.nonHiddenTemplate = "Owner";
            newResponse.nodes.push(n);
        });

        //#endregion

        return newResponse;
    }

    public getInitialLineage(ancestry: FilterAncestryMode, uid: string, numberOfHops: number, includeNonLeaf: boolean, descendancy: FilterDescendancyMode): Observable<AssetBrowserResponseModel> {
        const url = `api/v2/browser/lineage/initial`;
        if (numberOfHops <= 0 || numberOfHops > 5)
            {numberOfHops = 3;}

        return this.http.post(url, {
            ancestry: +ancestry,
            uid,
            hopCount: numberOfHops,
            includeNonLeaf,
            descendancy
        }).pipe(
            map((response: AssetBrowserResponseModel) => {
                this.processResponse(response);
                return response;
            }),
            catchError((err) => this.handleError(err))
        );
    }

    public getInitialImpact(uid: string, numberOfHops: number): Observable<AssetBrowserResponseModel> {
        const url = `api/v2/browser/impact/initial`;
        if (numberOfHops <= 0 || numberOfHops > 5)
            {numberOfHops = 3;}

        return this.http.post(url, {
            uid,
            hopCount: numberOfHops
        }).pipe(
			map((response: AssetBrowserResponseModel) => {
				let hierarchyKey = response.nodes.find((x) => x.assetUid == uid)?.hierarchyKey;

				this.processResponse(response, hierarchyKey);
                return response;
            }),
            catchError((err) => this.handleError(err))
        );
    }

    public getImpactHop(hierarchyKey: string, predicateUid: string, direction: AssetBrowserApiHopDirection, includeHierarchyBadges: boolean, assets: AssetBrowserApiHopAssetRequestModel[], intersects: number[]): Observable<AssetBrowserResponseModel> {
        const url = `api/v2/browser/impact/hop`;

		let initialSaltValue: number = -1;
		if (hierarchyKey && hierarchyKey.split('|').length === 3) {
			initialSaltValue = +hierarchyKey.split('|')[1];
		}

        return this.http.post(url, {
            assets,
            direction,
            hierarchyKey,
            includeHierarchyBadges,
            intersects,
			predicateUid,
			initialSaltValue
        }).pipe(
            map((response: AssetBrowserResponseModel) => {
				this.processResponse(response, hierarchyKey);
                return response;
            }),
            catchError((err) => this.handleError(err))
        );
    }

    public getLineageHop(model: AssetBrowserLineageRequest): Observable<AssetBrowserResponseModel> {
        const url = `api/v2/browser/lineage/hop`;

        return this.http.post(url, model).pipe(
            map((response: AssetBrowserResponseModel) => {
                this.processResponse(response);
                return response;
            }),
            catchError((err) => this.handleError(err))
        );
    }

    /**
    * Retrieve results from the Govern API for owners regarding specific assets.
    * @returns A deep model of owners.
    */
    public getOwnerHop(hierarchyKey: string, badgeIndex: number, responsibilityTypeId: number, responsibilityTypeName: string, assets: AssetBrowserApiHopAssetRequestModel[]): Observable<AssetBrowserResponseModel> {
        const url = `api/v2/browser/ownership/hop`;

        return this.http.post(url,
            {
                assets,
                hierarchyKey,
                responsibilityTypeId
            }).pipe(
            map((response: AssetBrowserOwnersModel) => {
                return this.processOwnerResponse(hierarchyKey, badgeIndex, responsibilityTypeId, responsibilityTypeName, response);
            }),
            catchError((err) => this.handleError(err))
        );
    }

    /**
    * Retrieve results from the Govern API for lineage regarding a specific asset.
    * @returns A deep models with hierarchical assets and relationships between them.
    */
    public getAlertsByAsset(
        model: AssetBrowserAlertRequest

    ): Observable<AssetBrowserAlert[]> {
        const url = `api/v2/actions/alerts`;

        return this.http.post(url, model).pipe(
            map((response) => response),
            catchError((err) => this.handleError(err))
        );
    }

    /**
    * Retrieves a set of options to filter by within the Asset Browser, for use in the filter panel.
    * @returns A set of filter options, as list properties.
    */
    public getFilterOptions(): Observable<FilterSelectionsModel> {
        const url = `api/v2/browser/filters`;

        return this.http.get(url).pipe(
            map((response: FilterSelectionsModel) => new FilterSelectionsModel(response.AssetTypeOptions, response.PredicateOptions, response.ResponsibilityTypeOptions)),
            catchError((err) => this.handleError(err))
        );
    }

    /**
    * Retrieves a set of options to filter by within the Asset Browser, for use in the filter panel.
    * @returns A set of filter options, as list properties.
    */
    public getUserFilters(): Observable<StoredAssetBrowserFilterModel[]> {
        const url = `api/v2/browser/filters/me`;

        return this.http.get(url).pipe(
            map((response: StoredAssetBrowserFilterModel[]) => response),
            catchError((err) => this.handleError(err))
        );
    }

    public saveUserFilter(model: StoredAssetBrowserFilterModel): Observable<StoredAssetBrowserFilterModel> {
        const url = `api/v2/browser/filters`;

        if (model.uid != null)
            {return this.http.put(url + '/' + model.uid, model).pipe(
                map((response: StoredAssetBrowserFilterModel) => response),
                catchError((err) => this.handleError(err))
            );}
        else
            {return this.http.post(url, model).pipe(
                map((response: StoredAssetBrowserFilterModel) => response),
                catchError((err) => this.handleError(err))
            );}
    }

    public deleteUserFilter(model: StoredAssetBrowserFilterModel): Observable<boolean> {
        const url = `api/v2/browser/filters`;

        return this.http.delete(url + '/' + model.uid).pipe(
            map((response: ApiResult) => response.Success),
            catchError((err) => this.handleError(err))
        );
    }

    public getDiagramTypes(uid: string): Observable<DiagramTypesModel> {
        return this.http.get(`api/v2/browser/types/${uid}/me`).pipe(
            map((response: DiagramTypesModel) => response),
            catchError((err) => this.handleError(err)));
    }

    /**
     * Accepts an icon string and asset type class
     * @returns The unicode value of the specified icon, the class default, or null if no match is found
     */
    private getIconUnicode(icon, assetClass): string {
        let id = this.iconService.removeIconPrefix(icon);

        if (icon == null || icon.length === 0) {
            if (assetClass == null)
                {return null;}
            id = this.iconService.getIconIdByClass(assetClass);
        }

        if (id != null) {
            let iconProperties = this.iconProperties.find((d) => d.id === id);
            if (iconProperties != null) {
                return String.fromCharCode(parseInt(iconProperties.unicodeValue, 16));
            }
        }
        return null;
    }
}
