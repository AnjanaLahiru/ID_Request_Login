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
        private const int PageLimit = 10;

        // GET: Home 
        public ActionResult Index(int page = 1)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (Session["UserType"]?.ToString() == "HR")
            {
                return RedirectToAction("Index", "HR");
            }

            string sectionName = Session["SectionName"]?.ToString() ?? "Unknown Section";
            ViewBag.SectionName = sectionName;

            List<Request_Data> allRequests = GetUserRequests();

            int totalItems = allRequests.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)PageLimit);

            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var pagedRequests = allRequests.Skip((page - 1) * PageLimit).Take(PageLimit).ToList();

            ViewBag.Requests = pagedRequests;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

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

                // Get the section ID from session
                string sectionId = Session["Section"]?.ToString();

                // Get the section name from the section_details table
                string sectionName = null;

                using (Entities1 db = new Entities1())
                {
                    if (!string.IsNullOrEmpty(sectionId))
                    {
                        int sectionIdInt;
                        if (int.TryParse(sectionId, out sectionIdInt))
                        {
                            var section = db.Section_Details.FirstOrDefault(s => s.Section_Id == sectionIdInt);
                            if (section != null)
                            {
                                sectionName = section.Section_Name;
                            }
                        }
                    }

                    // Set the section name instead of ID
                    requestData.Section = sectionName ?? Session["SectionName"]?.ToString() ?? sectionId;
                    requestData.RequestBy = Session["UserName"]?.ToString();

                    db.Request_Data.Add(requestData);
                    db.SaveChanges();
                }

                ViewData["Message"] = $"ID request for {requestData.EmployeeName} has been submitted successfully!";
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "An error occurred while submitting your request. Please try again.";
            }

            return RedirectToAction("Index", new { page = 1 });
        }

        private string GetSectionName()
        {
            return Session["SectionName"]?.ToString() ?? "Unknown Section";
        }


        private List<Request_Data> GetUserRequests()
        {
            List<Request_Data> requests = new List<Request_Data>();

            // Get section name from session
            string sectionName = Session["SectionName"]?.ToString();

            if (string.IsNullOrEmpty(sectionName))
            {
                return requests;
            }

            using (Entities1 db = new Entities1())
            {
                // Get requests where Section equals the section name
                requests = db.Request_Data
                    .Where(r => r.Section == sectionName)
                    .OrderByDescending(r => r.RequestDate)
                    .ToList();

                // If no requests found by section name, fall back to section ID for backward compatibility
                if (!requests.Any())
                {
                    string sectionId = Session["Section"]?.ToString();
                    if (!string.IsNullOrEmpty(sectionId))
                    {
                        requests = db.Request_Data
                            .Where(r => r.Section == sectionId)
                            .OrderByDescending(r => r.RequestDate)
                            .ToList();
                    }
                }
            }

            return requests;
        }

        [HttpPost]
        public ActionResult FilterRequests(string fromDate, string toDate, string status, int page = 1)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            List<Request_Data> filteredRequests = new List<Request_Data>();
            string sectionName = Session["SectionName"]?.ToString() ?? "Unknown Section";

            try
            {
                using (Entities1 db = new Entities1())
                {
                    // Filter by section name instead of section ID
                    var query = db.Request_Data.Where(r => r.Section == sectionName);

                    // If no results, try with section ID for backward compatibility
                    if (!query.Any())
                    {
                        string sectionId = Session["Section"]?.ToString();
                        if (!string.IsNullOrEmpty(sectionId))
                        {
                            query = db.Request_Data.Where(r => r.Section == sectionId);
                        }
                    }

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

                int totalItems = filteredRequests.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)PageLimit);

                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

                var pagedRequests = filteredRequests.Skip((page - 1) * PageLimit).Take(PageLimit).ToList();

                var formattedRequests = pagedRequests.Select(r => new
                {
                    ReqId = r.ReqId,
                    RequestDate = r.RequestDate.ToString("dd/MM/yyyy"),
                    RequestBy = r.RequestBy,
                    EmployeeName = r.EmployeeName,
                    EmployeeId = r.EmployeeId,
                    Reason = r.Reason,
                    Status = r.Status,
                    SectionName = sectionName,
                    CanDelete = r.Status != "Completed" && r.Status != "Issued" && r.Status != "Deleted"
                }).ToList();

                return Json(new
                {
                    data = formattedRequests,
                    currentPage = page,
                    totalPages = totalPages,
                    totalItems = totalItems,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error filtering requests: " + ex.Message });
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
            string sectionName = Session["SectionName"]?.ToString();

            try
            {
                using (Entities1 db = new Entities1())
                {
                    foreach (int id in ids)
                    {
                        // Find request by ID and section name
                        var request = db.Request_Data.FirstOrDefault(r => r.ReqId == id && r.Section == sectionName);

                        // If not found, try with section ID for backward compatibility
                        if (request == null)
                        {
                            string sectionId = Session["Section"]?.ToString();
                            if (!string.IsNullOrEmpty(sectionId))
                            {
                                request = db.Request_Data.FirstOrDefault(r => r.ReqId == id && r.Section == sectionId);
                            }
                        }

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
                return Json(new { success = false, message = "Error processing delete requests: " + ex.Message });
            }
        }
    }
}