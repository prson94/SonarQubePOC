create procedure [fusion].[ClearMarkitMapLineageData]
as
begin
	delete from mapitem where [owner] = 'MARKIT LINEAGE';
	delete from mapruleitem where [owner] = 'MARKIT LINEAGE';
	delete from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE';
	delete from [intersect] where [owner] = 'MARKIT LINEAGE';
end