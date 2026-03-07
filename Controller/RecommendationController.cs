using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;

namespace online_course_recommendation_system.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecommendationController : ControllerBase
    {
        private readonly IDriver _driver;

      
        public RecommendationController()
        {
            // sửa password của mọi người ở đây nha, nhớ là phải đúng với password của neo4j trên máy của mọi người
            // hoặc là mọi người đặt password cho instance trong NeO4J của mọi người là 12345678 thì khỏi phải sửa
            _driver = GraphDatabase.Driver("neo4j://127.0.0.1:7687", AuthTokens.Basic("neo4j", "12345678"));
        }

        [HttpGet("user-based/{userId}")]
        public async Task<IActionResult> GetUserBasedRecommendations(int userId)
        {
            var recommendedCourses = new List<object>();

            // Mở phiên làm việc với Neo4j
            await using var session = _driver.AsyncSession(o => o.WithDatabase("neo4j"));

            // Chèn câu Cypher thuật toán vào đây
            var query = @"
            MATCH (u1:User {id: $userId})-[r1:RATED]->(common_course:Course)<-[r2:RATED]-(u2:User)
            WHERE u1 <> u2 AND r1.rating >= 4 AND r2.rating >= 4
            MATCH (u2)-[r3:RATED]->(rec_course:Course)
            WHERE r3.rating >= 4 AND NOT (u1)-[:RATED]->(rec_course)
            RETURN rec_course.id AS CourseId, count(u2) AS RecommendationScore
            ORDER BY RecommendationScore DESC
            LIMIT 10";

            // Thực thi và lấy kết quả
            var result = await session.RunAsync(query, new { userId });

            await result.ForEachAsync(record =>
            {
                recommendedCourses.Add(new
                {
                    CourseId = record["CourseId"].As<int>(),
                    Score = record["RecommendationScore"].As<int>()
                });
            });

            return Ok(recommendedCourses);
        }
    }
}
