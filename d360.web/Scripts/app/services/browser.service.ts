import { Injectable } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import {
    catchError,
    distinctUntilChanged,
    map,
    switchMap
} from 'rxjs/operators';
import { Observable } from 'rxjs';

import {
    AssetBrowserTranslation,
    AssetBrowserTranslationNode,
    AssetBrowserTranslationLink,
    AssetBrowserDiagramAsset,
    AssetBrowserTranslationRelationCount,
    AssetBrowserApiHopDirection,    AssetBrowserApiHopType,
    FilterAncestryMode,
    FilterSelectionsModel,
    AssetBrowserApiHopRequestModel,
    AssetBrowserTranslationOwnerCount,
    AssetBrowserApiOwnerHopRequestModel,
    AssetBrowserAssetsModel,
    AssetBrowserAssetModel,
    AssetBrowserOwnersModel,
    AssetBrowserAssetRelationModel,
    AssetBrowserModel
} from '../models/lineage.model';

import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { IconService } from './icon.service';
import { AssetTypeClass } from '../models/asset.model';
import { IconProperties } from '../models/icon-properties.model';

@Injectable()
export class BrowserService extends BaseObservableService {
    private iconProperties: IconProperties[] = [];

    constructor(
        private http: HttpClient,
        private iconService: IconService,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
        this.iconService.getIconProperties().subscribe(data => {
            this.iconProperties = data;
        });
    }

    /**
    * Retrieve results from the Govern API for lineage regarding a specific asset.
    * @returns A deep models with hierarchical assets and relationships between them.
    */
    public getAssetBrowserDiagramAsset(
        uid: string
        
    ): Observable<AssetBrowserDiagramAsset> {
        const url = `api/v2/browser/diagramasset/${uid}`;

        return this.http.get(url).pipe(
            map(response => response),
            catchError(err => this.handleError(err))
        );
    }

    /**
    * Retrieve results from the Govern API for lineage regarding a specific asset.
    * @returns A deep models with hierarchical assets and relationships between them.
    */
    public getAssetLineage(
        model: AssetBrowserApiHopRequestModel
    ): Observable<AssetBrowserAssetsModel> {
        const url = `api/v2/browser`;

        return this.http.post(url, model).pipe(
            map(response => response),
            catchError(err => this.handleError(err)) 
        );

    }

    private findDirectParents(currentParent: AssetBrowserAssetModel, nodes: AssetBrowserAssetModel[], newHierarchy: AssetBrowserAssetModel[]) {
        let parent: AssetBrowserAssetModel;

        if (nodes) {
            for (let n of nodes) {
                if (n.items) {
                    parent = n;
                    this.findDirectParents(parent, n.items, newHierarchy);
                }
                else {
                    newHierarchy.push(currentParent);
                    break;
                }
            }

        }
        else {
            newHierarchy.push(currentParent);
        }
    }

    public convertResponseModel(model: AssetBrowserAssetsModel, ancestryMode: FilterAncestryMode): AssetBrowserAssetsModel {
        let convertedModel: AssetBrowserAssetsModel = model;

        switch (+ancestryMode) {
            case FilterAncestryMode.AllAncestors:
                break;
            case FilterAncestryMode.DirectAncestor:
                let assets: AssetBrowserAssetModel[] = new Array<AssetBrowserAssetModel>();
                this.findDirectParents(null, model.assets, assets);
                convertedModel.assets = assets;
                break;
        }

        return convertedModel;
    }

    /**
    * Retrieve results from the Govern API for lineage regarding a specific asset.
    * @returns A deep models with hierarchical assets and relationships between them.
    */
    public getAssetImpacts(
        model: AssetBrowserApiHopRequestModel
    ): Observable<AssetBrowserAssetsModel> {
        const url = `api/v2/browser`;

        model.HopType = AssetBrowserApiHopType.Impact; 
        return this.http.post(url, model).pipe(
            map(response => response),
            catchError(err => this.handleError(err))
        );
    }

    /**
    * Retrieve results from the Govern API for owners regarding specific assets.
    * @returns A deep model of owners.
    */
    public getAssetOwners(
        model: AssetBrowserApiOwnerHopRequestModel
    ): Observable<AssetBrowserOwnersModel> {
        const url = `api/v2/browser/owners`;
        return this.http.post(url, model).pipe(
            map(response => response),
            catchError(err => this.handleError(err))
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
            catchError(err => this.handleError(err))
        );
    }

    /**
    * Converts a response from the Govern API into a more appropriate representation for the asset browser diagram.
    * @returns A diagram-specific representation for the nodes and links.
    */
    public translateAssetsResponseModel(model: AssetBrowserAssetsModel): AssetBrowserTranslation {
        let translationModel: AssetBrowserTranslation = new AssetBrowserTranslation();

        try {
            model.assets.forEach(a => {
                this.loadTranslationChildNodes(translationModel, model.assetRelations, a, null, a.backColor, a.foreColor); // a.parentKey instead of null value in the paremeter to the left?
            });
        } catch (e) {
            console.log(e);
        }

        try {
            translationModel.links = this.determineLinkRoots(translationModel, model.assetRelations);
        } catch (e) {
            console.log(e);
        }

        return translationModel;
    }

    /**
    * Converts a response from the Govern API into a more appropriate representation for the asset browser diagram.
    * @returns A diagram-specific representation for the nodes and links.
    */
    public translateOwnersResponseModel(model: AssetBrowserOwnersModel): AssetBrowserTranslation {
        let translationModel: AssetBrowserTranslation = new AssetBrowserTranslation();

        let baseKey: string = model.fromKey + '|' + model.responsibilityTypeId;

        try {
            translationModel.nodes = new Array();

            let ownersNode: AssetBrowserTranslationNode = new AssetBrowserTranslationNode();

            ownersNode.showReveal = AssetBrowserApiHopDirection.None;
            ownersNode.hop = 0;
            //ownersNode.assetUid = a.assetUid;
            //ownersNode.assetTypeId = a.assetTypeId;
            ownersNode.class = AssetTypeClass.Organization; //convert string from API to enum value
            ownersNode.back = "#FFE5D0";
            ownersNode.backAmount = 0;
            ownersNode.fore = "#ffffff";
            ownersNode.foreAmount = 0;
            ownersNode.icon = this.getIconUnicode('fa-user', AssetTypeClass.BusinessAsset);
            ownersNode.isGroup = true;
            ownersNode.key = baseKey;
            ownersNode.text = model.responsibilityType;
            ownersNode.template = "Owners";
            ownersNode.responsibilityTypeId = model.responsibilityTypeId;
            translationModel.nodes.push(ownersNode); 

            model.owners.forEach(a => {
                let ownerNode: AssetBrowserTranslationNode = new AssetBrowserTranslationNode();

                ownerNode.showReveal = AssetBrowserApiHopDirection.None;
                ownerNode.hop = 0;
                ownerNode.assetUid = a.resourceUid;
                //ownerNode.assetTypeId = a.assetTypeId;
                ownerNode.class = AssetTypeClass.BusinessAsset; //convert string from API to enum value
                ownerNode.back = "#000000";
                ownerNode.backAmount = 0;
                ownerNode.fore = "#ffffff";
                ownerNode.foreAmount = 0;
                ownerNode.icon = this.getIconUnicode(a.icon, AssetTypeClass.BusinessAsset);
                ownerNode.isGroup = false; 
                ownerNode.group = baseKey; 
                ownerNode.key = a.key;
                ownerNode.template = "Owner";
                ownerNode.text = a.displayValue;
                translationModel.nodes.push(ownerNode);
            });
        } catch (e) {
            console.log(e);
        }

        try {
            let link = new AssetBrowserTranslationLink();

            link.back = "#cccccc";
            link.from = baseKey;
            link.fromPort = "R";
            link.to = model.fromKey;
            link.toPort = "L;"

            translationModel.links = new Array();
            translationModel.links.push(link);
        } catch (e) {
            console.log(e);
        }

        return translationModel;
    }

    /**
    * Walks the nodes to find the hierarchy of the specified parent key, building a list of keys along the way.
    * @returns An array of nodes keys in a hierarchy.
    */
    private compileDescendantUidList( 
        parentKey: string,
        nodes: AssetBrowserTranslationNode[],
        keys: string[]
    )//: string[]
    {

        //let keys: string[] = new Array();

        let childNodes = nodes.filter(n => { return n.group == parentKey; });
        childNodes.forEach(n => {
            this.compileDescendantUidList(n.key, nodes, keys);
            keys.push(n.key);
        });

        //return keys;
    }

    /**
    * Accepts impacted relationships for the root node, and details about the root node and comparison node, then
    * makes a determination as to whether these nodes need to have a link associated between them.
    * @returns A diagram link, if one is required.
    */
    private buildLinkRoot(
        assetRelations: AssetBrowserAssetRelationModel[],
        rootKey: string,
        rootNodeUids: string[],
        currentKey: string,
        currentNodeUids: string[],
        forward: boolean
    ): AssetBrowserTranslationLink {

        let fl: AssetBrowserTranslationLink;

        let relevantIntersects = assetRelations.filter(x => {
            return (forward) ?
                (rootNodeUids.indexOf(x.subjectKey) >= 0) :
                (rootNodeUids.indexOf(x.objectKey) >= 0);
        });

        relevantIntersects = relevantIntersects.filter(x => {
            return (forward) ?
                (currentNodeUids.indexOf(x.objectKey) >= 0) :
                (currentNodeUids.indexOf(x.subjectKey) >= 0);
        });

        if (relevantIntersects.length > 0) {
            fl = new AssetBrowserTranslationLink();

            fl.back = "#cccccc";
            fl.from = forward ? rootKey : currentKey;
            fl.fromPort = "R";
            fl.to = forward ? currentKey : rootKey;
            fl.toPort = "L;"

            let linkText: string = "";
            relevantIntersects.forEach(intersect => {
                if (linkText.indexOf(intersect.predicate) == -1) {
                    linkText += ((linkText === "") ? "" : ", ") + intersect.predicate;
                }
                if (!fl.predicateIds) {
                    fl.predicateIds = [];
                }
                fl.predicateIds.push(intersect.predicateId);
            });
            fl.text = linkText;

        }

        return fl;
    }

    /**
    * Traverses data and determines the links to draw on root diagram nodes, as determined 
    * by relationships within descendant nodes.
    * @returns An array of links to add to a diagram.
    */
    private determineLinkRoots(
        translationModel: AssetBrowserTranslation,
        assetRelations: AssetBrowserAssetRelationModel[]
    ): AssetBrowserTranslationLink[] {
        let links: AssetBrowserTranslationLink[] = new Array();

        let rootNodes = translationModel.nodes.filter(n => { return n.isGroup && !n.group; });
        let ignoredRootKeys: string[] = new Array();

        rootNodes.forEach(rootNode => {

            let keys: string[] = new Array();

            // 1. Cycle through all descendants and compile list of keys.
            this.compileDescendantUidList(rootNode.key, translationModel.nodes, keys);
            keys.push(rootNode.key);

            // 2. Loop through all assetRelations to see if any apply.
            let forwardIntersections = assetRelations.filter(x => { return keys.indexOf(x.subjectKey) >= 0; });
            let backwardIntersections = assetRelations.filter(x => { return keys.indexOf(x.objectKey) >= 0; });

            // You can ignore this node in loop below.
            ignoredRootKeys.push(rootNode.key);

            rootNodes
                .filter(nextRootNode => { return ignoredRootKeys.indexOf(nextRootNode.key) == -1; })
                .forEach(nextRootNode => {

                    let theseNodeKeys: string[] = new Array();
                    this.compileDescendantUidList(nextRootNode.key, translationModel.nodes, theseNodeKeys);
                    theseNodeKeys.push(nextRootNode.key);

                    let fl = this.buildLinkRoot(forwardIntersections, rootNode.key, keys, nextRootNode.key, theseNodeKeys, true);
                    if (fl) {
                        if (links.findIndex(l => { return l.from == fl.from && l.to == fl.to; }) == -1) {
                            links.push(fl);
                        }
                    }

                    let bl = this.buildLinkRoot(backwardIntersections, rootNode.key, keys, nextRootNode.key, theseNodeKeys, false);
                    if (bl) {
                        if (links.findIndex(l => { return l.from == bl.from && l.to == bl.to; }) == -1) {
                            links.push(bl);
                        }
                    }
            });
        });

        return links;
    }

    /**
    * Recurses through a node hierarchy received from the Govern API, then sends a list of impacted keys up the 
    * hierarchy in order to properly populate the impact property string collection on each ancestor node.
    * @returns A string of impacted keys. This impacted keys are used for node highlighting for impact paths.
    */
    private loadTranslationChildNodes(
        translationModel: AssetBrowserTranslation,
        assetRelations: AssetBrowserAssetRelationModel[],
        current: AssetBrowserAssetModel,
        parentKey: string,
        backColor: string,
        foreColor: string) {

        // Create the current node.
        let currentNode: AssetBrowserTranslationNode = this.createTranslationNode(current, parentKey, backColor, foreColor);

        if (current.items) {
            current.items.forEach(a => {
                // Recurse
                this.loadTranslationChildNodes(translationModel, assetRelations, a, current.key, backColor, foreColor);
            });
        }

        // Add the current node, after everything is calculated, including impact collection.
        translationModel.nodes.push(currentNode);

    }

    /**
    * Creates a diagram node based on the data from the API, as well as the base color and shading ratio (whole number) that should be applied.
    * @returns A diagram node.
    */
    private createTranslationNode(
        a: AssetBrowserAssetModel,
        parentKey: string,
        backColor: string,
        foreColor: string): AssetBrowserTranslationNode {
        let n: AssetBrowserTranslationNode = new AssetBrowserTranslationNode();

        a.ownerCounts.forEach(oC => {
            let assetBrowserTranslationOwnerCount: AssetBrowserTranslationOwnerCount = new AssetBrowserTranslationOwnerCount();
            assetBrowserTranslationOwnerCount.key = a.key + '-O-' + oC.ResponsibilityTypeID.toString();
            assetBrowserTranslationOwnerCount.expanded = false;
            assetBrowserTranslationOwnerCount.count = oC.Count;
            assetBrowserTranslationOwnerCount.responsibilityType = oC.ResponsibilityType;
            assetBrowserTranslationOwnerCount.responsibilityTypeId = oC.ResponsibilityTypeID;
            n.owners.push(assetBrowserTranslationOwnerCount);
        }); 

        a.relationCounts.forEach(rC => {
            let assetBrowserTranslationRelationCount: AssetBrowserTranslationRelationCount = new AssetBrowserTranslationRelationCount();
            assetBrowserTranslationRelationCount.key = a.key + '-R-' + a.reveal + '-' + rC.PredicateID.toString();
            assetBrowserTranslationRelationCount.expanded = false;
            assetBrowserTranslationRelationCount.count = rC.Count;
            assetBrowserTranslationRelationCount.direction = rC.Direction;
            assetBrowserTranslationRelationCount.predicate = rC.Predicate;
            assetBrowserTranslationRelationCount.predicateId = rC.PredicateID;
            assetBrowserTranslationRelationCount.predicateUid = rC.PredicateUid;
            n.relations.push(assetBrowserTranslationRelationCount);
        });

        n.showReveal = AssetBrowserApiHopDirection[a.reveal] as any; //convert string from API to enum value
        n.hop = a.hop;
        n.assetUid = a.assetUid;
        n.assetTypeId = a.assetTypeId;
        n.class = AssetTypeClass[a.class] as any; //convert string from API to enum value
        n.back = backColor;
        n.backAmount = a.backAmount;
        n.fore = (foreColor) ? foreColor : "#404040";
        n.foreAmount = a.foreAmount;
        n.icon = this.getIconUnicode(a.icon, n.class);
        n.isGroup = (a.items && a.items.length > 0);
        n.key = a.key;
        n.text = a.displayValue;
        n.hasAssetReadAccess = a.hasAssetReadAccess;
        
        if (parentKey && parentKey !== "") {
            n.group = parentKey;

            if (n.isGroup) {
                n.template = "Group";
            } 
        }
        else {
            n.template = "PortGroup";
        }

        return n;
    }

    /**
     * Accepts an icon string and asset type class
     * @returns The unicode value of the specified icon, the class default, or null if no match is found
     */
    private getIconUnicode(icon, assetClass): string {
        let id = this.iconService.removeIconPrefix(icon);

        if (icon == null || icon.length == 0) {
            if (assetClass == null)
                return null;
            id = this.iconService.getIconIdByClass(assetClass);
        }

        if (id != null) {
            let iconProperties = this.iconProperties.find(d => d.id == id);
            if (iconProperties != null) {
                return String.fromCharCode(parseInt(iconProperties.unicodeValue, 16));
            }
        }
        return null;
    }
}
