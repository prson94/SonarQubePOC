import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {
    catchError,
    distinctUntilChanged,
    map,
    switchMap
} from 'rxjs/operators';
import {Observable} from 'rxjs';

import { AssetBrowserLineageApiRequestModel, AssetBrowserLineageApiResponseModel, AssetBrowserTranslation, AssetBrowserTranslationNode, AssetBrowserLineageApiItemModel, AssetBrowserTranslationLink, AssetBrowserLineageApiRelationshipModel, AssetBrowserDiagramAsset, AssetBrowserTranslationRelationCount, AssetBrowserDirection, AssetBrowserImpactApiRequestModel } from '../models/lineage.model';

import {MessagesObservableService} from './messages-observable.service';

import {BaseObservableService} from "./baseObservable.service";

@Injectable()
export class BrowserService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    /**
    * Retrieve a set of static test data to test the various features of the asset browser, without needing to connect to the API.
    * @returns A diagram-specific representation for the nodes and links.
    */
    public getStaticDataForTesting(): AssetBrowserTranslation {
        let translationModel: any = new AssetBrowserTranslation();
        translationModel.nodes = new Array();
        translationModel.links = new Array();

        let color: string = "#B9F1AF";
        let transformColor: string = "#FAE7BC";
        let sysColor: string = "#DAAADB";
        let btColor: string = "#E0EAF7";

        translationModel.nodes.push({ assetUid: "", key: "btType1", isGroup: true, group: undefined, text: "Business Terms", template: "PortGroup", back: btColor, icon: "\uf02d", subgraph: null, showReveal: false });
        translationModel.nodes.push({ assetUid: "", key: "bt1", isGroup: false, group: "btType1", text: "Member Name", template: undefined, back: this.shadeColor(btColor, 15), icon: "\uf02d", subgraph: null, showReveal: false });

        translationModel.nodes.push({ assetUid: "", key: "sys1", isGroup: true, group: undefined, text: "Enrollment System", template: "PortGroup", back: sysColor, icon: "\uf233", subgraph: null, showReveal: false });
        translationModel.nodes.push({ assetUid: "", key: "sysTerm1", isGroup: false, group: "sys1", text: "Member Name", template: undefined, back: this.shadeColor(sysColor, 15), icon: "\uf02d", subgraph: null, showReveal: false });

        translationModel.nodes.push({ assetUid: "", key: "sys2", isGroup: true, group: undefined, text: "Claims Adjudication", template: "PortGroup", back: sysColor, icon: "\uf233", subgraph: null, showReveal: false });
        translationModel.nodes.push({ assetUid: "", key: "sysTerm2", isGroup: false, group: "sys2", text: "Member Name", template: undefined, back: this.shadeColor(sysColor, 15), icon: "\uf02d", subgraph: null, showReveal: false });

        translationModel.nodes.push({ assetUid: "", key: "tran1", isGroup: true, group: undefined, text: "BosEtlServer", template: "PortGroup", back: transformColor, icon: "\uf085", subgraph: null, showReveal: false });
        translationModel.nodes.push({ assetUid: "", key: "job1", isGroup: true, group: "tran1", text: "ETL_MEMBER_TO_CLAIM", template: "Group", back: this.shadeColor(transformColor, 15), icon: "\uf542", subgraph: null, showReveal: false });
        translationModel.nodes.push({ assetUid: "", key: "jobStep1", isGroup: false, group: "job1", text: "LOAD_MEMBER_NAME", template: undefined, back: this.shadeColor(transformColor, 30), icon: "\uf085", subgraph: null, showReveal: false });

        translationModel.nodes.push({ assetUid: "", key: "h1", isGroup: true, group: undefined, text: "DWH", template: "PortGroup", back: color, icon: "\uf1c0", subgraph: null, showReveal: false });
        translationModel.nodes.push({ assetUid: "", key: "s1", isGroup: true, group: "h1", text: "fact", template: "Group", back: this.shadeColor(color, 15), icon: "\uf007", subgraph: null, showReveal: false });
        translationModel.nodes.push({ assetUid: "", key: "t1", isGroup: true, group: "s1", text: "MEMBERS", template: "Group", back: this.shadeColor(color, 30), icon: "\uf0ce", subgraph: null, showReveal: false });
        translationModel.nodes.push({ assetUid: "", key: "c1_1", isGroup: false, group: "t1", text: "FIRST_NAME", template: undefined, back: this.shadeColor(color, 45), icon: "\uf0db", subgraph: null, showReveal: false });
        translationModel.nodes.push({ assetUid: "", key: "c1_2", isGroup: false, group: "t1", text: "LAST_NAME", template: undefined, back: this.shadeColor(color, 45), icon: "\uf0db", subgraph: null, showReveal: false });

        translationModel.nodes.push({ assetUid: "", key: "h2", isGroup: true, group: undefined, text: "EGL", template: "PortGroup", back: color, icon: "\uf1c0", subgraph: null, showReveal: false });
        translationModel.nodes.push({ assetUid: "", key: "s2", isGroup: true, group: "h2", text: "dbo", template: "Group", back: this.shadeColor(color, 15), icon: "\uf007", subgraph: null, showReveal: false });
        translationModel.nodes.push({ assetUid: "", key: "t2", isGroup: true, group: "s2", text: "MEMBERS", template: "Group", back: this.shadeColor(color, 30), icon: "\uf0ce", subgraph: null, showReveal: false });
        translationModel.nodes.push({ assetUid: "", key: "c2_1", isGroup: false, group: "t2", text: "FIRST_NAME", template: undefined, back: this.shadeColor(color, 45), icon: "\uf0db", subgraph: null, showReveal: false });
        translationModel.nodes.push({ assetUid: "", key: "c2_2", isGroup: false, group: "t2", text: "LAST_NAME", template: undefined, back: this.shadeColor(color, 45), icon: "\uf0db", subgraph: null, showReveal: false });

        translationModel.links.push({ from: "sys1", fromPort: "T", to: "btType1", toPort: "B", text: "see also", back: sysColor });
        translationModel.links.push({ from: "sys2", fromPort: "T", to: "btType1", toPort: "B", text: "see also", back: sysColor });
        translationModel.links.push({ from: "h1", fromPort: "T", to: "sys1", toPort: "B", text: "maps to", back: color });
        translationModel.links.push({ from: "h2", fromPort: "T", to: "sys2", toPort: "B", text: "maps to", back: color });
        translationModel.links.push({ from: "h1", fromPort: "R", to: "tran1", toPort: "L", text: "transformed by", back: transformColor });
        translationModel.links.push({ from: "tran1", fromPort: "R", to: "h2", toPort: "L", text: "transforms into", back: transformColor });

        translationModel.links.push({ from: "h1", fromPort: "R", to: "h1_moredata", toPort: "L", text: "", back: color });
        translationModel.links.push({ from: "h2", fromPort: "R", to: "h2_moredata", toPort: "L", text: "", back: color });

        return translationModel;
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
        model: AssetBrowserLineageApiRequestModel
    ): Observable<AssetBrowserLineageApiResponseModel> {
        const url = `api/v2/browser`;

        return this.http.post(url, model).pipe(
            map(response => response),
            catchError(err => this.handleError(err))
        );
    }

    /**
    * Retrieve results from the Govern API for lineage regarding a specific asset.
    * @returns A deep models with hierarchical assets and relationships between them.
    */
    public getAssetImpacts(
        model: AssetBrowserImpactApiRequestModel
    ): Observable<AssetBrowserLineageApiResponseModel> {
        const url = `api/v2/browser/impacts`;

        return this.http.post(url, model).pipe(
            map(response => response),
            catchError(err => this.handleError(err))
        );
    }

    /**
    * Converts a response from the Govern API into a more appropriate representation for the asset browser diagram.
    * @returns A diagram-specific representation for the nodes and links.
    */
    public translateAssetLineageResponseModel(model: AssetBrowserLineageApiResponseModel): AssetBrowserTranslation {
        let translationModel: AssetBrowserTranslation = new AssetBrowserTranslation();

        try {
            model.assets.forEach(a => {
                this.loadTranslationChildNodes(translationModel, model.intersects, a, null, a.backColor, 1);
            });
        } catch (e) {
            console.log(e);
        }

        try {
            translationModel.links = this.determineLinkRoots(translationModel, model.intersects);
        } catch (e) {
            console.log(e);
        }

        return translationModel;
    }

    /**
     * Traverses the list of raw relationships and figures out what impact the specified node has.
     * @returns An array of asset Uids
     */ 
    private analyzeSingleNodeImpact(
        currentKey: string,
        intersects: AssetBrowserLineageApiRelationshipModel[],
        allowBackward: boolean,
        allowForward: boolean
    ): string[] {

        let relevantKeys: string[] = new Array();

        //#region Get forward-facing relationships and work our way forward.
        if (allowForward) {
            try {
                let forward = intersects.filter(i => { return i.subjectKey == currentKey; });
                forward.forEach(f => {
                    if (f.objectKey && relevantKeys) {
                        relevantKeys.push(f.objectKey);
                        let impactedKeys: string[];
                        impactedKeys = this.analyzeSingleNodeImpact(f.objectKey, intersects, false, true);
                        if (!impactedKeys) {
                            impactedKeys = new Array();
                        }
                        relevantKeys = relevantKeys.concat(relevantKeys, impactedKeys);
                    }
                });
            } catch (e) {
                console.log(e);
            }
        }
        //#endregion

        //#region Get backward-facing relationships and work our way back.
        if (allowBackward) {
            try {
                let backward = intersects.filter(i => { return i.objectKey == currentKey; });
                backward.forEach(b => {
                    if (b.subjectKey && relevantKeys) {
                        relevantKeys.push(b.subjectKey);
                        let impactedKeys: string[];
                        impactedKeys = this.analyzeSingleNodeImpact(b.subjectKey, intersects, true, false);
                        if (!impactedKeys) {
                            impactedKeys = new Array();
                        }
                        relevantKeys = relevantKeys.concat(relevantKeys, impactedKeys);
                    }
                });
            } catch (e) {
                console.log(e);
            }
        }
        //#endregion

        if (!relevantKeys) {
            relevantKeys = new Array();
        }

        if (relevantKeys.length > 0) {
            relevantKeys = this.removeArrayDuplicates(relevantKeys);

            // Remove self.
            relevantKeys = relevantKeys.filter(i => { return i !== currentKey });
        }

        return relevantKeys;
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
        intersects: AssetBrowserLineageApiRelationshipModel[],
        rootKey: string,
        rootNodeUids: string[],
        currentKey: string,
        currentNodeUids: string[],
        forward: boolean
    ): AssetBrowserTranslationLink {

        let fl: AssetBrowserTranslationLink;

        let relevantIntersects = intersects.filter(x => {
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
            // Need to remove this logic. 
            fl.impacts = new Array();
            fl.impacts = fl.impacts.concat(fl.impacts, rootNodeUids);
            fl.impacts = fl.impacts.concat(fl.impacts, currentNodeUids);
            fl.to = forward ? currentKey : rootKey;
            fl.toPort = "L;"

            let linkText: string = "";
            relevantIntersects.forEach(intersect => {
                if (linkText.indexOf(intersect.predicate) == -1) {
                    linkText += ((linkText === "") ? "" : ", ") + intersect.predicate;
                }
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
        intersects: AssetBrowserLineageApiRelationshipModel[]
    ): AssetBrowserTranslationLink[] {
        let links: AssetBrowserTranslationLink[] = new Array();

        let rootNodes = translationModel.nodes.filter(n => { return n.isGroup && !n.group; });
        let ignoredRootKeys: string[] = new Array();

        rootNodes.forEach(rootNode => {

            let keys: string[] = new Array();

            // 1. Cycle through all descendants and compile list of keys.
            this.compileDescendantUidList(rootNode.key, translationModel.nodes, keys);
            keys.push(rootNode.key);

            // 2. Loop through all intersects to see if any apply.
            let forwardIntersections = intersects.filter(x => { return keys.indexOf(x.subjectKey) >= 0; });
            let backwardIntersections = intersects.filter(x => { return keys.indexOf(x.objectKey) >= 0; });

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
        intersects: AssetBrowserLineageApiRelationshipModel[],
        current: AssetBrowserLineageApiItemModel,
        parentKey: string,
        color: string,
        multiplier: number) {

        // Create the current node.
        let currentNode: AssetBrowserTranslationNode = this.createTranslationNode(current, parentKey, color, multiplier);

        //Instantiate new multiplier as we do not want to impact the parent's multiplier.
        let newMultiplier: number = multiplier + 1;

        if (current.items) {
            current.items.forEach(a => {
                // Recurse
                this.loadTranslationChildNodes(translationModel, intersects, a, current.key, color, newMultiplier);
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
        a: AssetBrowserLineageApiItemModel,
        parentKey: string,
        color: string,
        multiplier: number): AssetBrowserTranslationNode {
        let n: AssetBrowserTranslationNode = new AssetBrowserTranslationNode();

        a.relationCounts.forEach(rC => {
            let assetBrowserTranslationRelationCount: AssetBrowserTranslationRelationCount = new AssetBrowserTranslationRelationCount();
            assetBrowserTranslationRelationCount.count = rC.Count;
            assetBrowserTranslationRelationCount.direction = rC.Direction;
            assetBrowserTranslationRelationCount.predicate = rC.Predicate;
            assetBrowserTranslationRelationCount.predicateUid = rC.PredicateUid;
            n.relations.push(assetBrowserTranslationRelationCount);
        });

        n.showReveal = AssetBrowserDirection[a.reveal] as any; //convert string from API to enum value
        n.hop = a.hop;
        n.assetUid = a.assetUid;
        n.back = this.shadeColor(color, multiplier*15);
        n.icon = "\uf02d";
        n.isGroup = (a.items && a.items.length > 0);
        n.key = a.key;
        n.text = a.displayValue;
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
    * Removes duplicates from an array of type T.
    * @returns Returns a new array with a distinct list of T items.
    */
    private removeArrayDuplicates<T>(array: T[]): T[] {
        return array.filter((item, ix) => array.indexOf(item) == ix);
    }

    /**
    * Accepts an rgb color and converts it to a lighter shade based on the number provided.
    * @returns An RGB color that represents the new lighter color.
    */
    private shadeColor(col, amt): string {

        var usePound = false;
        if (col[0] == "#") {
            col = col.slice(1);
            usePound = true;
        }

        var num = parseInt(col, 16);

        var r = (num >> 16) + amt;

        if (r > 255) r = 255;
        else if (r < 0) r = 0;

        var b = ((num >> 8) & 0x00FF) + amt;

        if (b > 255) b = 255;
        else if (b < 0) b = 0;

        var g = (num & 0x0000FF) + amt;

        if (g > 255) g = 255;
        else if (g < 0) g = 0;

        return (usePound ? "#" : "") + (g | (b << 8) | (r << 16)).toString(16);
    }
}
