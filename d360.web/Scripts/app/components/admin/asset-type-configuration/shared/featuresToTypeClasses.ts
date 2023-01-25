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
		AssetTypeClass.TechnicalAsset,
		AssetTypeClass.Model,
		AssetTypeClass.Policy,
		AssetTypeClass.Rule
	],
	icon: [
		AssetTypeClass.BusinessAsset,
		AssetTypeClass.TechnicalAsset,
		AssetTypeClass.DiagramAsset,
		AssetTypeClass.Model,
		AssetTypeClass.Policy,
		AssetTypeClass.Rule
	],
	flowObjectType: [
		AssetTypeClass.DiagramAsset
	]
};