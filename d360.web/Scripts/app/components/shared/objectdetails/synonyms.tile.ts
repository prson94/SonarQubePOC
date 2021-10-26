import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { RelationshipsService } from '../../../services/relationships.service';
import { RelationshipV2 } from '../../../models/relationship.model';
import { Synonym, SynonymItem } from '../../../models/object-detail.model';
import { FormMode } from '../../../models/form.model';
import { BaseComponent } from '../base.component';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { SynonymPermission } from '../../../models/artifacts.model';
import { CompanySettingsService } from '../../../services/settings.service';


@Component({
    selector: 'd3s-synonyms-tile',
    styles: [
            `
            p-autoComplete > span > input {
                width: 100%;
            }
        `],
    templateUrl: './synonyms.tile.html',
    providers: [ObjectDetailService, RelationshipsService],
})

export class SynonymsTile extends BaseComponent implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() assetUid: string;
    @Input() readonly: boolean = true;
    @Input() predicateId: number;
    @Output() itemCount: number = 0;
    @Input() predicateName: string;

    @Input() hasAdd: boolean = true;
    @Input() hasDelete: boolean = true;

    @Input() synonymPermission: SynonymPermission;

    theDeleteCallback: Function;

    protected formMode = FormMode.Default;
    protected FormMode = FormMode;
    protected items: Synonym[] = [];
    protected selectedItem;
    protected synonymTypes = [];    
    protected selectedType: string = '';
    protected synonymItems = [];
    protected selectedSynonym: SynonymItem;    
    protected customSynonymName: string = '';
    protected isLoadingItems = false;

    constructor(
        private messagesService: MessagesObservableService,
        private objectDetailService: ObjectDetailService,
        private relationshipsService: RelationshipsService,
        protected settingsService: CompanySettingsService,
        private router: Router) {
        super(settingsService);

        this.theDeleteCallback = this.deleteSynonym.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.load();
     }

    load(): void {
        if (this.objectType == null || this.objectID == null) {
            return;
        }

        this.isLoading = true;

        this.objectDetailService.getObjectSynonyms(this.objectID, this.objectType, this.predicateId).subscribe(
            d => {
                this.items = d;
                this.itemCount = this.items.length;

                this.isLoading = false;
            }
        );

        this.isLoading = true;
    }

    protected deleteSynonym(item: Synonym) {
        this.isLoading = true;

        // custom synonyms are not objects dont have intersects and have to be deleted old way for now
        if (item.IntersectUid != null) {
            this.relationshipsService.deleteSingleRelationshipV2(item.IntersectTypeUid, item.IntersectUid).subscribe(
                res => {
                    this.isLoading = false;

                    this.showMessageForApiResults(this.messagesService, res,`${this.predicateName} successfully deleted.`);
                    
                    this.items = this.items.filter(x => x.IntersectID != item.IntersectID);
                    this.itemCount = this.items.length;
                    this.formMode = FormMode.Default;
                }
            )
        }
        else {
            this.objectDetailService.deleteCustomSynonym(item).subscribe(
                res => {
                    this.isLoading = false;

                    this.showMessageForResult(this.messagesService, res);

                    this.items = this.items.filter(x => x.CustomID != item.CustomID);                    
                    this.itemCount = this.items.length;
                    this.formMode = FormMode.Default;
                }
            );
        }
    }

    protected add() {
        this.selectedSynonym = null;

        //if we havent loaded synonym types already do so now
        if (this.synonymTypes.length == 0) {
            this.objectDetailService.getSynonymTypes(this.objectID, this.objectType, this.predicateId).subscribe(
                d => {
                    this.synonymTypes = d;
                    this.formMode = FormMode.Adding;
                }
            );
        } else {
            this.formMode = FormMode.Adding;
        }
    }

    protected delete() {
        this.formMode = FormMode.Deleting;
    }

    protected save() {
        this.isLoading = true;

        if (this.selectedSynonym && this.selectedSynonym.uid) {            
            let type = this.synonymTypes.find(t => t.Value == this.selectedType);
            let relationships: Array<RelationshipV2> = [];
            let relationship = new RelationshipV2();

            if (type.IntersectTypeIsSubjectSide) {                
                relationship.SubjectAssetUid = this.assetUid;
                relationship.ObjectAssetUid = this.selectedSynonym.uid;                
            }
            else {                
                relationship.SubjectAssetUid = this.selectedSynonym.uid;
                relationship.ObjectAssetUid = this.assetUid;                
            }

            relationships.push(relationship);
            
            this.relationshipsService.saveRelationships(type.uid, relationships).subscribe(
                res => {
                    this.showMessageForApiResults(this.messagesService, res, `${this.predicateName} successfully saved.`,true);
                    this.formMode = FormMode.Default;
                    this.load();
                }
            );

        } else if (this.customSynonymName) {
            this.objectDetailService.postCustomSynonym(this.customSynonymName, this.predicateId, this.objectType, this.objectID).subscribe(
                d => {
                    this.showMessageForResult(this.messagesService, d);
                    this.customSynonymName = '';
                    this.formMode = FormMode.Default;
                    this.load();
                }
            );
        }
    }

    protected navigate(url: string) {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url));
    }

    protected search(e: any) {
        this.isLoadingItems = true;

        let type = this.synonymTypes.find(t => t.Value == this.selectedType);

        if (!type) {
            this.isLoadingItems = false;
            return;
        }
        
        this.objectDetailService.getSynonymOptions(this.predicateId, type.Object, type.ObjectID, this.objectType, this.objectID, (e.query || '')).subscribe(
            r => {
                this.synonymItems = r;

                this.isLoadingItems = false;
            }
        );
    }

    protected clearSearch() {
        this.synonymItems = [];
        this.selectedSynonym = null;
    }
}