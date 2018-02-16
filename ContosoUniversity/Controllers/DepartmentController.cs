using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using System.Data.Entity.Infrastructure;

namespace ContosoUniversity.Controllers
{
    // most of the calls in the DepartmentController use async - await
    // pairs for reaching the concurrency primitives in the language.
    // the async code is not thread safe. in other words don't try to do multiple
    // operations in parallel using the same context instance.
    // if you want to take advantage of the performance benefits of async code,
    // make sure that any library packages you're using (such as for paging, etc.) 
    // also use async code if they are calling any EF methods that cause queries to the database 
    public class DepartmentController : Controller
    {
        private SchoolContext db = new SchoolContext();

        // GET: Department
        public async Task<ActionResult> Index()
        {
            // info about concurrency - async and await
            // method signature tells compiler to generate callbacks for the 
            // sections marked await and create the Task obj to be returned
            // Task<T> type represents ongoign work with a result of type T

            var departments = db.Departments.Include(d => d.Administrator);

            // await keyword applied to the web service call - 
            // when the compiler sees await it splits the method into 2 parts
            // 1 -first part ends with the operation that is started asynchronously
            // 2 -second part is put into a callback method that is called 
            // when the operation completes
            return View(await departments.ToListAsync());

            // the declaration of departments sets up a query to the db
            // but the query is not executed until the ToList is called.
            // that is the only part we will await
        }

        // GET: Department/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Find() exectues the db query so it gets the await keyword
            Department department = await db.Departments.FindAsync(id);
            if (department == null)
            {
                return HttpNotFound();
            }
            return View(department);
        }

        // GET: Department/Create
        public ActionResult Create()
        {
            ViewBag.InstructorID = new SelectList(db.Instructors, "ID", "FullName");
            return View();
        }

        // POST: Department/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "DepartmentID,Name,Budget,StartDate,InstructorID")] Department department)
        {
            if (ModelState.IsValid)
            {
                // Add is not a query to the db - just creating the C# object in memory
                db.Departments.Add(department);

                //  here is the async call - with await tag in front
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.InstructorID = new SelectList(db.Instructors, "ID", "FullName", department.InstructorID);
            return View(department);
        }

        // GET: Department/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // here is the actual execute query instruction - w/ await keyword
            Department department = await db.Departments.FindAsync(id);
            if (department == null)
            {
                return HttpNotFound();
            }
            ViewBag.InstructorID = new SelectList(db.Instructors, "ID", "FullName", department.InstructorID);
            return View(department);
        }

        // POST: Department/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "DepartmentID, Name, Budget, StartDate, RowVersion, InstructorID")] Department department)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Entry(department).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var entry = ex.Entries.Single();
                var clientValues = (Department)entry.Entity;
                var databaseEntry = entry.GetDatabaseValues();

                if (databaseEntry == null)
                {
                    ModelState.AddModelError(string.Empty,
                    "Unable to save changes. The department was deleted by another user.");
                }
                else
                {
                    var databaseValues = (Department)databaseEntry.ToObject();
                    if (databaseValues.Name != clientValues.Name)
                        ModelState.AddModelError("Name", "Current value: "
                            + databaseValues.Name);
                    if (databaseValues.Budget != clientValues.Budget)
                        ModelState.AddModelError("Budget", "Current value: "
                            + String.Format("{0:c}", databaseValues.Budget));
                    if (databaseValues.StartDate != clientValues.StartDate)
                        ModelState.AddModelError("StartDate", "Current value: "
                            + String.Format("{0:d}", databaseValues.StartDate));
                    if (databaseValues.InstructorID != clientValues.InstructorID)
                        ModelState.AddModelError("InstructorID", "Current value: "
                            + db.Instructors.Find(databaseValues.InstructorID).FullName);
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                            + "was modified by another user after you got the original value.The "
                            + "edit operation was canceled and the current values in the database "
                            + "have been displayed. If you still want to edit this record, click "
                            + "the Save button again. Otherwise click the Back to List hyperlink.");

                        department.RowVersion = databaseValues.RowVersion;
                }
            }
            catch (RetryLimitExceededException /* dex */)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                 ModelState.AddModelError(string.Empty, "Unable to save changes. Try again, and if the problem persists contact your system administrator.");
            }
            ViewBag.InstructorID = new SelectList(db.Instructors, "ID", "FullName", department.InstructorID);
            return View(department);
        }

        // GET: Department/Delete/5
        public ActionResult Delete(int? id, bool? concurrencyError)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Department department = db.Departments.Find(id);
            if (department == null)
            {
                return HttpNotFound();
            }
            if (concurrencyError.GetValueOrDefault())
            {
                if (department == null)
                {
                    ViewBag.ConcurrencyErrorMessage = "The record you attempted to delete "
                        + "was deleted by another user after you got the original values. "
                        + "Click the Back to List hyperlink.";
                }
                else
                {
                    ViewBag.ConcurrencyErrorMessage = "The record you attempted to delete "
                        + "was modified by another user after you got the original values. "
                        + "The delete operation was canceled and the current value in the "
                        + "database have been displayed. If you still want to delete this "
                        + "record, click the Delete button again. Otherwise "
                        + "click the Back to List hyperlink.";
                }
            }
            return View(department);
        }

        // POST: Department/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(Department department)
        {
            try
            {
                db.Entry(department).State = EntityState.Deleted;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                return RedirectToAction("Delete", new { concurrencyError = true });
            }
            catch (DataException /* dex */)
            {
                //Log the error (uncomment dex variable name after DataException and add a line here to write a log.
                ModelState.AddModelError(string.Empty, "Unable to delete. Try again, and if the problem persists contact your system administrator.");
                return View(department);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
