namespace DesiCorner.Models
{
    public interface IDishRepository
    {
        IEnumerable<Dish> AllDishes { get; }
        IEnumerable<Dish> DishesofTheWeek { get; }
        Dish? GetDishByID(int dishId);
    }
}
