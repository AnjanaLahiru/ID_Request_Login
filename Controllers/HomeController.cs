using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using ID_Request_Login.Models;

namespace ID_Request_Login.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home 
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (Session["UserType"]?.ToString() == "HR")
            {
                return RedirectToAction("Index", "HR");
            }

            List<Request_Data> requests = GetUserRequests();
            ViewBag.Requests = requests;

            return View();
        }

        [HttpPost]
        public ActionResult Index(Request_Data requestData)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            try
            {
                if (requestData.RequestDate == default(DateTime))
                {
                    requestData.RequestDate = DateTime.Now;
                }

                requestData.Status = "Pending";

                requestData.Section = Session["Section"]?.ToString();

                requestData.RequestBy = Session["UserName"]?.ToString();

                using (Entities db = new Entities())
                {
                    db.Request_Data.Add(requestData);
                    db.SaveChanges();
                }

                ViewData["Message"] = $"ID request for {requestData.EmployeeName} has been submitted successfully!";
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "An error occurred while submitting your request. Please try again.";
            }

            List<Request_Data> requests = GetUserRequests();
            ViewBag.Requests = requests;

            return View();
        }

        private List<Request_Data> GetUserRequests()
        {
            List<Request_Data> requests = new List<Request_Data>();

            string currentUser = Session["Section"]?.ToString();

            if (string.IsNullOrEmpty(currentUser))
            {
                return requests;
            }

            using (Entities db = new Entities())
            {
                requests = db.Request_Data
                    .Where(r => r.Section  == currentUser)
                    .OrderByDescending(r => r.RequestDate)
                    .ToList();
            }

            return requests;
        }

        [HttpPost]
        public ActionResult FilterRequests(string fromDate, string toDate, string status)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            List<Request_Data> filteredRequests = new List<Request_Data>();
            string currentUser = Session["UserName"]?.ToString();

            try
            {
                using (Entities db = new Entities())
                {
                    var query = db.Request_Data.Where(r => r.RequestBy == currentUser);

                    if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
                    {
                        DateTime fromDateObj = DateTime.Parse(fromDate);
                        DateTime toDateObj = DateTime.Parse(toDate).AddDays(1).AddSeconds(-1);

                        query = query.Where(r => r.RequestDate >= fromDateObj && r.RequestDate <= toDateObj);
                    }

                    if (!string.IsNullOrEmpty(status) && status != "All")
                    {
                        query = query.Where(r => r.Status == status);
                    }

                    filteredRequests = query.OrderByDescending(r => r.RequestDate).ToList();
                }

                var formattedRequests = filteredRequests.Select(r => new
                {
                    ReqId = r.ReqId,
                    RequestDate = r.RequestDate.ToString("dd/MM/yyyy"),
                    RequestBy = r.RequestBy,
                    EmployeeName = r.EmployeeName,
                    EmployeeId = r.EmployeeId,
                    Reason = r.Reason,
                    Status = r.Status,
                    CanDelete = r.Status != "Completed" && r.Status != "Issued"
                }).ToList();

                return Json(formattedRequests);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error filtering requests" });
            }
        }

        [HttpPost]
        public ActionResult DeleteRequests(int[] ids)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            int count = 0;         
            int skippedCount = 0;  
            string currentUser = Session["UserName"]?.ToString();

            try
            {
                using (Entities db = new Entities())
                {
                    foreach (int id in ids)
                    {
                        var request = db.Request_Data.FirstOrDefault(r => r.ReqId == id && r.RequestBy == currentUser);

                        if (request != null)
                        {
                            if (request.Status != "Completed" && request.Status != "Issued" && request.Status != "Deleted")
                            {
                                request.Status = "Deleted";
                                count++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                    }

                    if (count > 0)
                    {
                        db.SaveChanges();
                    }
                }

                return Json(new
                {
                    success = true,
                    count = count,
                    skippedCount = skippedCount,
                    message = skippedCount > 0 ?
                        $"{skippedCount} request(s) with status 'Completed' or 'Issued' or 'Deleted' cannot be deleted." : ""
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error processing delete requests" });
            }
        }
    }
}