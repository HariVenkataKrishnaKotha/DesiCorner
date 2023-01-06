namespace DesiCorner.Models
{
    public class MockCategoryRepository : ICategoryRepository
    {
        public IEnumerable<Category> AllCategories =>
            new List<Category>
            {
                new Category{CategoryId=1,CategoryName="Appetizers",Description="All-Appetizers"},
                new Category{CategoryId=2,CategoryName="Curries",Description="All-Curries"},
                new Category{CategoryId=3,CategoryName="Main Course",Description="All-Main Course"}
            };   
    }
}
