using d360.core.entities;
using d360.core.exceptions;
using d360.model;
using d360.web.Filters;
using d360.web.Extensions;
using Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Configuration;

namespace d360.web.Controllers
{
    public partial class FormController : BaseController
    {
        #region Report

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddReport")]
        public async Task<JsonResult> AddReport(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_FieldType);

                var objectType = form["ObjectType"].Split('|').ToArray();

                if (objectType.Length == 2)
                {
                    var fileCount = HttpContext.Request.Files.Count;
                    var reportType = parseTextField(form, "ReportType");
                    var name = parseTextField(form, "Name");
                    var showOnHomePage = reportType == "legacy" ? false : parseBooleanField(form, "ShowOnHomePage");
                    string powerBIID = string.Empty;
                    string datasetID = string.Empty;
                    string filename = string.Empty;

                    if (fileCount > 0 && reportType == "powerbi")
                    {
                        var file = HttpContext.Request.Files[0];


                        if (file.ContentLength > 0)
                        {
                            var importResult = await uploadPowerBIReport(file, name);

                            if (importResult.ImportState == "Failed")
                            {
                                throw new ArgumentNullException(FormControllerApiMessage.FailedToLoadPowerBI);
                            }
                            datasetID = importResult.Datasets.FirstOrDefault().Id;
                            powerBIID = importResult.Reports.FirstOrDefault().Id.ToString();
                            filename = file.FileName;
                        }
                    }
                    else if (reportType == "powerbi" && fileCount == 0)
                    {
                        throw new ConflictException(ApiMessages.Error,FormControllerApiMessage.FileRequired);
                    }

                    var model = new Report
                    {
                        Name = parseTextField(form, "Name"),
                        Description = parseTextField(form, "Description"),
                        ObjectType = objectType[0],
                        ObjectID = int.Parse(objectType[1]),
                        ReportType = parseTextField(form, "ReportType"),
                        PowerBIReportID = string.IsNullOrEmpty(powerBIID) ? null : powerBIID,
                        PowerBIDatasetID = string.IsNullOrEmpty(datasetID) ? null : datasetID,
                        Url = parseTextField(form, "Url"),
                        ShowOnHomePage = showOnHomePage,
                        FileName = filename
                    };

                    var visibleTo = form["VisibleTo"];

                    if (!string.IsNullOrEmpty(visibleTo))
                    {
                        model.Responsibilities = new List<ReportResponsibility>();

                        var visibleToResponsibilityTypes = visibleTo.Split(',').Select(x => int.Parse(x));

                        //add any new responsibilities
                        foreach (var newResponsibilityType in visibleToResponsibilityTypes)
                        {
                            model.Responsibilities.Add(new ReportResponsibility
                            {
                                ReportID = model.ID,
                                ResponsibilityTypeID = newResponsibilityType
                            });
                        }
                    }

                    if (showOnHomePage)
                    {
                        var existing = Company.Filter<Report>(r => r.ShowOnHomePage).FirstOrDefault();
                        if (existing != null)
                        {
                            existing.ShowOnHomePage = false;
                            Company.Update(existing);
                        }
                    }

                    Company.Add(model);

                    return jsonSuccess(string.Format(ApiMessages.SucessfullyCreated,FormControllerApiMessage.Dashboard), model.ID.ToString(), "add", HttpStatusCode.Created);
                }
                else
                {
                    throw new MissingPropertiesException(FormControllerApiMessage.Report);
                }
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        private static readonly string pbiUsername = ConfigurationManager.AppSettings["pbiUsername"];
        private static readonly string pbiPassword = ConfigurationManager.AppSettings["pbiPassword"];

        [HttpDelete, Route("DeleteReport")]
        public async Task<JsonResult> DeleteReport(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Report>(id);
                if (model == null) throw new NotFoundException(FormInfo.NoFormData_FieldType);

                //delete any power bi reports
                if (model.ReportType == "powerbi" && !string.IsNullOrEmpty(model.PowerBIDatasetID))
                {
                    var companySettings = SettingsRepository.GetSettings();

                    var clientId = companySettings.First(s => s.ID == core.enums.Setting.PowerBIClientId).Value;
                    var groupId = companySettings.First(s => s.ID == core.enums.Setting.PowerBIGroupId).Value;

                    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(groupId))
                    {
                        throw new ArgumentNullException(FormControllerApiMessage.UnableToFindPowerBISettings);
                    }

                    try
                    {
                        await PowerBI.DeleteDataset(pbiUsername, pbiPassword, clientId, groupId, model.PowerBIDatasetID);
                    }
                    catch { } // ok we cant delete the report delete the reference to it at least
                }

                Company.Delete(model);

                return jsonSuccess(string.Format(ApiMessages.SucessfullyDeleted, FormControllerApiMessage.Dashboard), id.ToString(), "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddPowerBICredentials")]
        public async Task<JsonResult> AddPowerBICredentials(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_FieldType);

                //get username / password
                var user = parseTextField(form, "Username");
                var pwd = parseTextField(form, "Password");

                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pwd))
                {
                    throw new ArgumentNullException(FormControllerApiMessage.PleaseSpecifyUserNamePassword);
                }
                var companySettings = SettingsRepository.GetSettings();
                var groupId = companySettings.First(s => s.ID == core.enums.Setting.PowerBIGroupId).Value;
                var clientId = companySettings.First(s => s.ID == core.enums.Setting.PowerBIClientId).Value;

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(groupId))
                {
                    throw new ArgumentNullException(FormControllerApiMessage.UnableToFindPowerBISettings);
                }
                // if the workspace id is null create a new one and update the companysettings
                groupId = await checkPowerBIValidWorkspace(groupId, clientId);

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(groupId))
                {
                    throw new ArgumentNullException(FormControllerApiMessage.UnableToFindPowerBISettings);
                }
                //save password in this workspace for all ds's
                await PowerBI.UpdateConnectionCredentials(pbiUsername, pbiPassword, clientId, groupId, user, pwd);

                return jsonSuccess(string.Format(ApiMessages.SucessfullyUpdated,FormControllerApiMessage.PowerBICredentials), "", "add", HttpStatusCode.Created);

            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPut, ValidateInput(false), Route("EditReport")]
        public async Task<JsonResult> EditReport(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Report>(id);

                if (model == null) throw new NotFoundException(FormInfo.NoFormData_FieldType);

                var fileCount = HttpContext.Request.Files.Count;
                var reportType = parseTextField(form, "ReportType");
                var name = parseTextField(form, "Name");
                var showOnHomePage = reportType == "legacy" ? false : parseBooleanField(form, "ShowOnHomePage");
                string powerBIID = string.Empty;
                string datasetID = string.Empty;
                string filename = string.Empty;
                string url = parseTextField(form, "Url");

                if (fileCount > 0 && reportType == "powerbi")
                {
                    var file = HttpContext.Request.Files[0];

                    if (file.ContentLength > 0)
                    {
                        var importResult = await uploadPowerBIReport(file, name, model.PowerBIDatasetID);

                        if (importResult.ImportState == "Failed")
                        {
                            throw new ArgumentNullException(FormControllerApiMessage.FailedToLoadPowerBI);
                        }

                        datasetID = importResult.Datasets.FirstOrDefault().Id;

                        var rpt = importResult.Reports.FirstOrDefault();

                        if (rpt != null)
                            powerBIID = rpt.Id.ToString();

                        filename = file.FileName;
                    }
                }
                else if (reportType == "powerbi" && string.IsNullOrEmpty(model.FileName))
                {
                    throw new ConflictException(ApiMessages.Error, FormControllerApiMessage.FileRequired);
                }

                var visibleTo = form["VisibleTo"];

                if (!string.IsNullOrEmpty(visibleTo))
                {
                    var visibleToResponsibilityTypes = visibleTo.Split(',').Select(x => int.Parse(x));

                    //delete any removed responsibilities
                    foreach (var responsibility in model.Responsibilities.ToList())
                    {
                        if (!visibleToResponsibilityTypes.Contains(responsibility.ResponsibilityTypeID))
                            Company.ReportResponsibilities.Remove(responsibility);

                    }

                    //add any new responsibilities
                    foreach (var newResponsibilityType in visibleToResponsibilityTypes)
                    {
                        if (!model.Responsibilities.Any(x => x.ResponsibilityTypeID == newResponsibilityType))
                        {
                            model.Responsibilities.Add(new ReportResponsibility
                            {
                                ReportID = model.ID,
                                ResponsibilityTypeID = newResponsibilityType
                            });
                        }
                    }
                }
                else
                {
                    foreach (var responsibility in model.Responsibilities.ToList())
                    {
                        Company.ReportResponsibilities.Remove(responsibility);
                    }
                }

                // Static fields
                var objectType = form["ObjectType"].Split('|').ToArray();

                if (objectType.Length == 2)
                {
                    model.Name = name;
                    model.Description = parseTextField(form, "Description");
                    model.ObjectType = objectType[0];
                    model.ObjectID = int.Parse(objectType[1]);
                    model.ReportType = reportType;
                    model.Url = url;
                    model.ShowOnHomePage = showOnHomePage;

                    if (!string.IsNullOrEmpty(datasetID))
                        model.PowerBIDatasetID = datasetID;

                    if (!string.IsNullOrEmpty(powerBIID))
                        model.PowerBIReportID = powerBIID;

                    if (!string.IsNullOrEmpty(filename))
                        model.FileName = filename;

                    if (showOnHomePage)
                    {
                        var existing = Company.Filter<Report>(r => r.ShowOnHomePage).FirstOrDefault();
                        if (existing != null)
                        {
                            existing.ShowOnHomePage = false;
                            Company.Update(existing);
                        }
                    }

                    Company.Update<Report>(model);

                    return jsonSuccess(string.Format(ApiMessages.SucessfullyEdited,FormControllerApiMessage.Dashboard), id.ToString(), "edit", HttpStatusCode.OK);
                }
                else
                {
                    throw new MissingPropertiesException(FormControllerApiMessage.Report);
                }
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        private async Task<string> checkPowerBIValidWorkspace(string groupId, string clientId)
        {
            groupId = (groupId ?? "").Trim();

            if (string.IsNullOrEmpty(groupId) && !string.IsNullOrEmpty(clientId))
            {
                var groupName = $"D3S{Company.CurrentCompanyID}";
                var res = await PowerBI.CreateWorkspace(pbiUsername, pbiPassword, clientId, groupName);
                SettingsRepository.UpsertSetting(core.enums.Setting.PowerBIGroupId, res.Id.ToString());
                return res.Id.ToString();
            }

            return groupId;
        }

        private async Task<Microsoft.PowerBI.Api.V2.Models.Import> uploadPowerBIReport(HttpPostedFileBase file, string name, string datasetId = "")
        {
            var companySettings = SettingsRepository.GetSettings();
            var groupId = companySettings.First(s => s.ID == core.enums.Setting.PowerBIGroupId).Value;
            var clientId = companySettings.First(s => s.ID == core.enums.Setting.PowerBIClientId).Value;

            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(FormControllerApiMessage.UnableToFindPowerBISettings);
            }
            // if the workspace id is null create a new one and update the companysettings
            groupId = await checkPowerBIValidWorkspace(groupId, clientId);

            // if an existing one exists delete it
            if (!string.IsNullOrEmpty(datasetId))
                await PowerBI.DeleteDataset(pbiUsername, pbiPassword, clientId, groupId, datasetId);


            return await PowerBI.ImportPbix(pbiUsername, pbiPassword, clientId, groupId, name, file.InputStream);
        }

        #endregion

        #endregion

    }
}