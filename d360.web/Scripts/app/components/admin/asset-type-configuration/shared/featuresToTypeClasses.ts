import { AssetTypeClass } from "../../../../models/asset.model";

export const featuresToTypeClasses = {
	assetTypeChilds: [
		AssetTypeClass.BusinessAsset,
		AssetTypeClass.TechnicalAsset
	],
	assetTypeMaxDepth: [
		AssetTypeClass.Model,
		AssetTypeClass.Policy
	],
	backgroundColor: [
		AssetTypeClass.BusinessAsset,
		AssetTypeClass.TechnicalAsset
	],
	icon: [
		AssetTypeClass.BusinessAsset,
		AssetTypeClass.TechnicalAsset,
		AssetTypeClass.DiagramAsset
	],
	flowObjectType: [
		AssetTypeClass.DiagramAsset
	]
};