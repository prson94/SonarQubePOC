--rollback
--alter table [predicate] drop constraint DF_Predicate_Uid;
--alter table [predicate] drop column [Uid];
--go;
--


alter table [predicate] add [Uid] uniqueidentifier;
go;

--system types
update [predicate] set [Uid] = 'D7FF74B8-5606-4FB9-A7EF-F42BE4299DC9' where id = 1;
update [predicate] set [Uid] = 'B8A4C392-6431-4CD7-A4EE-ABF260D538FD' where id = 2;
update [predicate] set [Uid] = '0F718E3D-13B1-4EFB-A407-258DEC05B844' where id = 3;
update [predicate] set [Uid] = '267D2361-CBE0-4C38-935E-226C222EE51D' where id = 4;
update [predicate] set [Uid] = 'DF813D88-7D53-482A-AF7A-DC35B13001ED' where id = 5;
update [predicate] set [Uid] = 'C88EBECD-EED5-4C27-99BE-A1EED29C13DD' where id = 44;
update [predicate] set [Uid] = '2A7FA12D-63AA-4595-83D0-CFA98AAC2AA4' where id = 45;

update [predicate]
set [Uid] = newID()
where [Uid] is null;

go;

alter table [predicate] alter column [Uid] uniqueidentifier not null;
go;
ALTER TABLE [predicate] ADD  CONSTRAINT [DF_Predicate_Uid]  DEFAULT (newid()) FOR [Uid];
go;