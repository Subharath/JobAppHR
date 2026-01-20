using JobAppHR.Models;
using JobAppHR.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace JobAppHR.Controllers
{
    [Authorize(Policy = "NormalUserPolicy")]
    public class JobPositionController : Controller
    {
        private readonly IDBOperations _DBOperations;
        private readonly IUtilityFn _UtilityFn;
        private string loginMsg = "";

        public JobPositionController(IDBOperations dbOperations, IUtilityFn utilityFn)
        {
            _DBOperations = dbOperations;
            _UtilityFn = utilityFn;
            loginMsg = "Your session is expired. Please re-login.";
        }

        public IActionResult Index(string? msg)
        {
            string fieldList = "*";
            DataTable tmpTable = _DBOperations.SelectRows("JOBPOSITION", fieldList, "", "", "");

            List<JobPosition> list = _UtilityFn.ConvertToList<JobPosition>(tmpTable);

            ViewBag.Message = msg;

            return View(list);
        }

        // GET: JobPositionController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        public ActionResult TemplateInfo()
        {
            return View();
        }

        // GET: JobPositionController/Create
        public ActionResult Create(string? msg)
        {
            List<SelectListItem> dataList = _DBOperations.AnyDataList("TEMPLATE", "JOBTEMPLATE", "JOBTEMPLATE", "", "JOBTEMPLATE");
            ViewBag.JobTemplate = new SelectList(dataList, "Value", "Text");

            ViewBag.Message = msg;
            return View();
        }

        // POST: JobPositionController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            string userId = "";
            string message = "";

            User user = _UtilityFn.GetCurrentUser();
            userId = user.UserId;

            try
            {
                JobPosition record = new();

                record.JobPositionName = collection["JobPositionName"];
                record.JobPositionCode = collection["JobPositionCode"].ToString().ToUpper();
                record.JobTemplate = collection["JobTemplate"];
                record.TalentPoolJob = collection["TalentPoolJob"];
                record.InsertedOn = DateTime.Now;
                record.InsertedBy = userId;
                
                record.OLRequired = collection["OLRequired"] == "1" ? 1 : 0;
                record.ALRequired = collection["ALRequired"] == "1" ? 1 : 0;
                record.HERequired = collection["HERequired"] == "1" ? 1 : 0;
                //record.PQRequired = collection["PQRequired"] == "1" ? 1 : 0;
                //record.WERequired = collection["WERequired"] == "1" ? 1 : 0;

                List<JobPosition> list = new List<JobPosition>();

                list.Add(record);

                DataTable tmpTable = new DataTable();

                tmpTable = _UtilityFn.ConvertToDataTable(list);

                //remove fields that need not to be updated
                tmpTable.Columns.Remove("UpdatedBy");
                tmpTable.Columns.Remove("UpdatedOn");

                message = _DBOperations.InsertRecords("JOBPOSITION", tmpTable, true, "JOBPOSITIONID");

                if (message == "SUCCESS")
                {
                    message = "Record successfully saved.";
                    return RedirectToAction("Index", new { msg = message });
                }
                else
                {
                    message = "Record could not be saved. Please try again. " + message;
                    return RedirectToAction("Create", new { msg = message });
                }
            }
            catch
            {
                message = "Record could not be saved. Please try again. " + message;
                return RedirectToAction("Create", new { msg = message });
            }
        }

        // GET: JobPositionController/Edit/5
        public ActionResult Edit(int id, string? msg)
        {
            string fieldList = "*";
            DataTable tmpTable = _DBOperations.SelectRows("JOBPOSITION", fieldList, "JOBPOSITIONID", id.ToString(), "", "int");

            if (tmpTable.Rows.Count == 0)
            {
                return BadRequest();
            }
            else
            {
                List<JobPosition> list = _UtilityFn.ConvertToList<JobPosition>(tmpTable);
                JobPosition item = list.First();

                List<SelectListItem> dataList = _DBOperations.AnyDataList("TEMPLATE", "JOBTEMPLATE", "JOBTEMPLATE", "", "JOBTEMPLATE");
                ViewBag.JobTemplate = new SelectList(dataList, "Value", "Text");
                ViewBag.Message = msg;

                return View(item);
            }
        }

        // POST: JobPositionController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            string userId = "";
            string message = "";

            User user = _UtilityFn.GetCurrentUser();
            userId = user.UserId;

            try
            {
                JobPosition record = new();

                record.JobPositionName = collection["JobPositionName"];
                record.JobPositionCode = collection["JobPositionCode"].ToString().ToUpper();
                record.JobTemplate = collection["JobTemplate"];
                record.TalentPoolJob = collection["TalentPoolJob"];
                record.UpdatedOn = DateTime.Now;
                record.UpdatedBy = userId;

                record.OLRequired = collection["OLRequired"] == "1" ? 1 : 0;
                record.ALRequired = collection["ALRequired"] == "1" ? 1 : 0;
                record.HERequired = collection["HERequired"] == "1" ? 1 : 0;
                //record.PQRequired = collection["PQRequired"] == "1" ? 1 : 0;
                //record.WERequired = collection["WERequired"] == "1" ? 1 : 0;

                List<JobPosition> list = new List<JobPosition>();

                list.Add(record);

                DataTable tmpTable = new DataTable();

                tmpTable = _UtilityFn.ConvertToDataTable(list);

                //remove fields that need not to be updated
                tmpTable.Columns.Remove("InsertedBy");
                tmpTable.Columns.Remove("InsertedOn");

                message = _DBOperations.UpdateRecords("JOBPOSITION", tmpTable, "JOBPOSITIONID", id.ToString(), "int");

                if (message == "SUCCESS")
                {
                    message = "Record successfully saved.";
                    return RedirectToAction("Index", new { msg = message });
                }
                else
                {
                    message = "Record could not be saved. Please try again. " + message;
                    return RedirectToAction("Edit", new { id = id, msg = message });
                }

            }
            catch
            {
                message = "Record could not be saved. Please try again. " + message;
                return RedirectToAction("Edit", new { id = id, msg = message });
            }
        }

        // GET: JobPositionController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: JobPositionController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

    }
}
