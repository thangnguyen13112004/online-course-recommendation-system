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
                    Score = record["RecommendationScore"].As<long>() 
                });
            });

            return Ok(recommendedCourses);
        }

        // CONTENT-BASED FILTERING
        // 1. API: Cập nhật độ tương đồng giữa các khóa học (Bước 1, 2, 3)
        [HttpPost("content-based/generate-similarity")]
        public async Task<IActionResult> GenerateContentSimilarity()
        {
            await using var session = _driver.AsyncSession(o => o.WithDatabase("neo4j"));

            try
            {
                // Bước 1: Xóa relationships cũ và drop projection cũ
                await session.RunAsync("MATCH ()-[r:CONTENT_SIMILAR]-() DELETE r");
                await session.RunAsync("CALL gds.graph.drop('contentGraph', false)");

                // Bước 2: Tạo projection mới
                await session.RunAsync(@"
                    CALL gds.graph.project(
                        'contentGraph',
                        ['Course','Topic'],
                        ['HAS_TOPIC']
                    )");

                // Bước 3: Chạy Node Similarity
                await session.RunAsync(@"
                    CALL gds.nodeSimilarity.write('contentGraph', {
                        nodeLabels:['Course','Topic'],
                        writeRelationshipType: 'CONTENT_SIMILAR',
                        writeProperty: 'score',
                        similarityCutoff: 0.5
                    })");

                return Ok(new { message = "Cập nhật thành công độ tương đồng giữa các khóa học." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật dữ liệu.", error = ex.Message });
            }
        }

        // 2. API: Tìm khóa học tương tự một khóa học cụ thể (Bước A)
        [HttpGet("content-based/similar-courses/{courseId}")]
        public async Task<IActionResult> GetSimilarCourses(int courseId)
        {
            var recommendedCourses = new List<object>();
            await using var session = _driver.AsyncSession(o => o.WithDatabase("neo4j"));

            // Lưu ý: Đã sửa id: 23 thành $courseId, thêm điều kiện i.id <> q.id để loại bỏ chính nó, và Order By
            var query = @"
                MATCH (i:Course {id: $courseId})-[r:CONTENT_SIMILAR]-(q:Course)
                WHERE r.score >= 0.5 AND i.id <> q.id
                RETURN DISTINCT q.id AS CourseId, q.title AS Title, r.score AS Score
                ORDER BY Score DESC
                LIMIT 10";

            var result = await session.RunAsync(query, new { courseId });
            await result.ForEachAsync(record =>
            {
                recommendedCourses.Add(new
                {
                    CourseId = record["CourseId"].As<int>(),
                    Title = record["Title"].As<string>(),
                    Score = record["Score"].As<double>() // Ép kiểu float/double cho score
                });
            });

            return Ok(recommendedCourses);
        }

        // 3. API: Gợi ý theo hồ sơ User (Bước B)
        [HttpGet("content-based/user-profile/{userId}")]
        public async Task<IActionResult> GetProfilePageItems(int userId)
        {
            var recommendedCourses = new List<object>();
            await using var session = _driver.AsyncSession(o => o.WithDatabase("neo4j"));

            // Lưu ý: Đã thay id: 1 thành $userId
            var query = @"
                MATCH (u:User {id: $userId})-[r:RATED]->(i:Course)
                WITH i, COLLECT(i) as ratedItems, (r.rating - 1) / 4.0 as normalizedRating
                ORDER BY normalizedRating DESC
                LIMIT 5

                MATCH (i)-[rel:CONTENT_SIMILAR]-(q:Course)
                WHERE NOT q IN ratedItems
                WITH q, rel.score + 1.5 * normalizedRating as simScore
                ORDER BY simScore DESC
                WITH DISTINCT q, simScore
                LIMIT 10
                RETURN q.id as CourseId, q.title as Title, simScore as Score";

            var result = await session.RunAsync(query, new { userId });
            await result.ForEachAsync(record =>
            {
                recommendedCourses.Add(new
                {
                    CourseId = record["CourseId"].As<int>(),
                    Title = record["Title"].As<string>(),
                    Score = record["Score"].As<double>()
                });
            });

            return Ok(recommendedCourses);
        }
    }
}
