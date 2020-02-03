using System.Web.Mvc;

namespace HexaFaceRecognition.Areas.Faces.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Detect");
        }
    }
}