--rollback
--alter table [predicate] drop constraint DF_Predicate_UID;
--alter table [predicate] drop column [UID];
--go
--


alter table [predicate] add [UID] uniqueidentifier;
go

--system types
update [predicate] set [uid] = 'D7FF74B8-5606-4FB9-A7EF-F42BE4299DC9' where id = 1;
update [predicate] set [uid] = 'B8A4C392-6431-4CD7-A4EE-ABF260D538FD' where id = 2;
update [predicate] set [uid] = '0F718E3D-13B1-4EFB-A407-258DEC05B844' where id = 3;
update [predicate] set [uid] = '267D2361-CBE0-4C38-935E-226C222EE51D' where id = 4;
update [predicate] set [uid] = 'DF813D88-7D53-482A-AF7A-DC35B13001ED' where id = 5;

update [predicate]
set [UID] = newID()
where [UID] is null;

go

alter table [predicate] alter column [UID] uniqueidentifier not null;
go
ALTER TABLE [predicate] ADD  CONSTRAINT [DF_Predicate_UID]  DEFAULT (newid()) FOR [uid];
go