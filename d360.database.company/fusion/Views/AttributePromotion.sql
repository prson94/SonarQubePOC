CREATE view [fusion].[AttributePromotion]
as
	select	R.ID,
			R.FusionID,

			FA.TextPath as ObjectName,
			R.ObjectType,
			R.ObjectID,

			case
				when R.PromotionObjectType = 'DomainType' and R.PromotionParentObjectType = 'Domain' then 'Reference: ' + PRO.Name + ' List Item' 
				when R.PromotionObjectType = 'DomainType' and R.PromotionParentObjectType is null then 'Reference: ' + PRO.Name + ' List' 
				when R.PromotionObjectType = 'ArtifactType' then 'Glossary: ' + PRO.Name 
				when R.PromotionObjectType = 'TaxonomyType' then 'Model: ' + PRO.Name 
				else PRO.Name
			end as PromotionName,
			R.PromotionObjectType, 
			R.PromotionObjectID,

			case
				when R.PromotionParentObjectType = 'Domain' then 'Reference: ' + PPO.ObjectTypeName + ': ' + PPO.Name
				when R.PromotionParentObjectType = 'Artifact' then 'Glossary: ' + PPO.ObjectTypeName + ': ' + PPO.Name 
				when R.PromotionParentObjectType = 'Taxonomy' then 'Model: ' + PPO.ObjectTypeName + ': ' + PPO.Name 
				else PPO.Name
			end as PromotionParentName,
			R.PromotionParentObjectType, 
			R.PromotionParentObjectID,

			R.[Enabled]

	from	FusionAttributePromotionRule R
			left join cache.ObjectDetails FA on FA.[Object] = R.ObjectType and FA.ObjectID = R.ObjectID
			left join cache.ObjectDetails PRO on
				PRO.[Object] = case 
					when R.PromotionObjectType = 'Artifact' then 'ArtifactType' 
					when R.PromotionObjectType = 'Domain' then 'DomainType' 
					when R.PromotionObjectType = 'Taxonomy' then 'TaxonomyType' 
					else R.PromotionObjectType 
				end
				and PRO.ObjectID = R.PromotionObjectID
			left join cache.ObjectDetails PPO on PPO.[Object] = R.PromotionParentObjectType and PPO.ObjectID = R.PromotionParentObjectID
