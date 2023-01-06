using DesiCorner.Models;

namespace DesiCorner.ViewModels
{
    public class DishListViewModel
    {
        public IEnumerable<Dish> Dishes { get; }
        public string? CurrentCategory { get; }

        public DishListViewModel(IEnumerable<Dish> dishes, string? currenCategory)
        {
            Dishes = dishes;
            CurrentCategory = currenCategory;
        }
    }
}
