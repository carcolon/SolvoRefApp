namespace Core.Models.Global
{
    public class PagedResponse<T> : Response<T>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
    }
}