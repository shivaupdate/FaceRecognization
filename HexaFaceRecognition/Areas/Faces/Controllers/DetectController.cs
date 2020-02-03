using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using HexaFaceRecognition.Areas.Faces.Models;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace HexaFaceRecognition.Areas.Faces.Controllers
{
    public class DetectController : FacesBaseController
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Detect faces";
            return View();
        }


        public async Task<ActionResult> Identify()
        {
            var personGroupId = Request["PersonGroupId"];
            var model = new IdentifyFacesModel();

            var groups = await FaceClient.ListPersonGroupsAsync();
            model.PersonGroups = groups.Select(g => new SelectListItem
                                                {
                                                    Value = g.PersonGroupId,
                                                    Text = g.Name
                                                }).ToList();

            if (Request.HttpMethod == "GET")
            {                                   
                return View(model);
            }

            try
            {

                Face[] faces = new Face[] { };
                Guid[] faceIds = new Guid[] { };
                IdentifyResult[] results = new IdentifyResult[] { };

                await RunOperationOnImage(async stream =>
                {
                    faces = await FaceClient.DetectAsync(stream);
                    faceIds = faces.Select(f => f.FaceId).ToArray();

                    if (faceIds.Count() > 0)
                    {
                        results = await FaceClient.IdentifyAsync(personGroupId, faceIds);
                    }
                });

                if (faceIds.Length == 0)
                {
                    model.Error = "No faces detected";
                    //return View("ConfirmationPage", model);
                }

                foreach (var result in results)
                {
                    var identifiedFace = new IdentifiedFace();
                    identifiedFace.Face = faces.FirstOrDefault(f => f.FaceId == result.FaceId);

                    foreach (var candidate in result.Candidates)
                    {
                        await RunOperationOnImage(async stream =>
                        {
                            var person = await FaceClient.GetPersonAsync(personGroupId, candidate.PersonId);
                            identifiedFace.PersonCandidates.Add(person.PersonId, person.Name);
                        });

                        // var person = await FaceClient.GetPersonAsync(personGroupId, candidate.PersonId);
                        // identifiedFace.PersonCandidates.Add(person.PersonId, person.Name);
                    }

                    identifiedFace.Color = Settings.ImageSquareColors[model.IdentifiedFaces.Count];

                    model.IdentifiedFaces.Add(identifiedFace);
                }
            }
            catch {
                model.Error = "No faces detected, Please try again";
            }
            model.ImageDump = GetInlineImageWithFaces(model.IdentifiedFaces.Select(x=>x.Face));
            TempData.Add("MyTempData", model);
            return RedirectToAction("ConfirmationPage" );
        }

        public async Task<ActionResult> ConfirmationPage()
        {
            return View(TempData["MyTempData"]);
        }
    }
}