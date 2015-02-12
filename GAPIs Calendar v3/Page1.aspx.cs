using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using GAPIs_Calendar_v3.Add_Code.GApisCalV3Integrator;
using System.Web.UI.HtmlControls;
using Google.Apis.Auth.OAuth2;
using System.Web.Configuration;
using Google.Apis.Calendar.v3;
using System.Threading;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using System.Security.Cryptography.X509Certificates;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Web;
using GAPIs_Calendar_v3.Add_Code.CalendarIntegrator;
using System.Threading.Tasks;

namespace GAPIs_Calendar_v3
{
    public partial class Page1 : System.Web.UI.Page
    {
        protected void Page_Init(object s, EventArgs e)
        {
            var clear = Request["clear"];
            if (clear != null)
            {
                Session["GapiUserId"] = null;
                Session["GooCalIntegrator"] = null;
                Response.Redirect(Request.Url.LocalPath, true);
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var newUserId = (IsPostBack && !string.IsNullOrEmpty(AccountNameInput.Text)) ? AccountNameInput.Text : null;
            var code = Request["code"];
            var error = Request["error"];
            var action = Request["action"];

            if (newUserId != null)
            {
                Session["GapiUserId"] = newUserId;
            }

            if (newUserId != null || code != null || error != null || action != null)
            {
                GooCalIntegrator integra = new GooCalV3Integrator();
                var result = integra.Authorize(Request, Session["GapiUserId"] as string);

                // проверяю обработку
                if (result is AuthorizationCodeWebApp.AuthResult)
                {
                    var authResult = result as AuthorizationCodeWebApp.AuthResult;
                    // нужно выполнить переход на этот url
                    Response.Redirect(authResult.RedirectUri);
                }
                else if (result is string && (result as string) == "OK")
                {
                    // все ок, пользователь авторизовался
                    // запоминаю объект интегратора
                    Session["GooCalIntegrator"] = integra;
                    // перехожу на следующую страницу
                    Response.Redirect("Page2.aspx");
                }
                else if (result is Task<string>)
                {
                    //var task = result as Task<string>;
                    // авторизация прошла, нужно сейчас просто обновить страницу и снова авторизоваться
                    Response.Redirect(Request.Url.AbsolutePath + "?action=check");
                }
                else if (result is UserRejectException)
                {
                    // пользователь отвергнул приглашение
                    RenderException(new Exception("Пользователь отвергнул приглашение", result as UserRejectException));
                }
                else if (result is Exception)
                {
                    // произошла другая ошибка при обращении к сервису Google Api
                    RenderException(result as Exception);
                }
                else
                {
                    // случилось непредвиденное
                    RenderException(new Exception("Случилось непредвиденное", new Exception("GooCalIntegrator.Authorize вернул другой тип результата")));
                }
            }
        }

        protected void RenderException(Exception ex)
        {
            while (ex != null)
            {
                ErrorPan.Controls.Add(new HtmlGenericControl() { InnerText = "• " + ex.Message });
                ex = ex.InnerException;
            }
        }
    }
}