using d360.web.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Resources;
using d360.core.exceptions;

namespace d360.web.Handlers
{
    public class ErrorMessageHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            try
            {
                return GenerateResponse(request, response);
            }
            catch (Exception ex)
            {
                return request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private HttpResponseMessage GenerateResponse(HttpRequestMessage request, HttpResponseMessage response)
        {
            string errorMessage = null;
            if (!IsResponseValid(response))
            {
                object responseContent;
                if (response.TryGetContentValue(out responseContent))
                {
                    try
                    {
                        if (responseContent is ErrorResponse)
                        {
                            return response;
                        }
                        else if (responseContent is Controllers.BaseApiController.GenericHttpError)
                        {
                            var httpError = responseContent as Controllers.BaseApiController.GenericHttpError;

                            if (httpError != null)
                            {
                                errorMessage = httpError.Message;
                                responseContent = null;
                            }

                            var responseMetadata = new ErrorResponse
                            {
                                message = errorMessage,
                                title = OthersMessages.BadRequestSubmitted
                            };
                            var result = request.CreateResponse(response.StatusCode, responseMetadata);
                            return result;
                        }
                        else if (responseContent is GenericException)
                        {
                            var genEx = responseContent as GenericException;
                            var responseMetadata = new ErrorResponse
                            {
                                message = genEx.StatusMessage,
                                title = genEx.StatusDescription
                            };
                            return request.CreateResponse(genEx.StatusCode, responseMetadata);
                        }
                        else if (responseContent is Exception)
                        {
                            var responseMetadata = new ErrorResponse
                            {
                                message = (responseContent as Exception).Message,
                                title = OthersMessages.BadRequestSubmitted
                            };
                            return request.CreateResponse(response.StatusCode, responseMetadata);
                        }
                        else
                        {
                            var httpError = responseContent as HttpError;

                            if (httpError != null)
                            {
                                errorMessage = httpError.Message;
                                responseContent = null;

                                if (!string.IsNullOrEmpty(httpError.ExceptionMessage))
                                    errorMessage += (" " + httpError.ExceptionMessage);
                            }

                            var responseMetadata = new ErrorResponse
                            {
                                message = errorMessage,
                                title = OthersMessages.BadRequestSubmitted
                            };
                            return request.CreateResponse(response.StatusCode, responseMetadata);
                        }
                    }
                    catch
                    {
                    } //continue on to return the normal response.
                }
            }

            return response;
        }

        private bool IsResponseValid(HttpResponseMessage response)
        {
            int statusCode = (int)response.StatusCode;
            if ((response != null) && (statusCode >= 200 && statusCode < 300))
                return true;
            return false;
        }
    }
}