using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using System.Threading;
using Google.Apis.Services;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Util.Store;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2.Web;
using GAPIs_Calendar_v3.Add_Code.CalendarIntegrator;

namespace GAPIs_Calendar_v3.Add_Code.GApisCalV3Integrator
{
    public class GooCalV3Integrator : GooCalIntegrator
    {
        //
        //  Inherit methods from parent class

        override public object Authorize(HttpRequest Request, string accountName)
        {
            try
            {
                var uri = Request.Url.ToString();
                var code = Request["code"];
                var error = Request["error"];

                if (!string.IsNullOrEmpty(error))
                    if (error == "access_denied")
                        return new UserRejectException();
                    else
                        return new UnknownException(error);

                // перед авторизацией нужно обнулить
                CalendarService = null;

                if (code != null)
                {
                    string redirectUri = uri.Substring(0, uri.IndexOf("?"));
                    var token = CodeFlow.ExchangeCodeForTokenAsync(accountName, code, redirectUri, CancellationToken.None).Result;
                    string state = Request["state"];
                    var result = AuthWebUtility.ExtracRedirectFromState(CodeFlow.DataStore, accountName, state);
                    return result;
                }
                else
                {
                    string redirectUri = uri;
                    string state = "ostate_";// Guid.NewGuid().ToString("N");
                    var result = new AuthorizationCodeWebApp(CodeFlow, redirectUri, state).AuthorizeAsync(accountName, CancellationToken.None).Result;
                    if (result.RedirectUri != null)
                    {
                        return result;
                    }
                    else
                    {
                        CalendarService = new CalendarService(new BaseClientService.Initializer()
                        {
                            HttpClientInitializer = result.Credential,
                            ApplicationName = APPLICATION_NAME
                        });
                        // alright
                        return "OK";
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

        }

        public override object GetCalendarList()
        {
            try
            {
                CalendarListResource.ListRequest req = CalendarService.CalendarList.List();
                return req.Execute();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public override object GetCalendar(string calendarId)
        {
            try
            {
                return CalendarService.CalendarList.Get(calendarId).ExecuteAsync().Result;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public override object GetEvents(string calendarId, DateTime? timeMin = null, DateTime? timeMax = null)
        {
            try
            {
                List<Event> result = new List<Event>();
                string pageToken = null;
                var eventList = CalendarService.Events.List(calendarId);

                // задам ограничения списка событий
                if (timeMin != null)
                    eventList.TimeMin = timeMin;
                if (timeMax != null)
                    eventList.TimeMax = timeMax;

                do
                {
                    eventList.PageToken = pageToken;
                    var events = eventList.ExecuteAsync().Result;
                    events.Items.ToList().ForEach(e => result.Add(e));
                    pageToken = events.NextPageToken;
                } while (pageToken != null);

                return result;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}