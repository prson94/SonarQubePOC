CREATE view [analytics].[StatisticDetail]
as
select	S.ID,
		S.ResourceID,
		RE.FirstName + ' ' + RE.LastName as ResourceName,
		'/resource/' + cast(RE.ResourceID as varchar) as ResourceUrl,
		S.[Timestamp],
		A.Value as [Action],
		B.Value as BrowserLanguage,
		H.Value as [Host],
		I.Value as [Ip],
		O.Value as [Object],
		S.ObjectID,
		coalesce(OA.DisplayValue, OAT.Name, ORE.Name) as ObjectName,
		U.Value as UserAgent
from	[analytics].Statistic S
		inner join [analytics].[Action] A on A.ID = S.ActionID
		inner join [analytics].[BrowserLanguage] B on B.ID = S.BrowserLanguageID
		inner join [analytics].[Host] H on H.ID = S.HostID
		inner join [analytics].[Ip] I on I.ID = S.IpID
		inner join [analytics].[Object] O on O.ID = S.Object
		inner join [analytics].[UserAgent] U on U.ID = S.UserAgentID
		left join reporting.Global_Resource RE on S.ResourceID = RE.ResourceID
		left join Artifact OA on O.Value = 'Artifact' and OA.ID = S.ObjectID
		left join ArtifactType OAT on O.Value = 'ArtifactType' and OAT.ID = S.ObjectID
		left join Report ORE on O.Value = 'Report' and ORE.ID = S.ObjectID