using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Mvc;
using HexaFaceRecognition.Areas.Faces.Models;
using HexaFaceRecognition.Models;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace HexaFaceRecognition.Areas.Faces.Controllers
{
    public abstract class FacesBaseController : ImageUsingBaseController
    {
        protected FaceServiceClient FaceClient { get; private set; }

        public FacesBaseController()
        { 
            var apiKey = ConfigurationManager.AppSettings["CognitiveServicesFaceApiKey"];
            var apiRoot = ConfigurationManager.AppSettings["CognitiveServicesFaceApiUrl"];
            FaceClient = new FaceServiceClient(apiKey, apiRoot);
        }        

        protected string GetInlineImageWithFaces(IEnumerable<Face> faces)
        {
            ImageToProcess.Seek(0, SeekOrigin.Begin);

            using (var img = new Bitmap(ImageToProcess))
            // make copy, drawing on indexed pixel format image is not supported
            using (var nonIndexedImg = new Bitmap(img.Width, img.Height))
            using (var g = Graphics.FromImage(nonIndexedImg))
            using (var mem = new MemoryStream())
            {
                g.DrawImage(img, 0, 0, img.Width, img.Height);

                //var i = 0;

                //foreach (var face in faces)
                //{
                //    var pen = new Pen(Settings.ImageSquareColors[i], 5);
                //    var faceRectangle = face.FaceRectangle;
                //    var rectangle = new Rectangle(faceRectangle.Left,
                //                                  faceRectangle.Top,
                //                                  faceRectangle.Width,
                //                                  faceRectangle.Height);

                //    g.DrawRectangle(pen, rectangle);
                //    i++;
                //}

                nonIndexedImg.Save(mem, ImageFormat.Png);

                var base64 = Convert.ToBase64String(mem.ToArray());
                return String.Format("data:image/png;base64,{0}", base64);
            }
        }

        protected string GetInlineImageWithIdentifiedFaces(IEnumerable<IdentifiedFace> faces)
        {
            ImageToProcess.Seek(0, SeekOrigin.Begin);

            using (var img = new Bitmap(ImageToProcess))
            // make copy, drawing on indexed pixel format image is not supported
            using (var nonIndexedImg = new Bitmap(img.Width, img.Height))
            using (var g = Graphics.FromImage(nonIndexedImg))
            using (var mem = new MemoryStream())
            {
                g.DrawImage(img, 0, 0, img.Width, img.Height);

                var i = 0;

                foreach (var face in faces)
                {
                    var pen = new Pen(face.Color, 5);
                    var faceRectangle = face.Face.FaceRectangle;
                    var rectangle = new Rectangle(faceRectangle.Left,
                                                  faceRectangle.Top,
                                                  faceRectangle.Width,
                                                  faceRectangle.Height);

                    g.DrawRectangle(pen, rectangle);
                    i++;
                }

                nonIndexedImg.Save(mem, ImageFormat.Png);

                var base64 = Convert.ToBase64String(mem.ToArray());
                return String.Format("data:image/png;base64,{0}", base64);
            }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            ViewBag.LeftMenu = "_FacesMenu";
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            base.OnException(filterContext);

            if(filterContext.ExceptionHandled)
            {
                return;
            }

            var message = filterContext.Exception.Message;
            message = filterContext.Exception.StackTrace;
            var code = "";

            if (filterContext.Exception is FaceAPIException)
            {
                var faex = filterContext.Exception as FaceAPIException;
                message = faex.ErrorMessage;
                code = faex.ErrorCode;
                message = faex.StackTrace;
            }
            //message = "Please come closer and try again.";
            filterContext.Result = new ViewResult
            {
                ViewName = "Error",
                ViewData = new ViewDataDictionary(filterContext.Controller.ViewData)
                {
                    Model = new ErrorModel { Code = code, Message = message }
                }
            };

            filterContext.ExceptionHandled = true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if(FaceClient != null)
            {
                FaceClient.Dispose();
                FaceClient = null;
            }            
        }
    }
}