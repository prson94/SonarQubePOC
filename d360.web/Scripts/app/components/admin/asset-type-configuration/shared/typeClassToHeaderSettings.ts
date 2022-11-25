import { AssetTypeClass } from "../../../../models/asset.model";

export const typeClassToHeaderSettings = new Map([
    [
        AssetTypeClass.BusinessAsset, {
            icon: 'fa-sliders',
            title: $localize`Business Assets`
        }
    ],
    [
        AssetTypeClass.TechnicalAsset, {
            icon: 'fa-sliders',
            title: $localize`Technical Assets`
        }
    ],
    [
        AssetTypeClass.DiagramAsset, {
            icon: 'fa-cog',
            title: $localize`Diagram Assets`
        }
    ]
]);
