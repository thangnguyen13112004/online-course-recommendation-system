namespace online_course_recommendation_system.Service
{
    public interface ICloudinaryService
    {
        Task<string?> UploadFileAsync(Microsoft.AspNetCore.Http.IFormFile file, string folder);
    }
}
