using System;
using System.Web.Mvc;
using ContosoUniversity.DAL;
using ContosoUniversity.ViewModels;
using System.Linq;

namespace ContosoUniversity.Controllers
{
    public class HomeController : Controller
    {
        private SchoolContext db = new SchoolContext();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            IQueryable<EnrollmentDateGroup> data = from student in db.Students
                    group student by student.EnrollmentDate into dateGroup
                    select new EnrollmentDateGroup()
                    {
                        EnrollmentDate = dateGroup.Key,
                        StudentCount = dateGroup.Count()
                    };

            return View(data.ToList());
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Dykstra Tutorial, ASP .NET MVC 5";

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
