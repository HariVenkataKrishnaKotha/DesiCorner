using DesiCorner.Models;
using DesiCorner.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DesiCorner.Controllers
{
    public class DishController : Controller
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICategoryRepository _categoryRepository;

        public DishController(IDishRepository dishRepository, ICategoryRepository categoryRepository)
        {
            _dishRepository = dishRepository;
            _categoryRepository = categoryRepository;
        }

        public IActionResult List()
        {
            //ViewBag.CurrentCategory = "Biriyani";
            //return View(_dishRepository.AllDishes);
            DishListViewModel dishListViewModel = new DishListViewModel(_dishRepository.AllDishes, "Biriyani");
            return View(dishListViewModel);
        }
    }
}
