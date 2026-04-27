using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace online_course_recommendation_system.Service
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file);
        Task<string> UploadVideoAsync(IFormFile file);
    }
}
