using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace HexaFaceRecognition.Areas.Faces.Controllers
{
    public class PeopleController : FacesBaseController
    {
        public async Task<ActionResult> Index(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                return HttpNotFound("Group ID is missing");
            }

            var model = await FaceClient.ListPersonsAsync(id);
            ViewBag.PersonGroupId = id;

            return View(model);
        }

        public async Task<ActionResult> Details(string id, Guid? personId)
        {
            if(string.IsNullOrEmpty(id))
            {
                return HttpNotFound();
            }

            if(personId == null)
            {
                return HttpNotFound();
            }

            var model = await FaceClient.GetPersonAsync(id, personId.Value);
            ViewBag.PersonGroupId = id;

            return View(model);
        }

        public ActionResult Create(string id)
        {
            ViewBag.PersonGroupId = id;

            return View("Edit", new Person());
        }

        [HttpPost]
        public async Task<ActionResult> Create(Person person)
        {
            return await Edit(person);
        }

        public async Task<ActionResult> Edit(string id, Guid personId)
        {
            ViewBag.PersonGroupId = id;

            var model = await FaceClient.GetPersonAsync(id, personId);

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(Person person)
        {
            var personGroupId = Request.Form["PersonGroupId"];
            if(string.IsNullOrEmpty(personGroupId))
            {
                return HttpNotFound("GroupId is missing");
            }

            if(!ModelState.IsValid)
            {
                ViewBag.PersonGroupId = personGroupId;
                return View(person);
            }

            try
            {
                if (person.PersonId == Guid.Empty)
                {
                    await FaceClient.CreatePersonAsync(personGroupId, person.Name, person.UserData);
                }
                else
                {
                    await FaceClient.UpdatePersonAsync(personGroupId, person.PersonId, person.Name, person.UserData);
                }

                return RedirectToAction("Index", new { id = personGroupId });
            }
            catch (FaceAPIException fex)
            {
                ModelState.AddModelError(string.Empty, fex.ErrorMessage);
            }

            return View(person);
        }

        [HttpGet]
        public ActionResult AddFace(string id, string personId)
        {
            return View();
        }

        public async Task<ActionResult> Delete(string id, Guid personId)
        {
            ViewBag.PersonGroupId = id;

            await FaceClient.DeletePersonAsync(id, personId);

            return RedirectToAction("Index", new { id = id });
        }

        [HttpPost]
        public async Task<ActionResult> AddFace()
        {
            var id = Request["id"];
            var personId = Guid.Parse(Request["personId"]);
            if (Request.Files.Count > 0)
            {
                //ResizeImage(Request.Files[0].InputStream, ImageToProcess);
                Image img = Image.FromStream(Request.Files[0].InputStream);
                string url = Request.Url.Authority;
                img.Save(Server.MapPath("~/images") + "\\IDCard\\"+ Request["personName"] +".png", ImageFormat.Png);
            }


            try
            {
                //await FaceClient.AddPersonFaceAsync(id, personId, Request.Files[0].InputStream);
                await RunOperationOnImage(async stream =>
                {
                    await FaceClient.AddPersonFaceAsync(id, personId, stream);
                });
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }

            //try
            //{
            //    await FaceClient.AddPersonFaceAsync(id, personId, Request.Files[0].InputStream);
            //}
            //catch (Exception ex)
            //{
            //    ViewBag.Error = ex.StackTrace;
            //    return View();
            //}

            return RedirectToAction("Index", new { id = id });
        }
    }
}