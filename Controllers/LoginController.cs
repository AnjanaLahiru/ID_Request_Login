using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity; 
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
            using (Entities1 db = new Entities1())
            {
                var userdetails = db.Users
                    .Include(u => u.Section_Details)
                    .Where(x => x.UserName == usermodel.UserName && x.Password == usermodel.Password)
                    .FirstOrDefault();

                if (userdetails != null)
                {
                    Session["UserID"] = userdetails.UserID;
                    Session["UserName"] = userdetails.UserName;
                    Session["Section_Id"] = userdetails.Section_Id;

                    Session["Section"] = userdetails.Section_Id.ToString();

                    if (userdetails.Section_Details != null)
                    {
                        Session["SectionName"] = userdetails.Section_Details.Section_Name;
                    }

                    return RedirectToAction("Index", "Home");
                }

                var hrUserdetails = db.HRUsers
                    .Include(h => h.Section_Details)
                    .Where(x => x.HrUserName == usermodel.UserName && x.HrPassword == usermodel.Password)
                    .FirstOrDefault();

                if (hrUserdetails != null)
                {
                    Session["UserID"] = hrUserdetails.HRUserId;
                    Session["UserName"] = hrUserdetails.HrUserName;
                    Session["Section_Id"] = hrUserdetails.Section_Id;

                    Session["Section"] = hrUserdetails.Section_Id.ToString();

                    if (hrUserdetails.Section_Details != null)
                    {
                        Session["SectionName"] = hrUserdetails.Section_Details.Section_Name;
                    }

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