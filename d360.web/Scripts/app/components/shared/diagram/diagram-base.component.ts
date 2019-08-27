import { BaseComponent } from '../base.component';
import * as go from 'gojs';

export class DiagramBaseComponent extends BaseComponent {

    protected diagram: go.Diagram;
    protected palette: go.Palette;
    protected g = go.GraphObject.make;

    constructor() {
        super();
        (go as any).licenseKey = this.getLicenseKey();
    }

    protected getLicenseKey(): string {
        let licenseKey = "73f146e2b20537c702d90776423d6bf919a17564ce8418a30d0415f6e8083d06329fee2b58d38d90d0af4cfe1c7cc989d8c0392093480d3db531d1db42e182aeb73320e5410b479cb40573939ffa78f1fd6a61f1c3b57fbdd3678ff5";
        return licenseKey;
    }

    protected diagramModelAsGraph(): go.GraphLinksModel {
        return <go.GraphLinksModel>this.diagram.model;
    }
}