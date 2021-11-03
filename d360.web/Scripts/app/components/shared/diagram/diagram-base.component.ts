import { BaseComponent } from '../base.component';
import * as go from 'gojs';
import { CompanySettingsService } from '../../../services/settings.service';

export class DiagramBaseComponent extends BaseComponent {

    protected diagram: go.Diagram;
    protected palette: go.Palette;
    protected g = go.GraphObject.make;

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
        (go as any).licenseKey = this.getLicenseKey();
    }

    protected getLicenseKey(): string {
        let licenseKey = "73f142e7b60537c702d90776423d6af919a17564ce841ca30a0411f6ef0d3d06329fee2b58d38d90d0af4cfe1c7cc989d8c0392093480d3db531d1db42e182aeb73320e5410b479cb40573939ffa78f1fd6a61f1c3b676bddc678ff1";
        return licenseKey;
    }

    protected diagramModelAsGraph(): go.GraphLinksModel {
        return <go.GraphLinksModel>this.diagram.model;
    }
}