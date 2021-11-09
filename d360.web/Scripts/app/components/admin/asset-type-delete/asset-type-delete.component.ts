import { Input, Output, Component, EventEmitter, OnInit } from "@angular/core";
import { ArtifactTypeService } from "../../../services/artifact-type.service";
import { ArtifactService } from "../../../services/artifacts.service";
import { BaseComponent } from "../../shared/base.component";
import { AssetTypeClass } from "../../../models/asset.model";
import { ActivatedRoute } from "@angular/router";
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: "d3s-asset-type-delete",
    templateUrl: "./asset-type-delete.component.html",
    providers: [ArtifactTypeService, ArtifactService]
})

export class AssetTypeDeleteComponent extends BaseComponent implements OnInit {
    @Input() callback: Function;
    @Input() artifactTypeId: number;
    @Input() artifactTypeUid: string;
    @Input() assetTypeId: number;
    @Input() assetTypeClass: AssetTypeClass;
    @Input() assetTypeName: string = "Unknown";
    @Input() count: number = 0;
    @Output() onCancel = new EventEmitter();

    signoff: boolean = false;
    className: string;
    private sub: any;

    constructor(
        protected settingsService: CompanySettingsService,
        private route: ActivatedRoute
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            try {
                if (!this.assetTypeClass) {
                    let assetTypeClassString: keyof typeof AssetTypeClass = params["class"];
                    this.assetTypeClass = AssetTypeClass[assetTypeClassString];
                    if (!this.assetTypeClass) {
                        this.assetTypeClass = AssetTypeClass.BusinessAsset;
                    }
                }
            } catch (e) {
                this.assetTypeClass = AssetTypeClass.BusinessAsset;
            }
            let name: string = "";
            switch (this.assetTypeClass) {
                case AssetTypeClass.BusinessAsset:
                    name = "Business Asset";
                    break;
                case AssetTypeClass.TechnicalAsset:
                    name = "Technical Asset";
                    break;
                case AssetTypeClass.DiagramAsset:
                    name = "Diagram Asset";
                    break;
                case AssetTypeClass.Rule:
                    name = "Rule";
                    break;
                case AssetTypeClass.Model:
                    name = "Model";
                    break;
                default: name = "Business Asset";
                    break;

            }

            this.className = name;
        });
    }

    delete(): void {
        if (this.isLoading) {
            return;
        }

        this.isLoading = true;

        if (this.callback) {
            if (this.artifactTypeUid)
                this.callback(this.artifactTypeUid);
            else
                this.callback(this.artifactTypeId);

        }

    }

    cancel(): void {
        this.onCancel.emit(null);
    }
}
