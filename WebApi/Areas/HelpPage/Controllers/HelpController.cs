using System;
using System.Web.Http;
using System.Web.Mvc;
using WebApi.Areas.HelpPage.Models;

namespace WebApi.Areas.HelpPage.Controllers
{
    /// <summary>
    /// The controller that will handle requests for the help page.
    /// </summary>
    public class HelpController : Controller
    {
        //Remove constructors and existing Configuration property.

        //public HelpController()
        //    : this(GlobalConfiguration.Configuration)
        //{
        //}

        //public HelpController(HttpConfiguration config)
        //{
        //    Configuration = config;
        //}

        //public HttpConfiguration Configuration { get; private set; }

        /// <summary>
        /// Add new Configuration Property
        /// </summary>
        protected static HttpConfiguration Configuration
        {
            get { return GlobalConfiguration.Configuration; }
        }

        public ActionResult Index()
        {
            return View(Configuration.Services.GetApiExplorer().ApiDescriptions);
        }

        public ActionResult Api(string apiId)
        {
            if (!String.IsNullOrEmpty(apiId))
            {
                HelpPageApiModel apiModel = Configuration.GetHelpPageApiModel(apiId);
                if (apiModel != null)
                {
                    return View(apiModel);
                }
            }

            return View("Error");
        }
    }
}