import { BaseComponent } from '../base.component';
import * as go from 'gojs';

export class DiagramBaseComponent extends BaseComponent {
    constructor() {
        super();
        (go as any).licenseKey = this.getLicenseKey();
    }

    protected getLicenseKey(): string {
        let licenseKey = "73fe41e0ba1c28c702d95d76423d6cbc5cf07f21de824aa0055116a7ee5b69172699eb7003d78dc8d1f84efa1b7d93ded8d7792f911f0c3be161d18b41e080f8bb6776b74401438aac0574c39bfd2ba2f82f74f691e222a1da6a9cf4bef8c59c0eb8f2c658c90fbb2f670e2e557a";
        return licenseKey;
    }
}