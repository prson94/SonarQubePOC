CREATE FUNCTION dbo.SpaceBeforeCapitalLetter
(
 @str nvarchar(max)
)
returns nvarchar(max)
as
begin

declare @i int, @j int
declare @returnval nvarchar(max)
set @returnval = ''
select @i = 1, @j = len(@str)

declare @w nvarchar(max)

while @i <= @j
begin
 if substring(@str,@i,1) = UPPER(substring(@str,@i,1)) collate Latin1_General_CS_AS
 begin
  if @w is not null
  set @returnval = @returnval + ' ' + @w
  set @w = substring(@str,@i,1)
 end
 else
  set @w = @w + substring(@str,@i,1)
 set @i = @i + 1
end
if @w is not null
 set @returnval = @returnval + ' ' + @w

return ltrim(@returnval)
end