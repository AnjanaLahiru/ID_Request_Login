using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;
using ID_Request_Login.Models;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text;

namespace ID_Request_Login.Controllers
{
    public class HRController : Controller
    {
        // GET: HR
        public ActionResult Index()
        {
            List<Request_Data> requests = GetAllRequests();
            ViewBag.Requests = requests;
            return View();
        }

        private List<Request_Data> GetAllRequests()
        {
            List<Request_Data> requests = new List<Request_Data>();
            string mainconn = ConfigurationManager.ConnectionStrings["SqlConnection"].ConnectionString;

            using (SqlConnection sqlconn = new SqlConnection(mainconn))
            {
                sqlconn.Open();
                string sqlquery = "SELECT * FROM [dbo].[Request_Data] ORDER BY RequestDate DESC";

                using (SqlCommand sqlcomm = new SqlCommand(sqlquery, sqlconn))
                {
                    using (SqlDataReader reader = sqlcomm.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Request_Data request = new Request_Data
                            {
                                ReqId = Convert.ToInt32(reader["ReqId"]),
                                RequestDate = Convert.ToDateTime(reader["RequestDate"]),
                                RequestBy = reader["RequestBy"].ToString(),
                                EmployeeName = reader["EmployeeName"].ToString(),
                                EmployeeId = reader["EmployeeId"].ToString(),
                                Reason = reader["Reason"].ToString(),
                                Status = reader["Status"].ToString(),
                                Section = reader["Section"] != DBNull.Value ? reader["Section"].ToString() : null
                            };
                            requests.Add(request);
                        }
                    }
                }
            }

            return requests;
        }

        [HttpPost]
        public ActionResult FilterRequests(string fromDate, string toDate, string status, string section)
        {
            List<Request_Data> allRequests = GetAllRequests();
            List<Request_Data> filteredRequests = allRequests;

            if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
            {
                DateTime fromDateObj = DateTime.Parse(fromDate);
                DateTime toDateObj = DateTime.Parse(toDate);

                filteredRequests = filteredRequests.Where(r =>
                    r.RequestDate.Date >= fromDateObj.Date &&
                    r.RequestDate.Date <= toDateObj.Date
                ).ToList();
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                filteredRequests = filteredRequests.Where(r => r.Status == status).ToList();
            }

            if (!string.IsNullOrEmpty(section) && section != "All")
            {
                filteredRequests = filteredRequests.Where(r => r.Section == section).ToList();
            }

            return Json(filteredRequests);
        }
        [HttpPost]
        public ActionResult CreateRequest(Request_Data request)
        {
            try
            {
                string mainconn = ConfigurationManager.ConnectionStrings["SqlConnection"].ConnectionString;

                using (SqlConnection sqlconn = new SqlConnection(mainconn))
                {
                    sqlconn.Open();
                    string sqlquery = "INSERT INTO [dbo].[Request_Data] (RequestDate, RequestBy, EmployeeName, EmployeeId, Reason, Status, Section) " +
                                      "VALUES (@RequestDate, @RequestBy, @EmployeeName, @EmployeeId, @Reason, @Status, @Section)";

                    using (SqlCommand sqlcomm = new SqlCommand(sqlquery, sqlconn))
                    {
                        DateTime requestDate;
                        string dateStr = request.RequestDate.ToString();

                        if (dateStr.Contains("-"))
                        {
                            if (DateTime.TryParse(dateStr, out requestDate))
                            {
                                sqlcomm.Parameters.AddWithValue("@RequestDate", requestDate);
                            }
                            else
                            {
                                sqlcomm.Parameters.AddWithValue("@RequestDate", DateTime.Now);
                            }
                        }
                        else if (dateStr.Contains("/"))
                        {
                            string[] dateParts = dateStr.Split('/');
                            if (dateParts.Length == 3)
                            {
                                int day, month, year;
                                if (int.TryParse(dateParts[0], out day) &&
                                    int.TryParse(dateParts[1], out month) &&
                                    int.TryParse(dateParts[2], out year))
                                {
                                    try
                                    {
                                        requestDate = new DateTime(year, month, day);
                                        sqlcomm.Parameters.AddWithValue("@RequestDate", requestDate);
                                    }
                                    catch (ArgumentOutOfRangeException)
                                    {
                                        sqlcomm.Parameters.AddWithValue("@RequestDate", DateTime.Now);
                                    }
                                }
                                else
                                {
                                    sqlcomm.Parameters.AddWithValue("@RequestDate", DateTime.Now);
                                }
                            }
                            else
                            {
                                sqlcomm.Parameters.AddWithValue("@RequestDate", DateTime.Now);
                            }
                        }
                        else
                        {
                            if (DateTime.TryParse(dateStr, out requestDate))
                            {
                                sqlcomm.Parameters.AddWithValue("@RequestDate", requestDate);
                            }
                            else
                            {
                                sqlcomm.Parameters.AddWithValue("@RequestDate", DateTime.Now);
                            }
                        }

                        sqlcomm.Parameters.AddWithValue("@RequestBy", request.RequestBy);
                        sqlcomm.Parameters.AddWithValue("@EmployeeName", request.EmployeeName);
                        sqlcomm.Parameters.AddWithValue("@EmployeeId", request.EmployeeId);
                        sqlcomm.Parameters.AddWithValue("@Reason", request.Reason);
                        sqlcomm.Parameters.AddWithValue("@Status", request.Status ?? "Pending");
                        sqlcomm.Parameters.AddWithValue("@Section", request.Section);

                        sqlcomm.ExecuteNonQuery();
                    }
                }

                return new EmptyResult();
            }
            catch (Exception ex)
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public ActionResult DeleteRequests(int[] ids)
        {
            int count = 0;
            string mainconn = ConfigurationManager.ConnectionStrings["SqlConnection"].ConnectionString;

            using (SqlConnection sqlconn = new SqlConnection(mainconn))
            {
                sqlconn.Open();
                string sqlquery = "UPDATE [dbo].[Request_Data] SET Status = 'Deleted' WHERE ReqId = @ReqId";

                foreach (int id in ids)
                {
                    using (SqlCommand sqlcomm = new SqlCommand(sqlquery, sqlconn))
                    {
                        sqlcomm.Parameters.AddWithValue("@ReqId", id);
                        int rowsAffected = sqlcomm.ExecuteNonQuery();
                        count += rowsAffected;
                    }
                }
            }

            return Json(new { success = true, count = count });
        }

        [HttpPost]
        public ActionResult UpdateRequestStatus(int reqId, string newStatus)
        {
            string currentStatus = string.Empty;
            string mainconn = ConfigurationManager.ConnectionStrings["SqlConnection"].ConnectionString;

            using (SqlConnection sqlconn = new SqlConnection(mainconn))
            {
                sqlconn.Open();
                string getStatusQuery = "SELECT Status FROM [dbo].[Request_Data] WHERE ReqId = @ReqId";
                using (SqlCommand getStatusCmd = new SqlCommand(getStatusQuery, sqlconn))
                {
                    getStatusCmd.Parameters.AddWithValue("@ReqId", reqId);
                    var result = getStatusCmd.ExecuteScalar();
                    if (result != null)
                    {
                        currentStatus = result.ToString();
                    }
                    else
                    {
                        return Json(new { success = false, message = "Request not found." });
                    }
                }

                bool isValidProgression = false;

                if (currentStatus == "Pending" && newStatus == "In-Progress")
                {
                    isValidProgression = true;
                }
                else if (currentStatus == "In-Progress" && newStatus == "Completed")
                {
                    isValidProgression = true;
                }
                else if (currentStatus == "Completed" && newStatus == "Issued")
                {
                    isValidProgression = true;
                }
                else if (currentStatus == newStatus)
                {
                    return Json(new { success = true, message = "Status unchanged." });
                }

                if (!isValidProgression)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Invalid status transition from '{currentStatus}' to '{newStatus}'. Status can only progress forward."
                    });
                }

                string updateQuery = "UPDATE [dbo].[Request_Data] SET Status = @Status WHERE ReqId = @ReqId";
                using (SqlCommand updateCmd = new SqlCommand(updateQuery, sqlconn))
                {
                    updateCmd.Parameters.AddWithValue("@Status", newStatus);
                    updateCmd.Parameters.AddWithValue("@ReqId", reqId);

                    int rowsAffected = updateCmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = $"Status updated from '{currentStatus}' to '{newStatus}' successfully." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Failed to update status. Please try again." });
                    }
                }
            }
        }

        [HttpPost]
        public ActionResult GenerateReport(string fromDate, string toDate, string status, string section)
        {
            List<Request_Data> allRequests = GetAllRequests();
            List<Request_Data> filteredRequests = allRequests;

            if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
            {
                DateTime fromDateObj = DateTime.Parse(fromDate);
                DateTime toDateObj = DateTime.Parse(toDate);

                filteredRequests = filteredRequests.Where(r =>
                    r.RequestDate.Date >= fromDateObj.Date &&
                    r.RequestDate.Date <= toDateObj.Date
                ).ToList();
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                filteredRequests = filteredRequests.Where(r => r.Status == status).ToList();
            }

            if (!string.IsNullOrEmpty(section) && section != "All")
            {
                filteredRequests = filteredRequests.Where(r => r.Section == section).ToList();
            }

            MemoryStream ms = new MemoryStream();
            Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);

            PdfWriter writer = PdfWriter.GetInstance(document, ms);
            writer.PageEvent = new PageNumberFooter();

            document.AddAuthor("ID Request System");
            document.AddCreator("HR Department");
            document.AddTitle("ID Requests Report");
            document.Open();

            Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DARK_GRAY);
            Paragraph title = new Paragraph("ID Requests Report", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 20;
            document.Add(title);

            Font infoFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.DARK_GRAY);
            StringBuilder reportInfo = new StringBuilder();
            reportInfo.AppendLine($"Generated by: {Session["UserName"]}");
            reportInfo.AppendLine($"Date: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");

            if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
            {
                reportInfo.AppendLine($"Date Range: {fromDate} to {toDate}");
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                reportInfo.AppendLine($"Status Filter: {status}");
            }

            if (!string.IsNullOrEmpty(section) && section != "All")
            {
                reportInfo.AppendLine($"Section Filter: {section}");
            }

            reportInfo.AppendLine($"Total Records: {filteredRequests.Count}");

            Paragraph info = new Paragraph(reportInfo.ToString(), infoFont);
            info.SpacingAfter = 20;
            document.Add(info);

            PdfPTable table = new PdfPTable(7);
            table.WidthPercentage = 100;

            float[] columnWidths = new float[] { 15f, 15f, 20f, 10f, 15f, 10f, 15f };
            table.SetWidths(columnWidths);

            Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.BLACK);
            BaseColor headerBackground = BaseColor.WHITE;

            AddHeaderCell(table, "Request Date", headerFont, headerBackground);
            AddHeaderCell(table, "Request By", headerFont, headerBackground);
            AddHeaderCell(table, "Employee Name", headerFont, headerBackground);
            AddHeaderCell(table, "Employee ID", headerFont, headerBackground);
            AddHeaderCell(table, "Reason", headerFont, headerBackground);
            AddHeaderCell(table, "Section", headerFont, headerBackground);
            AddHeaderCell(table, "Status", headerFont, headerBackground);

            Font cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
            Font pendingFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, new BaseColor(255, 152, 0));
            Font inProgressFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, new BaseColor(33, 150, 243));
            Font completedFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, new BaseColor(76, 175, 80));
            Font deletedFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, new BaseColor(244, 67, 54));

            foreach (var request in filteredRequests)
            {
                BaseColor rowBackground = table.Rows.Count % 2 == 1 ? new BaseColor(245, 247, 251) : BaseColor.WHITE;

                AddCell(table, request.RequestDate.ToString("yyyy-MM-dd"), cellFont, rowBackground);
                AddCell(table, request.RequestBy, cellFont, rowBackground);
                AddCell(table, request.EmployeeName, cellFont, rowBackground);
                AddCell(table, request.EmployeeId, cellFont, rowBackground);
                AddCell(table, request.Reason, cellFont, rowBackground);
                AddCell(table, request.Section ?? "", cellFont, rowBackground);

                Font statusFont = cellFont;
                switch (request.Status)
                {
                    case "Pending": statusFont = pendingFont; break;
                    case "In-Progress": statusFont = inProgressFont; break;
                    case "Completed": statusFont = completedFont; break;
                    case "Deleted": statusFont = deletedFont; break;
                }

                AddCell(table, request.Status, statusFont, rowBackground);
            }

            document.Add(table);

            document.Close();
            writer.Close();

            byte[] pdfBytes = ms.ToArray();
            return File(pdfBytes, "application/pdf", "ID_Requests_Report.pdf");
        }

        private void AddHeaderCell(PdfPTable table, string text, Font font, BaseColor backgroundColor)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = backgroundColor;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Padding = 5;
            table.AddCell(cell);
        }

        private void AddCell(PdfPTable table, string text, Font font, BaseColor backgroundColor)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = backgroundColor;
            cell.HorizontalAlignment = Element.ALIGN_LEFT;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Padding = 5;
            table.AddCell(cell);
        }
    }

    public class PageNumberFooter : PdfPageEventHelper
    {
        public override void OnEndPage(PdfWriter writer, Document document)
        {
            PdfPTable footerTable = new PdfPTable(1);
            footerTable.TotalWidth = 530;
            footerTable.HorizontalAlignment = Element.ALIGN_CENTER;

            Font footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.DARK_GRAY);
            PdfPCell cell = new PdfPCell(new Phrase($"Page {writer.PageNumber}", footerFont));

            cell.Border = Rectangle.NO_BORDER;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;

            footerTable.AddCell(cell);

            footerTable.WriteSelectedRows(0, -1, 30, 30, writer.DirectContent);
        }
    }
}