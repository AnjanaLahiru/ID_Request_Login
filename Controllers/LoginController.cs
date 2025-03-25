using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ID_Request_Login.Models;

namespace ID_Request_Login.Controllers
{
    public class LoginController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Authersize(ID_Request_Login.Models.User usermodel)
        {
            using (Entities db = new Entities())
            {
                var userdetails = db.Users.Where(x => x.UserName == usermodel.UserName && x.Password == usermodel.Password).FirstOrDefault();

                if (userdetails != null)
                {
                    Session["UserID"] = userdetails.UserID;
                    Session["UserName"] = userdetails.UserName;
                    Session["Section"] = userdetails.Section;
                    return RedirectToAction("Index", "Home");
                }

                var hrUserdetails = db.HRUsers.Where(x => x.HrUserName == usermodel.UserName && x.HrPassword == usermodel.Password).FirstOrDefault();

                if (hrUserdetails != null)
                {
                    Session["UserID"] = hrUserdetails.HRUserId;
                    Session["UserName"] = hrUserdetails.HrUserName;
                    Session["Section"] = hrUserdetails.Section;
                    Session["UserType"] = "HR";
                    return RedirectToAction("Index", "HR");
                }

                usermodel.LoginErrorMessage = "Wrong username or password.";
                return View("Index", usermodel);
            }
        }

        public ActionResult LogOut()
        {
            int UserId = (int)Session["UserID"];
            Session.Abandon();
            return RedirectToAction("Index", "Login");
        }
    }
}