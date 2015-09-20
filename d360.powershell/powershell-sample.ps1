$company = "964CF4A5-040E-428F-B769-E39076D3E96D"
$key = "uZ71iTXzp8WHiO&naa-8gJzSq"
$secret = "kuENJ7C1IeW4pXe1TFfsmjWOs=U-37AKFd4v4FNogGmCVW0T2m"
$auth = $company + ";" + $key + ";" + $secret

$evts = @{ 
	DateCreated="2013-07-31T23:30:00.000"; 
	GroupKey="Somearbitrarygroupkey"; 
	Name="Test Recon Exception From IDQ"; 
	Events=@(
		@{
		SourceID="8745-4356-46ADDFFER-454545";
		instBatchID="1"; 
		instRecTypeID="234"; 
		iStartTime="2013-07-31T21:08:00.000"; 
		iEndTime="2013-07-31T23:08:00.000"; 
		mdRecTypeID="somRecType"; 
		iDescription="A description"; 
		mdRecTypeRuleId="Key Orphan"; 
		mdSourceID="Nulls"; 
		keyField="TheKey"; 
		keyValue="TheKeyValue"; 
		fieldName="TheField"; 
		fieldValue="TheFieldValue"; 
		PrimaryKey="Primarykey"; 
		SourcePrimaryKey="Secondary Key"; 
		SecInfo="Some security info"; 
		EntityInfo="Some entity info"; 
		instRecTypeExceptionID="3456232"; 
		isOpen="Open"; 
		info="Last record for exception"; 
		Severity="1"; 
		};
		)
	} | ConvertTo-Json

$results = Invoke-RestMethod -Uri http://api.data3sixty.com/events/5 -Headers @{"Authorization"=$auth} -ContentType "application/json; charset=utf-8" -Method Post -Body $evts