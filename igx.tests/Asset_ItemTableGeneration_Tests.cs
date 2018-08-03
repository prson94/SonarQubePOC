using d360.core;
using d360.core.entities;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.extensions.storage;
using d360.model;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace igx.tests
{
    [TestClass]
    public class Asset_ItemTableGeneration_Tests : BaseTest
    {
        [TestMethod]
        public void DropTables()
        {
            var company = getCompanyConnection(4);
            company.Open();
            var assetTypes = company.Query<AssetType>("select * from AssetType").ToList();

            assetTypes.ForEach(at => {
                var assetTableSql = $"drop table asset.Item_{at.ID}";
                company.Execute(assetTableSql);
            });

            company.Close();
            company.Dispose();

        }

        [TestMethod]
        public void GenerateTables()
        {
            var company = getCompanyConnection(4);
            company.Open();
            var assetTypes = company.Query<AssetType>("select * from AssetType").ToList();
            var fieldTypes = company.Query<FieldType>("select * from FieldType").ToList();

            assetTypes.ForEach(at => {
                var columns = new List<string>();
                
                foreach (var ft in fieldTypes.Where(i => i.AssetTypeID == at.ID).OrderBy(i => i.ID))
                {
                    var dataType = "";
                    var nullable = (ft.IsRequired) ? "not null" : "null";
                    switch (ft.Type)
                    {
                        case "Boolean":
                            dataType = "bit";
                            break;
                        case "Date":
                            dataType = "date";
                            break;
                        case "DateTime":
                            dataType = "datetime";
                            break;
                        case "Html":
                            dataType = "nvarchar(max)";
                            break;
                        case "Number":
                            dataType = "int";
                            break;
                        case "Decimal":
                            dataType = "decimal(18,3)";
                            break;
                        case "Text":
                            dataType = "nvarchar(max)";
                            break;
                        case "Password":
                            dataType = "nvarchar(max)";
                            break;
                        case "Link":
                            dataType = "nvarchar(max)";
                            break;
                        case "Color":
                            dataType = "nvarchar(25)";
                            break;
                    }
                    if (!string.IsNullOrEmpty(dataType))
                    {
                        columns.Add($"[{ft.ID}] {dataType} {nullable}");
                    }
                }

                var assetTableSql = $"create table asset.Item_{at.ID} (ID bigint not null, {string.Join(", ", columns)} {((columns.Count>0) ?", ":"")} constraint [PK_Asset_Item_{at.ID}] primary key clustered (ID asc))";
                company.Execute(assetTableSql);
            });

            company.Close();
            company.Dispose();

        }

        [TestMethod]
        public void LoadTables()
        {
            var company = getCompanyConnection(4);
            company.Open();
            var assetTypes = company.Query<AssetType>("select * from AssetType where ID = 39").ToList();
            var fieldTypes = company.Query<FieldType>("select * from FieldType where Type not in ('FusionLookup', 'Attribute', 'FilteredLookup', 'ComplexRelationLookup', 'OwnershipLookup', 'Relationship', 'FieldFromRelationship', 'RefListRelationship')").ToList();

            assetTypes.ForEach(at => {
                var columns = new List<string>() { "A.ID" };
                var joins = new List<string>();

                foreach (var ft in fieldTypes.Where(i => i.AssetTypeID == at.ID).OrderBy(i => i.ID))
                {
                    switch (ft.Type)
                    {
                        case "Boolean":
                            joins.Add($"left join Field F{ft.ID} on F{ft.ID}.AssetID = A.ID and F{ft.ID}.FieldTypeID = {ft.ID}");
                            columns.Add($"IIF(upper(F{ft.ID}.Value) = 'TRUE', 1, 0) as [{ft.ID}]");
                            break;
                        case "Date":
                        case "DateTime":
                        case "Html":
                        case "Number":
                        case "Decimal":
                        case "Text":
                        case "Password":
                        case "Link":
                        case "Color":
                            joins.Add($"left join Field F{ft.ID} on F{ft.ID}.AssetID = A.ID and F{ft.ID}.FieldTypeID = {ft.ID}");
                            if (ft.IsRequired)
                                columns.Add($"coalesce(F{ft.ID}.Value, '') as [{ft.ID}]");
                            else
                                columns.Add($"F{ft.ID}.Value as [{ft.ID}]");
                            break;
                    }
                }

                try
                {
                    var assetTableSql = $@"
insert into asset.Item_{at.ID} 
    select	{string.Join(", ", columns)}
    from    Asset A
            {string.Join(" ", joins)}
    where   A.AssetTypeID = {at.ID}";
                    company.Execute(assetTableSql);
                }
                catch(Exception ex)
                {
                }
            });

            company.Close();
            company.Dispose();

        }
    }
}
